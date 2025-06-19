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

        private byte[] GetLegacyFile(AnimationModel animation)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);
            
            //prefix PANI
            writer.Write(new byte[] { 0x50, 0x41, 0x4E, 0x49 });
            
            //tag
            //TODO: RRT files have a different tag
            writer.Write(new byte[] { 0x03, 0x01, 0x01, 0x00, 0x03 });
            
            //colour mapping (1-15)
            for (byte i = 1; i < 16; i++)
            {
                if (!animation.Data.ColorMapping.TryGetValue(i, out var col))
                {
                    col = i;
                    if (i == 5)
                    {
                        col = 0; //standard here
                    }
                }
                writer.Write(col);
            }
            
            //5 bytes of padding 00
            writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 });
            
            //initial data
            writer.Write((ushort)animation.Data.BoundingWidth);
            writer.Write((ushort)animation.Data.BoundingHeight);
            writer.Write((ushort)animation.Data.GlobalFrameSkip);
            writer.Write((byte)animation.Data.BackgroundType);
            
            //for ClearToImage, the background image is encoded before the indexing data
            if (animation.Data.BackgroundType == AnimationModel.BackgroundType.ClearToImage)
            {
                var background = animation.Images[-1];
                var backgroundData = _imageExporter.GetLegacyFileData(background);
                writer.Write(backgroundData);
                //images are aligned to 2 bytes
                if (backgroundData.Length % 2 == 1)
                {
                    writer.Write((byte)0);
                }
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
            for (var i = 0; i < 250; i++)
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
                var image = animation.Images[imageIndex];
                var imageData = _imageExporter.GetLegacyFileData(image);
                writer.Write(imageData);
                if (imageData.Length % 2 == 1)
                {
                    writer.Write((byte)0);
                }
            }
            
            //after the last image, there's a ushort same size as the data section / 16
            //therefore we first build the data section
            byte[] dataSection = Array.Empty<byte>();
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
                for (var i = 0; i < animation.Data.Instructions.Count; i++)
                {
                    var instruction = animation.Data.Instructions[i];
                    instructionIndexToOffset[i] = offset;
                    switch (instruction.Opcode)
                    {
                        case AnimationModel.AnimationInstruction.AnimationOpcode.RawByte:
                            offset += 1;
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.RawShort:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.RawLabel:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.RawDataLabel:
                            offset += 2;
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.PushCopyOfStackValue:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.End:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.EndImmediate:
                            offset += 1;
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.PopStackToRegister:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.Jump:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.ConditionalJump:
                            offset += 3;
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.PushToStack:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.PushRegisterToStack:
                            offset += 4;
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
                        case AnimationModel.AnimationInstruction.AnimationOpcode.Subtract:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.Multiply:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.Divide:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.Add:
                            offset += 5;
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite:
                            offset += 29;
                            break;
                        default:
                            throw new Exception($"Unhandled opcode: {instruction.Opcode}");
                    }
                }

                for (var i = 0; i < animation.Data.Steps.Count; i++)
                {
                    var step = animation.Data.Steps[i];
                    stepIndexToOffset[i] = offset;
                    switch (step.Type)
                    {
                        case AnimationModel.AnimationStep.StepType.Loop:
                        case AnimationModel.AnimationStep.StepType.Pause:
                        case AnimationModel.AnimationStep.StepType.Restart:
                        case AnimationModel.AnimationStep.StepType.Stop:
                            offset += 1;
                            break;
                        case AnimationModel.AnimationStep.StepType.DrawFrame:
                            offset += 2;
                            break;
                        case AnimationModel.AnimationStep.StepType.SetFrameSkip:
                        case AnimationModel.AnimationStep.StepType.SetFrameAdjustment:
                        case AnimationModel.AnimationStep.StepType.PushCounter:
                        case AnimationModel.AnimationStep.StepType.JumpIfCounter:
                            offset += 3;
                            break;
                        case AnimationModel.AnimationStep.StepType.MoveAbsolute:
                        case AnimationModel.AnimationStep.StepType.MoveRelative:
                            offset += 5;
                            break;
                        default:
                            throw new Exception($"Unhandled type: {step.Type}");
                    }
                }
                
                //now we know the offsets for each instruction/step, so we can write the actual data out
                for (var i = 0; i < animation.Data.Instructions.Count; i++)
                {
                    var instruction = animation.Data.Instructions[i];
                    switch (instruction.Opcode)
                    {
                        case AnimationModel.AnimationInstruction.AnimationOpcode.RawByte:
                            dataSectionWriter.Write((byte)instruction.Data[0]);
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.RawShort:
                            dataSectionWriter.Write((byte)instruction.Data[0]);
                            dataSectionWriter.Write((byte)instruction.Data[1]);
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.RawLabel:
                            var rawTargetLabel = instruction.Label;
                            var rawTargetIndex = animation.Data.InstructionLabels[rawTargetLabel];
                            var rawTargetOffset = instructionIndexToOffset[rawTargetIndex];
                            dataSectionWriter.Write(new [] { (byte)(rawTargetOffset & 0xFF), (byte)((rawTargetOffset & 0xFF00) >> 8)} );
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.RawDataLabel:
                            var rawTargetDataLabel = instruction.DataLabel;
                            var rawTargetDataIndex = animation.Data.DataLabels[rawTargetDataLabel];
                            var rawTargetDataOffset = stepIndexToOffset[rawTargetDataIndex];
                            dataSectionWriter.Write(new [] { (byte)(rawTargetDataOffset & 0xFF), (byte)((rawTargetDataOffset & 0xFF00) >> 8)} );
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.PushCopyOfStackValue:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.End:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.EndImmediate:
                            dataSectionWriter.Write((byte)instruction.Opcode);
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.PopStackToRegister:
                            dataSectionWriter.Write((byte)instruction.Opcode);
                            dataSectionWriter.Write(new [] { instruction.Data[0], instruction.Data[1] });
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.Jump:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.ConditionalJump:
                            dataSectionWriter.Write((byte)instruction.Opcode);
                            var targetLabel = instruction.Label;
                            var targetIndex = animation.Data.InstructionLabels[targetLabel];
                            var targetOffset = instructionIndexToOffset[targetIndex];
                            dataSectionWriter.Write(new [] { (byte)(targetOffset & 0xFF), (byte)((targetOffset & 0xFF00) >> 8)} );
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.PushToStack:
                            dataSectionWriter.Write(new [] { (byte)0x05, (byte)0x00, instruction.Data[0], instruction.Data[1] });
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
                            dataSectionWriter.Write(new [] { (byte)0x05, (byte)0x00, 
                                (byte)(instruction.StackParameters[0] & 0xFF), 
                                (byte)((instruction.StackParameters[0] & 0xFF00) >> 8) });
                            dataSectionWriter.Write((byte)instruction.Opcode);
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite:
                            var targetDataLabel = instruction.DataLabel;
                            var targetDataIndex = animation.Data.DataLabels[targetDataLabel];
                            var targetDataOffset = stepIndexToOffset[targetDataIndex];
                            dataSectionWriter.Write(new [] { (byte)0x05, (byte)0x00, 
                                (byte)(targetDataOffset & 0xFF), 
                                (byte)((targetDataOffset & 0xFF00) >> 8) });
                            dataSectionWriter.Write(new [] { (byte)0x05, (byte)0x00, 
                                (byte)(instruction.StackParameters[0] & 0xFF), 
                                (byte)((instruction.StackParameters[0] & 0xFF00) >> 8) });
                            dataSectionWriter.Write(new [] { (byte)0x05, (byte)0x00, 
                                (byte)(instruction.StackParameters[1] & 0xFF), 
                                (byte)((instruction.StackParameters[1] & 0xFF00) >> 8) });
                            dataSectionWriter.Write(new [] { (byte)0x05, (byte)0x00, 
                                (byte)(instruction.StackParameters[2] & 0xFF), 
                                (byte)((instruction.StackParameters[2] & 0xFF00) >> 8) });
                            dataSectionWriter.Write(new [] { (byte)0x05, (byte)0x00, 
                                (byte)(instruction.StackParameters[3] & 0xFF), 
                                (byte)((instruction.StackParameters[3] & 0xFF00) >> 8) });
                            dataSectionWriter.Write(new [] { (byte)0x05, (byte)0x00, 
                                (byte)(instruction.StackParameters[4] & 0xFF), 
                                (byte)((instruction.StackParameters[4] & 0xFF00) >> 8) });
                            dataSectionWriter.Write(new [] { (byte)0x05, (byte)0x00, 
                                (byte)(instruction.StackParameters[5] & 0xFF), 
                                (byte)((instruction.StackParameters[5] & 0xFF00) >> 8) });
                            dataSectionWriter.Write((byte)instruction.Opcode);
                            break;
                        default:
                            throw new Exception($"Unhandled opcode: {instruction.Opcode}");
                    }
                }

                for (var i = 0; i < animation.Data.Steps.Count; i++)
                {
                    var step = animation.Data.Steps[i];
                    dataSectionWriter.Write((byte)step.Type);
                    switch (step.Type)
                    {
                        case AnimationModel.AnimationStep.StepType.Loop:
                        case AnimationModel.AnimationStep.StepType.Pause:
                        case AnimationModel.AnimationStep.StepType.Restart:
                        case AnimationModel.AnimationStep.StepType.Stop:
                            break;
                        case AnimationModel.AnimationStep.StepType.DrawFrame:
                            dataSectionWriter.Write((byte)step.Data[0]);
                            break;
                        case AnimationModel.AnimationStep.StepType.SetFrameSkip:
                        case AnimationModel.AnimationStep.StepType.SetFrameAdjustment:
                        case AnimationModel.AnimationStep.StepType.PushCounter:
                            dataSectionWriter.Write(new[] {step.Data[0], step.Data[1]});
                            break;
                        case AnimationModel.AnimationStep.StepType.JumpIfCounter:
                            var targetDataLabel = step.Label;
                            if (!animation.Data.DataLabels.TryGetValue(targetDataLabel, out var targetDataIndex))
                            {
                                throw new Exception($"Missing data label {targetDataLabel} when processing {animation.Key}");
                            }
                            var targetDataOffset = stepIndexToOffset[targetDataIndex];
                            dataSectionWriter.Write(new [] { 
                                (byte)(targetDataOffset & 0xFF), 
                                (byte)((targetDataOffset & 0xFF00) >> 8) });
                            break;
                        case AnimationModel.AnimationStep.StepType.MoveAbsolute:
                        case AnimationModel.AnimationStep.StepType.MoveRelative:
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

                dataSection = dataSectionStream.ToArray();
            }
            
            //we write the size of the data section first
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
    }
}