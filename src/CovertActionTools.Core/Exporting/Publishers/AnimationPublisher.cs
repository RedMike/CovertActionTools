using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Publishers
{
    /// <summary>
    /// Given a loaded model for an Animation, returns multiple assets to save:
    ///   * PAN file (legacy animation)
    /// </summary>
    internal class AnimationPublisher : BaseExporter<Dictionary<string, AnimationModel>>, ILegacyPublisher
    {
        private const int PaletteBlockLength = 774;
        private const int ImageTableLength = 250;

        private readonly ILogger<AnimationPublisher> _logger;
        private readonly SharedImageExporter _imageExporter;

        private readonly List<string> _keys = new();
        private int _index = 0;

        public AnimationPublisher(ILogger<AnimationPublisher> logger, SharedImageExporter imageExporter)
        {
            _logger = logger;
            _imageExporter = imageExporter;
        }

        protected override string Message => "Processing animations..";

        protected override Dictionary<string, AnimationModel> GetFromModel(PackageModel model)
        {
            return model.Animations
                .Where(x => model.Index.AnimationIncluded.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        protected override void Reset()
        {
            _keys.Clear();
            _index = 0;
        }

        protected override int GetTotalItemCountInPath()
        {
            return _keys.Count;
        }

        protected override int RunExportStepInternal()
        {
            if (_index >= _keys.Count)
            {
                return _index;
            }
            var nextKey = _keys[_index];

            var files = Export(Data[nextKey]);
            foreach (var pair in files)
            {
                File.WriteAllBytes(System.IO.Path.Combine(Path, pair.Key), pair.Value);
            }

            return _index++;
        }

        protected override void OnExportStart()
        {
            _keys.AddRange(GetKeys());
            _index = 0;
        }

        private List<string> GetKeys()
        {
            return Data.Keys.ToList();
        }

        private IDictionary<string, byte[]> Export(AnimationModel animation)
        {
            var dict = new Dictionary<string, byte[]>()
            {
                [$"{animation.Key}.PAN"] = GetLegacyFile(animation),
            };
            return dict;
        }

        #region Header

        public byte[] GetLegacyFile(AnimationModel animation)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            //prefix PANI
            writer.Write(new byte[] { 0x50, 0x41, 0x4E, 0x49 });

            //version, image format
            writer.Write((byte)0x03);
            writer.Write((byte)(animation.Data.ImageFormat == AnimationModel.ImageFormat.Raw ? 0x00 : 0x01));

            //colour block flag, kind and block
            //Note: anything but the embedded 17-byte block is not used in retail game versions but supported by the legacy game engine
            switch (animation.Data.ColorMappingType)
            {
                case AnimationModel.ColorMappingType.None:
                    writer.Write((byte)0x00);
                    break;
                case AnimationModel.ColorMappingType.Embedded:
                    writer.Write((byte)0x01);
                    writer.Write((byte)0x00);
                    for (byte i = 0; i < 16; i++)
                    {
                        if (!animation.Data.ColorMapping.TryGetValue(i, out var col))
                        {
                            //legacy files store 3 in entry 0 and map colour 5 to 0
                            col = i == 0 ? (byte)3 : i == 5 ? (byte)0 : i;
                        }
                        writer.Write(col);
                    }
                    writer.Write(animation.Data.BorderColor);
                    break;
                case AnimationModel.ColorMappingType.Previous:
                    writer.Write((byte)0x01);
                    writer.Write((byte)0x01);
                    break;
                case AnimationModel.ColorMappingType.Palette:
                    writer.Write((byte)0x01);
                    writer.Write((byte)0x02);
                    var palette = new byte[PaletteBlockLength];
                    Array.Copy(animation.Data.PaletteData, palette, Math.Min(palette.Length, animation.Data.PaletteData.Length));
                    writer.Write(palette);
                    break;
                default:
                    throw new Exception($"Unhandled colour mapping type: {animation.Data.ColorMappingType}");
            }

            //initial data
            writer.Write((ushort)animation.Data.PositionX);
            writer.Write((ushort)animation.Data.PositionY);
            writer.Write((ushort)animation.Data.BoundingWidth);
            writer.Write((ushort)animation.Data.BoundingHeight);
            writer.Write((ushort)animation.Data.FrameDelay);
            writer.Write((byte)animation.Data.BackgroundType);

            //for ClearToImage, the background image is encoded before the indexing data
            if (animation.Data.BackgroundType == AnimationModel.BackgroundType.ClearToImage)
            {
                WriteImage(writer, animation, animation.Images[-1]);
            }

            //for ClearToColor, right before indexing data there's two bytes
            if (animation.Data.BackgroundType == AnimationModel.BackgroundType.ClearToColor)
            {
                writer.Write((byte)animation.Data.ClearColor);
                writer.Write((byte)animation.Data.Unknown2);
            }

            //indexing data is 250 pairs of bytes, each pair corresponds to either an image or a gap
            //this maps the image indexes (the # of the image in the file) to IDs (the number used in control data)
            //the actual value of the bytes is unknown but likely authoring data
            for (var i = 0; i < ImageTableLength; i++)
            {
                if (!animation.Data.ImageIdToIndex.TryGetValue(i, out var index))
                {
                    //it's a gap
                    writer.Write((ushort)0);
                    continue;
                }

                if (!animation.Data.ImageIndexToUnknownData.TryGetValue(index, out var unknownData))
                {
                    unknownData = 1000 + i; //the data is unknown and not used
                }
                writer.Write((ushort)unknownData);
            }

            //now each image is added to the file sequentially, aligned to 2 bytes
            var imageIndices = animation.Images.Keys
                .Where(x => x >= 0)
                .OrderBy(x => x)
                .ToList();
            foreach (var imageIndex in imageIndices)
            {
                WriteImage(writer, animation, animation.Images[imageIndex]);
            }

            //after the last image, there's a ushort same size as the data section / 16
            var dataSection = GetDataSection(animation);
            writer.Write((ushort)Math.Ceiling((float)dataSection.Length/16));
            //then we write the actual data section, aligned to 16 bytes
            writer.Write(dataSection);
            var paddingLength = 16 - dataSection.Length % 16;
            if (paddingLength != 16)
            {
                for (var i = 0; i < paddingLength; i++)
                {
                    writer.Write((byte)0);
                }
            }

            return memStream.ToArray();
        }

        #endregion

        #region Images

        private void WriteImage(BinaryWriter writer, AnimationModel animation, SharedImageModel image)
        {
            var imageData = animation.Data.ImageFormat == AnimationModel.ImageFormat.Raw
                ? GetRawImageData(image)
                : _imageExporter.GetLegacyFileData(image);
            writer.Write(imageData);
            //images are aligned to 2 bytes
            if (imageData.Length % 2 == 1)
            {
                writer.Write((byte)0);
            }
        }

        /// <summary>
        /// Raw images share the 3-word header with the compressed format but store one byte per pixel
        /// Note: not used in retail game versions but supported by the legacy game engine
        /// </summary>
        private static byte[] GetRawImageData(SharedImageModel image)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);
            writer.Write((ushort)0);
            writer.Write((ushort)image.Data.Width);
            writer.Write((ushort)image.Data.Height);
            writer.Write(image.RawVgaImageData, 0, image.Data.Width * image.Data.Height);
            return memStream.ToArray();
        }

        #endregion

        #region Control data

        private static byte[] GetDataSection(AnimationModel animation)
        {
            using var dataSectionStream = new MemoryStream();
            using var dataSectionWriter = new BinaryWriter(dataSectionStream);
            //the data section is split into two: instructions and steps
            //instructions are instructions for a stack-based VM with opcode prefix with a single executing head
            //some instructions reference both data and instruction labels
            //steps are instructions for a simple VM (no branching) that each sprite runs while simulating
            //some steps reference data labels
            //labels are pointers into the data section (both instruction and data)
            //because the pointers are byte-based, but the instruction/steps in memory are not,
            //there must be a two-pass approach to turn labels into offsets first, then actually write the bytes
            var instructionIndexToOffset = new Dictionary<int, long>();
            var stepIndexToOffset = new Dictionary<int, long>();
            var offset = 0;
            for (var i = 0; i < animation.Control.Instructions.Count; i++)
            {
                instructionIndexToOffset[i] = offset;
                offset += GetInstructionLength(animation.Control.Instructions[i]);
            }

            for (var i = 0; i < animation.Control.Steps.Count; i++)
            {
                stepIndexToOffset[i] = offset;
                offset += GetStepLength(animation.Control.Steps[i]);
            }

            long GetInstructionOffset(string label)
            {
                if (!animation.Control.InstructionLabels.TryGetValue(label, out var index))
                {
                    throw new Exception($"Missing label {label} when processing {animation.Key}");
                }

                return instructionIndexToOffset[index];
            }
            long GetStepOffset(string label)
            {
                if (!animation.Control.StepLabels.TryGetValue(label, out var index))
                {
                    throw new Exception($"Missing data label {label} when processing {animation.Key}");
                }

                return stepIndexToOffset[index];
            }
            void WriteOffset(long value)
            {
                dataSectionWriter.Write(new[] { (byte)(value & 0xFF), (byte)((value & 0xFF00) >> 8) });
            }
            void WritePush(short value)
            {
                dataSectionWriter.Write(new[] { (byte)0x05, (byte)0x00, (byte)(value & 0xFF), (byte)((value & 0xFF00) >> 8) });
            }

            //now we know the offsets for each instruction/step, so we can write the actual data out
            foreach (var instruction in animation.Control.Instructions)
            {
                switch (instruction.Opcode)
                {
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Comment:
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.RawByte:
                        dataSectionWriter.Write((byte)instruction.Data[0]);
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.RawShort:
                        dataSectionWriter.Write((byte)instruction.Data[0]);
                        dataSectionWriter.Write((byte)instruction.Data[1]);
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.RawLabel:
                        WriteOffset(GetInstructionOffset(instruction.Label));
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.RawDataLabel:
                        WriteOffset(GetStepOffset(instruction.StepLabel));
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.PushCopyOfStackValue:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.End:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.EndImmediate:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Return:
                        dataSectionWriter.Write((byte)instruction.Opcode);
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.PopStackToRegister:
                        dataSectionWriter.Write((byte)instruction.Opcode);
                        dataSectionWriter.Write(new [] { instruction.Data[0], instruction.Data[1] });
                        break;
                    //Note: Call (and Return above) are not used in retail game versions but supported by the legacy game engine
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Jump:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.ConditionalJump:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Call:
                        dataSectionWriter.Write((byte)instruction.Opcode);
                        WriteOffset(GetInstructionOffset(instruction.Label));
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.PushToStack:
                        //a labelled push carries a step pointer instead of a literal
                        //Note: not used in retail game versions but supported by the legacy game engine
                        if (!string.IsNullOrEmpty(instruction.StepLabel))
                        {
                            WritePush((short)GetStepOffset(instruction.StepLabel));
                        }
                        else
                        {
                            dataSectionWriter.Write(new [] { (byte)0x05, (byte)0x00, instruction.Data[0], instruction.Data[1] });
                        }
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.PushRegisterToStack:
                        dataSectionWriter.Write(new [] { (byte)0x05, (byte)0x01, instruction.Data[0], instruction.Data[1] });
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.RemoveSprite:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.WaitForFrames:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.TriggerAudio:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.StampSprite:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareEqual:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareLessThan:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareNotEqual:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareGreaterThan:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareGreaterOrEqual:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareLessOrEqual:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Add:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Subtract:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Multiply:
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Divide:
                        foreach (var parameter in instruction.StackParameters)
                        {
                            WritePush(parameter);
                        }
                        dataSectionWriter.Write((byte)instruction.Opcode);
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite:
                        //the step pointer is pushed first, then the parameters, unless everything is already on the stack
                        //Note: the bare form is not used in retail game versions but supported by the legacy game engine
                        if (!string.IsNullOrEmpty(instruction.StepLabel))
                        {
                            if (instruction.StackParameters.Length != 6)
                            {
                                throw new Exception($"SetupSprite needs 6 parameters when processing {animation.Key}");
                            }
                            WritePush((short)GetStepOffset(instruction.StepLabel));
                        }
                        foreach (var parameter in instruction.StackParameters)
                        {
                            WritePush(parameter);
                        }
                        dataSectionWriter.Write((byte)instruction.Opcode);
                        break;
                    default:
                        throw new Exception($"Unhandled opcode: {instruction.Opcode}");
                }
            }

            foreach (var step in animation.Control.Steps)
            {
                if (step.Type == AnimationModel.AnimationStep.StepType.Comment)
                {
                    continue;
                }

                switch (step.Type)
                {
                    case AnimationModel.AnimationStep.StepType.RawByte:
                        dataSectionWriter.Write((byte)step.Data[0]);
                        break;
                    case AnimationModel.AnimationStep.StepType.RawShort:
                        dataSectionWriter.Write(new[] {step.Data[0], step.Data[1]});
                        break;
                    case AnimationModel.AnimationStep.StepType.RawLabel:
                        WriteOffset(GetStepOffset(step.StepLabel));
                        break;
                    case AnimationModel.AnimationStep.StepType.Loop:
                    case AnimationModel.AnimationStep.StepType.Pause:
                    case AnimationModel.AnimationStep.StepType.Restart:
                    case AnimationModel.AnimationStep.StepType.Stop:
                        dataSectionWriter.Write((byte)step.Type);
                        break;
                    case AnimationModel.AnimationStep.StepType.DrawFrame:
                        dataSectionWriter.Write((byte)step.Type);
                        dataSectionWriter.Write((byte)step.Data[0]);
                        break;
                    case AnimationModel.AnimationStep.StepType.SetSpeed:
                    case AnimationModel.AnimationStep.StepType.AddSpeed:
                    case AnimationModel.AnimationStep.StepType.PushCounter:
                        dataSectionWriter.Write((byte)step.Type);
                        dataSectionWriter.Write(new[] {step.Data[0], step.Data[1]});
                        break;
                    case AnimationModel.AnimationStep.StepType.JumpIfCounter:
                        dataSectionWriter.Write((byte)step.Type);
                        WriteOffset(GetStepOffset(step.StepLabel));
                        break;
                    case AnimationModel.AnimationStep.StepType.MoveAbsolute:
                    case AnimationModel.AnimationStep.StepType.MoveRelative:
                        dataSectionWriter.Write((byte)step.Type);
                        dataSectionWriter.Write(new[]
                        {
                            step.Data[0], step.Data[1],
                            step.Data[2], step.Data[3]
                        });
                        break;
                    default:
                        throw new Exception($"Unhandled type: {step.Type}");
                }
            }

            return dataSectionStream.ToArray();
        }

        private static int GetInstructionLength(AnimationModel.AnimationInstruction instruction)
        {
            switch (instruction.Opcode)
            {
                case AnimationModel.AnimationInstruction.AnimationOpcode.Comment:
                    return 0;
                case AnimationModel.AnimationInstruction.AnimationOpcode.RawByte:
                    return 1;
                case AnimationModel.AnimationInstruction.AnimationOpcode.RawShort:
                case AnimationModel.AnimationInstruction.AnimationOpcode.RawLabel:
                case AnimationModel.AnimationInstruction.AnimationOpcode.RawDataLabel:
                    return 2;
                case AnimationModel.AnimationInstruction.AnimationOpcode.PushCopyOfStackValue:
                case AnimationModel.AnimationInstruction.AnimationOpcode.End:
                case AnimationModel.AnimationInstruction.AnimationOpcode.EndImmediate:
                case AnimationModel.AnimationInstruction.AnimationOpcode.Return:
                    return 1;
                case AnimationModel.AnimationInstruction.AnimationOpcode.PopStackToRegister:
                case AnimationModel.AnimationInstruction.AnimationOpcode.Jump:
                case AnimationModel.AnimationInstruction.AnimationOpcode.ConditionalJump:
                case AnimationModel.AnimationInstruction.AnimationOpcode.Call:
                    return 3;
                case AnimationModel.AnimationInstruction.AnimationOpcode.PushToStack:
                case AnimationModel.AnimationInstruction.AnimationOpcode.PushRegisterToStack:
                    return 4;
                case AnimationModel.AnimationInstruction.AnimationOpcode.RemoveSprite:
                case AnimationModel.AnimationInstruction.AnimationOpcode.WaitForFrames:
                case AnimationModel.AnimationInstruction.AnimationOpcode.TriggerAudio:
                case AnimationModel.AnimationInstruction.AnimationOpcode.StampSprite:
                case AnimationModel.AnimationInstruction.AnimationOpcode.CompareEqual:
                case AnimationModel.AnimationInstruction.AnimationOpcode.CompareLessThan:
                case AnimationModel.AnimationInstruction.AnimationOpcode.CompareNotEqual:
                case AnimationModel.AnimationInstruction.AnimationOpcode.CompareGreaterThan:
                case AnimationModel.AnimationInstruction.AnimationOpcode.CompareGreaterOrEqual:
                case AnimationModel.AnimationInstruction.AnimationOpcode.CompareLessOrEqual:
                case AnimationModel.AnimationInstruction.AnimationOpcode.Subtract:
                case AnimationModel.AnimationInstruction.AnimationOpcode.Multiply:
                case AnimationModel.AnimationInstruction.AnimationOpcode.Divide:
                case AnimationModel.AnimationInstruction.AnimationOpcode.Add:
                    return 1 + 4 * instruction.StackParameters.Length;
                case AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite:
                    return 1 + 4 * instruction.StackParameters.Length + (string.IsNullOrEmpty(instruction.StepLabel) ? 0 : 4);
                default:
                    throw new Exception($"Unhandled opcode: {instruction.Opcode}");
            }
        }

        private static int GetStepLength(AnimationModel.AnimationStep step)
        {
            switch (step.Type)
            {
                case AnimationModel.AnimationStep.StepType.Comment:
                    return 0;
                case AnimationModel.AnimationStep.StepType.RawByte:
                    return 1;
                case AnimationModel.AnimationStep.StepType.RawShort:
                case AnimationModel.AnimationStep.StepType.RawLabel:
                    return 2;
                case AnimationModel.AnimationStep.StepType.Loop:
                case AnimationModel.AnimationStep.StepType.Pause:
                case AnimationModel.AnimationStep.StepType.Restart:
                case AnimationModel.AnimationStep.StepType.Stop:
                    return 1;
                case AnimationModel.AnimationStep.StepType.DrawFrame:
                    return 2;
                case AnimationModel.AnimationStep.StepType.SetSpeed:
                case AnimationModel.AnimationStep.StepType.AddSpeed:
                case AnimationModel.AnimationStep.StepType.PushCounter:
                case AnimationModel.AnimationStep.StepType.JumpIfCounter:
                    return 3;
                case AnimationModel.AnimationStep.StepType.MoveAbsolute:
                case AnimationModel.AnimationStep.StepType.MoveRelative:
                    return 5;
                default:
                    throw new Exception($"Unhandled type: {step.Type}");
            }
        }

        #endregion
    }
}
