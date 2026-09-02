using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    internal class LegacyAnimationParser : BaseImporter<Dictionary<string, AnimationModel>>, ILegacyParser
    {
        private const int ColorBlockLength = 17;
        private const int PaletteBlockLength = 774;
        private const int ImageTableLength = 250;

        private readonly ILogger<LegacyAnimationParser> _logger;
        private readonly SharedImageParser _imageParser;

        private readonly List<string> _keys = new();
        private readonly Dictionary<string, AnimationModel> _result = new Dictionary<string, AnimationModel>();

        private int _index = 0;

        public LegacyAnimationParser(ILogger<LegacyAnimationParser> logger, SharedImageParser imageParser)
        {
            _logger = logger;
            _imageParser = imageParser;
        }

        protected override string Message => "Processing animations..";

        public override void SetResult(PackageModel model)
        {
            model.Animations = GetResult();
        }

        protected override bool CheckIfValidForImportInternal(string path)
        {
            if (Directory.GetFiles(path, "*.PAN").Length == 0)
            {
                return false;
            }

            return true;
        }

        protected override int GetTotalItemCountInPath()
        {
            return _keys.Count;
        }

        protected override int RunImportStepInternal()
        {
            var nextKey = _keys[_index];

            _result[nextKey] = Parse(Path, nextKey);

            return _index++;
        }

        protected override Dictionary<string, AnimationModel> GetResultInternal()
        {
            return _result;
        }

        protected override void OnImportStart()
        {
            _keys.AddRange(GetKeys(Path));
            _index = 0;
        }

        private List<string> GetKeys(string path)
        {
            return Directory.GetFiles(path, "*.PAN")
                .Select(System.IO.Path.GetFileNameWithoutExtension)
                .ToList();
        }

        private AnimationModel Parse(string path, string key)
        {
            var filePath = System.IO.Path.Combine(path, $"{key}.PAN");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing PAN file: {key}");
            }

            var rawData = File.ReadAllBytes(filePath);
            var model = ParseAnimation(key, rawData);
            return model;
        }

        #region Header

        private AnimationModel ParseAnimation(string key, byte[] rawData)
        {
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);

            var prefix = reader.ReadBytes(4);
            if (prefix[0] != 0x50 || prefix[1] != 0x41 || prefix[2] != 0x4E || prefix[3] != 0x49)
            {
                throw new Exception($"Unexpected prefix: {string.Join(" ", prefix.Select(x => $"{x:X2}"))}");
            }

            //the game refuses any other version
            var version = reader.ReadByte();
            if (version != 0x03)
            {
                throw new Exception($"Unsupported version: {version:X2}");
            }

            var imageFormat = reader.ReadByte() == 0
                ? AnimationModel.ImageFormat.Raw
                : AnimationModel.ImageFormat.Compressed;

            //the colour block is optional, and its kind byte only exists when the flag is set
            //Note: anything but the embedded 17-byte block is not used in retail game versions but supported by the legacy game engine
            var colorMappingType = AnimationModel.ColorMappingType.None;
            var colorMapping = new Dictionary<byte, byte>();
            byte borderColor = 0;
            var paletteData = Array.Empty<byte>();
            if (reader.ReadByte() != 0)
            {
                var kind = reader.ReadByte();
                switch (kind)
                {
                    case 0x00:
                        colorMappingType = AnimationModel.ColorMappingType.Embedded;
                        for (byte i = 0; i < 16; i++)
                        {
                            colorMapping[i] = reader.ReadByte();
                        }
                        borderColor = reader.ReadByte();
                        break;
                    case 0x02:
                        colorMappingType = AnimationModel.ColorMappingType.Palette;
                        paletteData = reader.ReadBytes(PaletteBlockLength);
                        break;
                    default:
                        colorMappingType = AnimationModel.ColorMappingType.Previous;
                        if (kind != 0x01)
                        {
                            _logger.LogWarning($"Unexpected colour block kind for {key}: {kind:X2}");
                        }
                        break;
                }
            }

            var positionX = reader.ReadUInt16();
            var positionY = reader.ReadUInt16();
            var aWidth = reader.ReadUInt16(); //width - 1
            var aHeight = reader.ReadUInt16(); //height - 1
            var frameDelay = reader.ReadUInt16();
            var backgroundType = (AnimationModel.BackgroundType)reader.ReadByte();

            //for ClearToImage, there is an image before the index table, otherwise it's straight to the index table
            var images = new Dictionary<int, SharedImageModel>();
            if (backgroundType == AnimationModel.BackgroundType.ClearToImage)
            {
                var image = ReadImage(reader, memStream, key, -1, imageFormat);
                if (image == null)
                {
                    throw new Exception("Missing first image");
                }

                images[-1] = image;
            }

            byte clearColor = 0;
            byte unknown2 = 0;
            if (backgroundType == AnimationModel.BackgroundType.ClearToColor)
            {
                clearColor = reader.ReadByte();
                unknown2 = reader.ReadByte();
            }

            //the index table is 250 pairs of bytes, the entry number corresponds to the image in the file, 00 00 represents a skipped ID
            var imageIdx = 0;
            var imageIdToIndex = new Dictionary<int, int>();
            var imageIndexToUnknownData = new Dictionary<int, int>();
            for (var imageId = 0; imageId < ImageTableLength; imageId++)
            {
                var data = reader.ReadUInt16();
                if (data == 0)
                {
                    //it's a gap
                    continue;
                }

                imageIdToIndex[imageId] = imageIdx;
                imageIndexToUnknownData[imageIdx] = data;
                imageIdx++;
            }

            //the number of images is determined from the previous list
            for (var img = 0; img < imageIdToIndex.Count; img++)
            {
                var image = ReadImage(reader, memStream, key, img, imageFormat);
                if (image == null)
                {
                    throw new Exception("Unparseable image");
                }

                images[img] = image;
            }

            //the data section is prefixed by its size in 16-byte paragraphs, anything beyond it is ignored by the game
            var dataSectionLength = reader.ReadUInt16() * 16;
            var dataSectionStart = (int)memStream.Position;
            var availableLength = rawData.Length - dataSectionStart;
            if (availableLength != dataSectionLength)
            {
                _logger.LogWarning($"Data section for {key} declares {dataSectionLength} bytes but {availableLength} remain");
            }

            var dataSection = new byte[Math.Min(dataSectionLength, availableLength)];
            Array.Copy(rawData, dataSectionStart, dataSection, 0, dataSection.Length);
            var control = ParseControlData(dataSection);

            var model = new AnimationModel()
            {
                Key = key,
                Images = images,
                Data = new AnimationModel.GlobalData()
                {
                    FrameDelay = frameDelay,
                    BackgroundType = backgroundType,
                    ImageFormat = imageFormat,
                    ColorMappingType = colorMappingType,
                    ColorMapping = colorMapping,
                    BorderColor = borderColor,
                    PaletteData = paletteData,
                    PositionX = positionX,
                    PositionY = positionY,
                    BoundingWidth = aWidth,
                    BoundingHeight = aHeight,
                    ClearColor = clearColor,
                    Unknown2 = unknown2,
                    ImageIdToIndex = imageIdToIndex,
                    ImageIndexToUnknownData = imageIndexToUnknownData,
                },
                Control = control,
                Metadata = new SharedMetadata()
                {
                    Name = key,
                    Comment = "Legacy import"
                }
            };
            return model;
        }

        #endregion

        #region Images

        private SharedImageModel? ReadImage(BinaryReader reader, MemoryStream memStream, string key, int img, AnimationModel.ImageFormat imageFormat)
        {
            //because we don't do piece-meal parsing, we have to read a ton of extra bytes and pass them over first
            //but parsing the image will return the actual offset
            SharedImageModel? model = null;
            try
            {
                var startOffset = memStream.Position;
                model = imageFormat == AnimationModel.ImageFormat.Raw
                    ? ReadRawImage(reader, $"{key}_{img}")
                    : _imageParser.Parse($"{key}_{img}", reader);
                if ((memStream.Position - startOffset) % 2 == 1)
                {
                    reader.ReadByte(); //it's padded to 2 bytes
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Failed to parse image {key} {img}: {e}");
            }

            return model;
        }

        /// <summary>
        /// Raw images share the 3-word header with the compressed format but store one byte per pixel
        /// Note: not used in retail game versions but supported by the legacy game engine
        /// </summary>
        private static SharedImageModel ReadRawImage(BinaryReader reader, string key)
        {
            reader.ReadUInt16(); //format flag, ignored by the game
            var width = reader.ReadUInt16();
            var height = reader.ReadUInt16();
            var pixels = reader.ReadBytes(width * height);
            if (pixels.Length != width * height)
            {
                throw new Exception($"Truncated raw image {key}");
            }

            //only the low nibble is a colour index
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] &= 0x0F;
            }

            return new SharedImageModel()
            {
                RawVgaImageData = pixels,
                VgaImageData = ImageConversion.VgaToTexture(width, height, pixels),
                CgaImageData = Array.Empty<byte>(),
                Data = new SharedImageModel.ImageData()
                {
                    Type = Constants.GetLikelyImageType(key),
                    Width = width,
                    Height = height,
                    LegacyColorMappings = null,
                    CompressionDictionaryWidth = 11
                }
            };
        }

        #endregion

        #region Control data

        private class RawInstruction
        {
            public int Offset;
            public int Length;
            public byte Opcode;
            public byte Sub;
            public short Value;
            public int Target => (ushort)Value;
        }

        private class RawStep
        {
            public int Offset;
            public int Length;
            public byte Type;
            public byte[] Data = Array.Empty<byte>();
            public int Target => Data[0] | (Data[1] << 8);
        }

        private class InstructionEntry
        {
            public int Offset;
            public int Length;
            public AnimationModel.AnimationInstruction Instruction = new();
        }

        /// <summary>
        /// The data section has no fixed layout: instructions are whatever is reachable from offset 0,
        /// and steps are whatever is reachable from the step pointers those instructions push.
        /// </summary>
        private AnimationModel.ControlData ParseControlData(byte[] data)
        {
            var decodedInstructions = new SortedDictionary<int, RawInstruction>();
            var jumpTargets = new HashSet<int>();
            var pending = new Stack<int>();
            pending.Push(0);
            while (pending.Count > 0)
            {
                var offset = pending.Pop();
                if (decodedInstructions.ContainsKey(offset))
                {
                    continue;
                }

                var raw = DecodeInstruction(data, offset);
                decodedInstructions[offset] = raw;
                switch (raw.Opcode)
                {
                    case 0x12:
                    case 0x17:
                        jumpTargets.Add(raw.Target);
                        pending.Push(raw.Target);
                        pending.Push(offset + raw.Length);
                        break;
                    case 0x13:
                        jumpTargets.Add(raw.Target);
                        pending.Push(raw.Target);
                        break;
                    case 0x14:
                    case 0x15:
                    case 0x16:
                        break;
                    default:
                        pending.Push(offset + raw.Length);
                        break;
                }
            }

            //jump targets become labels, and step pointers become data labels, in offset order
            var instructionLabels = new Dictionary<int, string>();
            var instructionLabelId = 1;
            var dataLabels = new Dictionary<int, string>();
            var dataLabelId = 1;
            string GetInstructionLabel(int target)
            {
                if (!instructionLabels.TryGetValue(target, out var label))
                {
                    label = $"LABEL_{instructionLabelId++}";
                    instructionLabels[target] = label;
                }

                return label;
            }
            string GetDataLabel(int target, string prefix)
            {
                if (!dataLabels.TryGetValue(target, out var label))
                {
                    label = $"{prefix}_{dataLabelId++}";
                    dataLabels[target] = label;
                }

                return label;
            }

            var entries = new List<InstructionEntry>();
            foreach (var raw in decodedInstructions.Values)
            {
                var instruction = new AnimationModel.AnimationInstruction();
                var entryOffset = raw.Offset;
                var pops = 0;
                switch (raw.Opcode)
                {
                    case 0x00:
                        instruction.Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite;
                        pops = 7;
                        break;
                    case 0x01:
                        instruction.Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.RemoveSprite;
                        pops = 1;
                        break;
                    case 0x02:
                        instruction.Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.WaitForFrames;
                        pops = 1;
                        break;
                    case 0x03:
                        instruction.Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.TriggerAudio;
                        pops = 1;
                        break;
                    case 0x04:
                        instruction.Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.StampSprite;
                        pops = 1;
                        break;
                    case 0x05:
                        instruction.Opcode = raw.Sub == 0
                            ? AnimationModel.AnimationInstruction.AnimationOpcode.PushToStack
                            : AnimationModel.AnimationInstruction.AnimationOpcode.PushRegisterToStack;
                        instruction.Data = new[] { (byte)(raw.Value & 0xFF), (byte)((raw.Value >> 8) & 0xFF) };
                        break;
                    case 0x06:
                        instruction.Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.PopStackToRegister;
                        instruction.Data = new[] { (byte)(raw.Value & 0xFF), (byte)((raw.Value >> 8) & 0xFF) };
                        break;
                    case 0x07:
                        instruction.Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.PushCopyOfStackValue;
                        break;
                    case 0x08:
                    case 0x09:
                    case 0x0A:
                    case 0x0B:
                    case 0x0C:
                    case 0x0D:
                    case 0x0E:
                    case 0x0F:
                    case 0x10:
                    case 0x11:
                        instruction.Opcode = (AnimationModel.AnimationInstruction.AnimationOpcode)raw.Opcode;
                        pops = 1;
                        break;
                    case 0x12:
                        instruction.Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.ConditionalJump;
                        instruction.Label = GetInstructionLabel(raw.Target);
                        break;
                    case 0x13:
                        instruction.Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.Jump;
                        instruction.Label = GetInstructionLabel(raw.Target);
                        break;
                    case 0x14:
                        instruction.Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.End;
                        break;
                    case 0x15:
                        instruction.Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.EndImmediate;
                        break;
                    //Note: Return and Call are not used in retail game versions but supported by the legacy game engine
                    case 0x16:
                        instruction.Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.Return;
                        break;
                    case 0x17:
                        instruction.Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.Call;
                        instruction.Label = GetInstructionLabel(raw.Target);
                        break;
                    default:
                        throw new Exception($"Unknown instruction: {raw.Opcode:X2} at offset {raw.Offset:X4}");
                }

                //literal pushes directly before a consuming instruction are folded into it as parameters
                if (pops > 0 && TryFoldPushes(entries, pops, raw.Offset, jumpTargets, out var values, out var foldedOffset))
                {
                    if (instruction.Opcode == AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite)
                    {
                        instruction.StepLabel = GetDataLabel((ushort)values[0], "START");
                        values.RemoveAt(0);
                    }

                    instruction.StackParameters = values.ToArray();
                    entryOffset = foldedOffset;
                }
                else if (instruction.Opcode == AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite)
                {
                    //the step pointer is still needed, so the push that produced it becomes a labelled push
                    //Note: computed parameters are not used in retail game versions but supported by the legacy game engine
                    var producerIndex = FindProducer(entries, raw.Offset, 7);
                    if (producerIndex < 0 ||
                        entries[producerIndex].Instruction.Opcode != AnimationModel.AnimationInstruction.AnimationOpcode.PushToStack ||
                        !string.IsNullOrEmpty(entries[producerIndex].Instruction.StepLabel))
                    {
                        throw new Exception($"Unsupported SetupSprite step pointer at offset {raw.Offset:X4}");
                    }

                    var producer = entries[producerIndex].Instruction;
                    var pointer = producer.Data[0] | (producer.Data[1] << 8);
                    producer.StepLabel = GetDataLabel(pointer, "START");
                    producer.Data = Array.Empty<byte>();
                }

                entries.Add(new InstructionEntry()
                {
                    Offset = entryOffset,
                    Length = raw.Offset + raw.Length - entryOffset,
                    Instruction = instruction
                });
            }

            var listInstructions = entries.Select(x => x.Instruction).ToList();
            var listLabels = new Dictionary<string, int>();
            foreach (var pair in instructionLabels)
            {
                var index = entries.FindIndex(x => x.Offset == pair.Key);
                if (index < 0)
                {
                    throw new Exception($"Jump target {pair.Key:X4} is not an instruction");
                }

                listLabels[pair.Value] = index;
            }

            //steps are reachable from the step pointers, and from counter jumps within those sequences
            var decodedSteps = new SortedDictionary<int, RawStep>();
            foreach (var start in dataLabels.Keys)
            {
                pending.Push(start);
            }
            while (pending.Count > 0)
            {
                var offset = pending.Pop();
                if (decodedSteps.ContainsKey(offset))
                {
                    continue;
                }

                var raw = DecodeStep(data, offset);
                decodedSteps[offset] = raw;
                switch ((AnimationModel.AnimationStep.StepType)raw.Type)
                {
                    case AnimationModel.AnimationStep.StepType.JumpIfCounter:
                        pending.Push(raw.Target);
                        pending.Push(offset + raw.Length);
                        break;
                    case AnimationModel.AnimationStep.StepType.Restart:
                    case AnimationModel.AnimationStep.StepType.Loop:
                    case AnimationModel.AnimationStep.StepType.Pause:
                    case AnimationModel.AnimationStep.StepType.Stop:
                        break;
                    default:
                        pending.Push(offset + raw.Length);
                        break;
                }
            }

            var spriteStarts = listInstructions
                .Where(x => x.Opcode == AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite &&
                            x.StackParameters.Length == 6)
                .GroupBy(x => x.StepLabel)
                .ToDictionary(x => x.Key, x => x.Select(y => y.StackParameters[0]).Distinct().OrderBy(y => y).ToList());
            var listSteps = new List<AnimationModel.AnimationStep>();
            var stepOffsets = new List<int>();
            foreach (var raw in decodedSteps.Values)
            {
                var step = new AnimationModel.AnimationStep()
                {
                    Type = (AnimationModel.AnimationStep.StepType)raw.Type,
                    Data = raw.Data
                };
                if (step.Type == AnimationModel.AnimationStep.StepType.JumpIfCounter)
                {
                    step.StepLabel = GetDataLabel(raw.Target, "DATA");
                    step.Data = Array.Empty<byte>();
                }

                if (dataLabels.TryGetValue(raw.Offset, out var startLabel) && spriteStarts.TryGetValue(startLabel, out var sprites))
                {
                    step.Comment = $"Sprites start: {string.Join(", ", sprites)}";
                }

                stepOffsets.Add(raw.Offset);
                listSteps.Add(step);
            }

            var listDataLabels = new Dictionary<string, int>();
            foreach (var pair in dataLabels)
            {
                var index = stepOffsets.IndexOf(pair.Key);
                if (index < 0)
                {
                    throw new Exception($"Step target {pair.Key:X4} is not a step");
                }

                listDataLabels[pair.Value] = index;
            }

            return new AnimationModel.ControlData()
            {
                Instructions = listInstructions,
                InstructionLabels = listLabels,
                Steps = listSteps,
                StepLabels = listDataLabels
            };
        }

        private static RawInstruction DecodeInstruction(byte[] data, int offset)
        {
            if (offset < 0 || offset >= data.Length)
            {
                throw new Exception($"Instruction offset out of range: {offset:X4}");
            }

            var opcode = data[offset];
            var length = opcode switch
            {
                0x05 => 4,
                0x06 or 0x12 or 0x13 or 0x17 => 3,
                <= 0x17 => 1,
                _ => throw new Exception($"Unknown instruction: {opcode:X2} at offset {offset:X4}")
            };
            if (offset + length > data.Length)
            {
                throw new Exception($"Truncated instruction at offset {offset:X4}");
            }

            var valueOffset = length == 4 ? offset + 2 : offset + 1;
            return new RawInstruction()
            {
                Offset = offset,
                Length = length,
                Opcode = opcode,
                Sub = length == 4 ? data[offset + 1] : (byte)0,
                Value = length > 1 ? (short)(data[valueOffset] | (data[valueOffset + 1] << 8)) : (short)0
            };
        }

        private static RawStep DecodeStep(byte[] data, int offset)
        {
            if (offset < 0 || offset >= data.Length)
            {
                throw new Exception($"Step offset out of range: {offset:X4}");
            }

            var type = data[offset];
            var length = type switch
            {
                0x00 => 2,
                0x01 or 0x02 => 5,
                0x03 or 0x04 or 0x05 or 0x06 => 3,
                <= 0x0A => 1,
                _ => throw new Exception($"Unknown step type: {type:X2} at offset {offset:X4}")
            };
            if (offset + length > data.Length)
            {
                throw new Exception($"Truncated step at offset {offset:X4}");
            }

            var stepData = new byte[length - 1];
            Array.Copy(data, offset + 1, stepData, 0, stepData.Length);
            return new RawStep()
            {
                Offset = offset,
                Length = length,
                Type = type,
                Data = stepData
            };
        }

        /// <summary>
        /// Folds only when the preceding instructions are contiguous literal pushes that nothing jumps into
        /// </summary>
        private static bool TryFoldPushes(List<InstructionEntry> entries, int count, int offset, HashSet<int> jumpTargets, out List<short> values, out int foldedOffset)
        {
            values = new List<short>();
            foldedOffset = offset;
            if (entries.Count < count)
            {
                return false;
            }

            var start = entries.Count - count;
            var expectedEnd = offset;
            for (var i = entries.Count - 1; i >= start; i--)
            {
                var entry = entries[i];
                if (entry.Instruction.Opcode != AnimationModel.AnimationInstruction.AnimationOpcode.PushToStack ||
                    !string.IsNullOrEmpty(entry.Instruction.StepLabel) ||
                    entry.Offset + entry.Length != expectedEnd ||
                    (i != start && jumpTargets.Contains(entry.Offset)))
                {
                    return false;
                }

                expectedEnd = entry.Offset;
            }

            for (var i = start; i < entries.Count; i++)
            {
                var bytes = entries[i].Instruction.Data;
                values.Add((short)(bytes[0] | (bytes[1] << 8)));
            }

            foldedOffset = entries[start].Offset;
            entries.RemoveRange(start, count);
            return true;
        }

        /// <summary>
        /// Walks back through contiguous instructions to find which one pushed the value at the given stack depth
        /// </summary>
        private static int FindProducer(List<InstructionEntry> entries, int offset, int depth)
        {
            var expectedEnd = offset;
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                if (entry.Offset + entry.Length != expectedEnd)
                {
                    return -1;
                }

                expectedEnd = entry.Offset;
                var effect = GetStackEffect(entry.Instruction);
                if (effect == null)
                {
                    return -1;
                }

                var (pops, pushes) = effect.Value;
                if (depth <= pushes)
                {
                    return i;
                }

                depth = depth - pushes + pops;
            }

            return -1;
        }

        private static (int pops, int pushes)? GetStackEffect(AnimationModel.AnimationInstruction instruction)
        {
            var parameters = instruction.StackParameters.Length;
            switch (instruction.Opcode)
            {
                case AnimationModel.AnimationInstruction.AnimationOpcode.PushToStack:
                case AnimationModel.AnimationInstruction.AnimationOpcode.PushRegisterToStack:
                    return (0, 1);
                case AnimationModel.AnimationInstruction.AnimationOpcode.PushCopyOfStackValue:
                    return (1, 2);
                case AnimationModel.AnimationInstruction.AnimationOpcode.PopStackToRegister:
                    return (1, 0);
                case AnimationModel.AnimationInstruction.AnimationOpcode.RemoveSprite:
                case AnimationModel.AnimationInstruction.AnimationOpcode.WaitForFrames:
                case AnimationModel.AnimationInstruction.AnimationOpcode.TriggerAudio:
                case AnimationModel.AnimationInstruction.AnimationOpcode.StampSprite:
                    return (1 - parameters, 0);
                case AnimationModel.AnimationInstruction.AnimationOpcode.CompareEqual:
                case AnimationModel.AnimationInstruction.AnimationOpcode.CompareNotEqual:
                case AnimationModel.AnimationInstruction.AnimationOpcode.CompareGreaterThan:
                case AnimationModel.AnimationInstruction.AnimationOpcode.CompareLessThan:
                case AnimationModel.AnimationInstruction.AnimationOpcode.CompareGreaterOrEqual:
                case AnimationModel.AnimationInstruction.AnimationOpcode.CompareLessOrEqual:
                case AnimationModel.AnimationInstruction.AnimationOpcode.Add:
                case AnimationModel.AnimationInstruction.AnimationOpcode.Subtract:
                case AnimationModel.AnimationInstruction.AnimationOpcode.Multiply:
                case AnimationModel.AnimationInstruction.AnimationOpcode.Divide:
                    return (2 - parameters, 1);
                case AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite:
                    return (string.IsNullOrEmpty(instruction.StepLabel) ? 7 - parameters : 0, 0);
                default:
                    return null;
            }
        }

        #endregion
    }
}
