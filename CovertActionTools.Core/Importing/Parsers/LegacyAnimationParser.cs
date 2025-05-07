using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing.Parsers
{
    public class LegacyAnimationParser : BaseImporter<Dictionary<string, AnimationModel>>
    {
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

        private AnimationModel ParseAnimation(string key, byte[] rawData)
        {
            using var memStream = new MemoryStream(rawData);
            using var reader = new BinaryReader(memStream);

            var prefix = reader.ReadBytes(4);
            if (prefix[0] != 0x50 || prefix[1] != 0x41 || prefix[2] != 0x4E || prefix[3] != 0x49)
            {
                throw new Exception($"Unexpected prefix: {string.Join(" ", prefix.Select(x => $"{x:X2}"))}");
            }

            var tag = reader.ReadBytes(5);
            if (tag[0] != 0x03 || tag[1] != 0x01 || tag[2] != 0x01 || tag[3] != 0x00 || tag[4] != 0x03)
            {
                throw new Exception($"Unexpected tag: {string.Join(" ", tag.Select(x => $"{x:X2}"))}");
            }
            
            var colorMapping = new Dictionary<byte, byte>();
            for (var i = 1; i < 16; i++)
            {
                colorMapping[(byte)i] = reader.ReadByte();
            }

            var padding = reader.ReadBytes(5);
            if (padding.Any(x => x != 0x00))
            {
                throw new Exception($"Unexpected padding: {string.Join(" ", padding.Select(x => $"{x:X2}"))}");
            }
            
            var aWidth = reader.ReadUInt16(); //width - 1
            var aHeight = reader.ReadUInt16(); //height - 1
            var subAnimationCount = reader.ReadUInt16();
            var backgroundType = (AnimationModel.BackgroundType)reader.ReadByte();
            
            //header is always a fixed size
            var headerLength = backgroundType == AnimationModel.BackgroundType.ClearToColor ? 502 : 500;

            //for ClearToImage, there is an image before the header, otherwise it's straight to the header
            var images = new Dictionary<int, SimpleImageModel>();
            var img = 0;
            if (backgroundType == AnimationModel.BackgroundType.ClearToImage)
            {
                //there is one image before the header
                var image = ReadImage(rawData, reader, memStream, key, img, withOffset:true, offsetLength:headerLength);
                if (image == null)
                {
                    throw new Exception("Missing first image");
                }

                images[img++] = image;
            }

            byte clearColor = 0;
            byte unknown1 = 0;
            if (backgroundType == AnimationModel.BackgroundType.ClearToColor)
            {
                clearColor = reader.ReadByte();
                unknown1 = reader.ReadByte();
            }
            
            //header is always 500 bytes, which is 250 pairs of bytes
            //the entry number corresponds to the image in the file, 00 00 represents a skipped index 
            var imageIdx = 0;
            var imageIdToIndex = new Dictionary<int, int>();
            var imageIndexToUnknownData = new Dictionary<int, int>();
            for (var imageId = 0; imageId < 250; imageId++)
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
            
            //there are a variable number of images, ends with 00 05 00
            while (rawData[memStream.Position] == 0x07)
            {
                var image = ReadImage(rawData, reader, memStream, key, img);
                if (image == null)
                {
                    throw new Exception("Unparseable image");
                }

                images[img++] = image;
            }

            //TODO: correct image loading so this isn't necessary
            while (rawData[memStream.Position] == 0x00) //00 is used as padding sometimes
            {
                reader.ReadByte();
            }

            if (rawData[memStream.Position] != 0x05) //TODO: this is probably wrong on at least one animation
            {
                throw new Exception($"Invalid start to data section for {key}: {memStream.Position:X} {rawData[memStream.Position]:X2}");
            }
            
            //the data section is split into two sub-sections: instructions (VM opcodes) and data (simple instructions)
            //the instruction sub-section ends on opcode 14 (but may have more 14 immediately after, find the last one)
            //the data sub-section is referenced from opcodes in the instruction sub-section
            //some opcodes in the instruction sub-section are jumps to other areas of the instruction sub-section
            var dataSectionStart = memStream.Position;
            
            //first we parse the instructions as simple opcodes, against the offset they're in
            var instructions = new Dictionary<long, AnimationModel.AnimationInstruction>();
            //jump instructions are turned into labels as targets assigned to particular offsets
            var instructionLabels = new Dictionary<long, string>();
            var instructionLabelId = 1;
            //some instructions have pointers into data sub-section which are turned into other labels
            var dataLabels = new Dictionary<short, string>();
            var dataLabelId = 1;
            var instructionsDone = false;
            long lastOffset = 0;
            var offsets = new List<long>();
            var pushedInstruction = true;
            var stack = new List<byte>();
            do
            {
                if (pushedInstruction)
                {
                    lastOffset = memStream.Position - dataSectionStart;
                    pushedInstruction = false;
                }
                stack.Add(reader.ReadByte());
                
                //special case for end opcodes because we need to handle more than one in a row correctly
                if (stack[0] == 0x14)
                {
                    instructions[lastOffset] = new AnimationModel.AnimationInstruction()
                    {
                        Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.End
                    };
                    
                    //but also handle any subsequent 14 (like 14 14 shows up as two Ends)
                    while (rawData[memStream.Position] == 0x14)
                    {
                        lastOffset = memStream.Position - dataSectionStart;
                        reader.ReadByte();
                        instructions[lastOffset] = new AnimationModel.AnimationInstruction()
                        {
                            Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.End
                        };
                    }
                    //and now we're done
                    instructionsDone = true;
                    continue;
                }

                //the others are now basically a binary tree search of instructions
                var foundInstruction = true;
                var unknownInstruction = false;
                var opcode = AnimationModel.AnimationInstruction.AnimationOpcode.Unknown;
                var data = Array.Empty<byte>();
                var label = string.Empty;
                var prevPushesToRemove = 0;
                switch (stack[0])
                {
                    //first the ones that have no extra data, so they are always immediately found
                    case 0x00:
                        prevPushesToRemove = 7;
                        opcode = AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite;
                        break;
                    case 0x01:
                        prevPushesToRemove = 1;
                        opcode = AnimationModel.AnimationInstruction.AnimationOpcode.UnknownSprite01;
                        break;
                    case 0x02:
                        prevPushesToRemove = 1;
                        opcode = AnimationModel.AnimationInstruction.AnimationOpcode.WaitForFrames;
                        break;
                    case 0x04:
                        prevPushesToRemove = 1;
                        opcode = AnimationModel.AnimationInstruction.AnimationOpcode.KeepSpriteDrawn;
                        break;
                    case 0x07:
                        opcode = AnimationModel.AnimationInstruction.AnimationOpcode.Unknown07;
                        break;
                    case 0x08:
                        opcode = AnimationModel.AnimationInstruction.AnimationOpcode.Unknown08;
                        break;
                    case 0x0B:
                        prevPushesToRemove = 1;
                        opcode = AnimationModel.AnimationInstruction.AnimationOpcode.Unknown0B;
                        break;
                    case 0x0E:
                        prevPushesToRemove = 1;
                        opcode = AnimationModel.AnimationInstruction.AnimationOpcode.Unknown0E;
                        break;
                    //no need to handle 0x14 because of the above code
                    case 0x15:
                        opcode = AnimationModel.AnimationInstruction.AnimationOpcode.Unknown15;
                        break;
                    
                    //then the ones that have extra data after, so might not be found
                    case 0x05:
                        if (stack.Count == 4 && stack[1] == 0x00)
                        {
                            opcode = AnimationModel.AnimationInstruction.AnimationOpcode.Push;
                            data = new[] { stack[2], stack[3] };
                        }
                        else if (stack.Count == 4 && stack[1] == 0x01)
                        {
                            opcode = AnimationModel.AnimationInstruction.AnimationOpcode.Unknown0501;
                            data = new[] { stack[2], stack[3] };
                        }
                        else if (stack.Count > 4)
                        {
                            unknownInstruction = true;
                        }
                        else
                        {
                            foundInstruction = false;
                        }
                        break;
                    
                    case 0x06:
                        if (stack.Count == 3)
                        {
                            opcode = AnimationModel.AnimationInstruction.AnimationOpcode.Unknown06;
                            data = new[] { stack[1], stack[2] };
                        } else if (stack.Count > 3)
                        {
                            unknownInstruction = true;
                        }
                        else
                        {
                            foundInstruction = false;
                        }
                        break;
                    
                    case 0x12:
                        if (stack.Count == 3)
                        {
                            opcode = AnimationModel.AnimationInstruction.AnimationOpcode.Jump12;
                            data = new[] { stack[1], stack[2] };
                            var target = (long)(stack[1] | (stack[2] << 8));
                            instructionLabels[target] = $"LABEL_{instructionLabelId++}";
                            label = instructionLabels[target];
                        } else if (stack.Count > 3)
                        {
                            unknownInstruction = true;
                        }
                        else
                        {
                            foundInstruction = false;
                        }
                        break;
                    
                    case 0x13:
                        if (stack.Count == 3)
                        {
                            opcode = AnimationModel.AnimationInstruction.AnimationOpcode.Jump13;
                            data = new[] { stack[1], stack[2] };
                            var target = (long)(stack[1] | (stack[2] << 8));
                            instructionLabels[target] = $"LABEL_{instructionLabelId++}";
                            label = instructionLabels[target];
                        } else if (stack.Count > 3)
                        {
                            unknownInstruction = true;
                        }
                        else
                        {
                            foundInstruction = false;
                        }
                        break;
                    
                    default:
                        unknownInstruction = true;
                        break;
                }

                //unknown instructions means we have an overlap in the binary tree
                if (unknownInstruction)
                {
                    throw new Exception($"Unknown instruction: {string.Join(" ", stack.Select(x => $"{x:X2}"))}");
                }

                //not found means we have part of an instruction, so just continue reading
                if (foundInstruction)
                {
                    var stackParameters = new List<short>();
                    if (prevPushesToRemove > 0)
                    {
                        if (offsets.Count < prevPushesToRemove)
                        {
                            throw new Exception("Attempted to remove pushes when not enough instructions yet");
                        }
                        
                        //_logger.LogError($"Attempting to remove {prevPushesToRemove}, list is {offsets.Count}");
                        
                        for (var i = 0; i < prevPushesToRemove; i++)
                        {
                            //_logger.LogError($"Attempting to remove {i}: {offsets.Count - prevPushesToRemove + i}");
                            var offset = offsets[offsets.Count - prevPushesToRemove + i];
                            if (instructions[offset].Opcode != AnimationModel.AnimationInstruction.AnimationOpcode.Push)
                            {
                                throw new Exception($"Attempted to remove Push but found: {instructions[offset].Opcode}");
                            }

                            if (i != 0 && instructionLabels.ContainsKey(offset))
                            {
                                throw new Exception($"Attempted to remove Push referenced by label");
                            }

                            if (i == 0)
                            {
                                //by removing pushes we 'shift up' the instruction into that offset
                                lastOffset = offset;
                            }

                            var bytes = instructions[offset].Data;
                            stackParameters.Add((short)(bytes[0] | (bytes[1] << 8)));
                            instructions.Remove(offset);
                        }
                    }
                    
                    //special handling for some instructions
                    var dataLabel = string.Empty;
                    if (opcode == AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite)
                    {
                        //the first short is a data label
                        if (!dataLabels.TryGetValue(stackParameters[0], out dataLabel))
                        {
                            dataLabel = $"DATA_{dataLabelId++}";
                            dataLabels[stackParameters[0]] = dataLabel;
                        }
                        
                        stackParameters.RemoveAt(0);
                    }
                    
                    offsets.Add(lastOffset);
                    instructions.Add(lastOffset, new AnimationModel.AnimationInstruction()
                    {
                        Opcode = opcode,
                        Data = data,
                        Label = label,
                        StackParameters = stackParameters.ToArray(),
                        DataLabel = dataLabel
                    });
                    stack.Clear();
                    pushedInstruction = true;
                }
            } while (!instructionsDone);
            
            //double check that all instruction labels point to valid instructions
            var missingLabels = instructionLabels
                .Where(x => !instructions.ContainsKey(x.Key))
                .ToList();
            if (missingLabels.Count != 0)
            {
                _logger.LogError($"Missing labels ({missingLabels.Count}/{instructionLabels.Count}):");
                foreach (var missingLabel in missingLabels)
                {
                    _logger.LogError($"Missing label: {missingLabel.Value} {missingLabel.Key}");
                }
            }
            
            //as a second pass we turn the absolute jumps into relative jumps to labels
            var listInstructions = new List<AnimationModel.AnimationInstruction>(instructions.Count);
            var listLabels = new Dictionary<string, int>(instructionLabels.Count);
            var instructionIndex = 0;
            foreach (var offset in instructions.OrderBy(x => x.Key).Select(x => x.Key))
            {
                if (instructionLabels.TryGetValue(offset, out var label))
                {
                    listLabels[label] = instructionIndex;
                }

                listInstructions.Add(instructions[offset]);
                instructionIndex++;
            }
            
            //now we parse the data sub-section which contains simple step instructions that are
            //prefixed with their type as a byte, and have variable length depending on the type
            //data labels are used to track jump targets, including starting points referenced from
            //instructions (previous section)
            //the ending is padded with garbage data until a word ends, and the only way to detect
            //this is to know when we're in or out of a sequence triggered by a data label
            var steps = new Dictionary<long, AnimationModel.AnimationStep>();
            var inSequence = false;
            do
            {
                try
                {
                    var offset = (short)(memStream.Position - dataSectionStart);
                    if (!inSequence)
                    {
                        if (dataLabels.ContainsKey(offset))
                        {
                            inSequence = true;
                        }
                    }
                    var type = (AnimationModel.AnimationStep.StepType)reader.ReadByte();
                    var data = Array.Empty<byte>();
                    var dataLabel = string.Empty;
                    var isEnd = false;
                    switch (type)
                    {
                        case AnimationModel.AnimationStep.StepType.SetImage:
                            data = reader.ReadBytes(1);
                            break;
                        case AnimationModel.AnimationStep.StepType.Move1:
                            data = reader.ReadBytes(4);
                            break;
                        case AnimationModel.AnimationStep.StepType.Move:
                            data = reader.ReadBytes(4);
                            break;
                        case AnimationModel.AnimationStep.StepType.SetCounter:
                            data = reader.ReadBytes(2);
                            break;
                        case AnimationModel.AnimationStep.StepType.Jump:
                            data = reader.ReadBytes(2);
                            var target = (short)(data[0] | (data[1] << 8));
                            if (!dataLabels.TryGetValue(target, out dataLabel))
                            {
                                dataLabel = $"DATA_{dataLabelId++}";
                            }

                            dataLabels[target] = dataLabel;
                            break;
                        case AnimationModel.AnimationStep.StepType.Restart:
                            isEnd = true;
                            break;
                        case AnimationModel.AnimationStep.StepType.End9:
                            isEnd = true;
                            break;
                        case AnimationModel.AnimationStep.StepType.End7:
                            isEnd = true;
                            break;
                        case AnimationModel.AnimationStep.StepType.End:
                            isEnd = true;
                            break;
                        default:
                            throw new Exception($"Unknown step type: {(byte)type:X2} at offset {(offset + dataSectionStart):X4}");
                    }

                    // if (!inSequence)
                    // {
                    //     throw new Exception($"Got step {(byte)type:X2} while not in active sequence");
                    // }

                    if (isEnd)
                    {
                        inSequence = false;
                    }

                    steps.Add(offset, new AnimationModel.AnimationStep()
                    {
                        Type = type,
                        Data = data,
                        Label = dataLabel
                    });
                }
                catch (Exception e)
                {
                    if (memStream.Position > memStream.Length - 15 && !inSequence)
                    {
                        //we've hit garbage at the end of the file
                        //ignore the rest of the data
                        reader.ReadBytes(100);
                    }
                    else
                    {
                        throw;
                    }
                }
            } while (memStream.Position < memStream.Length);
            
            //double check that all data labels point to valid steps
            var missingDataLabels = dataLabels
                .Where(x => !steps.ContainsKey(x.Key))
                .ToList();
            if (missingDataLabels.Count != 0)
            {
                _logger.LogError($"Missing data labels ({missingDataLabels.Count}/{dataLabels.Count}):");
                foreach (var missingLabel in missingDataLabels)
                {
                    _logger.LogError($"Missing data label: {missingLabel.Value} {missingLabel.Key}");
                }
            }
            
            //as a second pass we turn the absolute jumps into relative jumps to labels
            var listSteps = new List<AnimationModel.AnimationStep>(steps.Count);
            var listDataLabels = new Dictionary<string, int>(dataLabels.Count);
            var stepIndex = 0;
            foreach (var offset in steps.OrderBy(x => x.Key).Select(x => x.Key))
            {
                if (dataLabels.TryGetValue((short)offset, out var dataLabel))
                {
                    listDataLabels[dataLabel] = stepIndex;
                }

                listSteps.Add(steps[offset]);
                stepIndex++;
            }

            var model = new AnimationModel()
            {
                Key = key,
                Images = images,
                ExtraData = new AnimationModel.Metadata()
                {
                    Name = key,
                    Comment = "Legacy import",
                    SubAnimationCount = subAnimationCount,
                    BackgroundType = backgroundType,
                    BoundingWidth = aWidth,
                    BoundingHeight = aHeight,
                    ColorMapping = colorMapping,
                    ClearColor = clearColor,
                    Unknown1 = unknown1,
                    ImageIdToIndex = imageIdToIndex,
                    ImageIndexToUnknownData = imageIndexToUnknownData,
                    Instructions = listInstructions,
                    InstructionLabels = listLabels,
                    Steps = listSteps,
                    DataLabels = listDataLabels
                }
            };
            return model;
        }

        private SimpleImageModel? ReadImage(byte[] rawData, BinaryReader reader, MemoryStream memStream, string key, int img, bool withOffset = false, int offsetLength = 502)
        {
            //because we don't do piece-meal parsing, we have to read a ton of extra bytes and pass them over first
            //but parsing the image will return the actual offset
            var originalOffset = memStream.Position;
            var bytes = reader.ReadBytes(16000); //no image can be longer than this anyway
            SimpleImageModel? model = null;
            try
            {
                model = _imageParser.Parse($"{key}_{img}", bytes, out var byteOffset);
                memStream.Seek(originalOffset + byteOffset, SeekOrigin.Begin);
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Failed to parse image {key} {img}: {e}");
            }
            
            //some images have some arbitrary bytes before the next image
            //TODO: for now we're just skipping them and logging them
            var hadToFix = false;
            var bytesSkipped = new List<byte>();
            var unfixedOffset = memStream.Position;
            while ((!withOffset && (rawData[memStream.Position] != 0x07 || rawData[memStream.Position+1] != 0x00) && rawData[memStream.Position] != 0x05) ||
                   (withOffset && (rawData[memStream.Position + offsetLength] != 0x07 || rawData[memStream.Position + offsetLength + 1] != 0x00)))
            {
                hadToFix = true;
                bytesSkipped.Add(reader.ReadByte());
            }

            if (hadToFix)
            {
                _logger.LogError($"Had to fix offset on {key} {img} from {unfixedOffset:X4} to {memStream.Position:X4}: {string.Join(", ", bytesSkipped.Select(x => $"{x:X2}"))}");
            }

            return model;
        }
    }
}