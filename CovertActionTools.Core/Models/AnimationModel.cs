using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CovertActionTools.Core.Models
{
    /// <summary>
    /// The format for PAN files is not fully understood, so this model is a best-guess attempt at parsing.
    /// Some of the information in the file is understood and seems to correlate to behaviour, but even minor
    /// changes end up breaking the animation loading in-game because of the use of pointers and unknown correlated
    /// values. Therefore this data is considered read-only until the format is fully understood.
    /// </summary>
    public class AnimationModel
    {
        public enum BackgroundType
        {
            Unknown = -1,
            /// <summary>
            /// Relies on previous animation to have set up content.
            /// Header section is before any image.
            /// Header section is only 500 bytes long instead of 502.
            /// </summary>
            PreviousAnimation = 0x00,
            /// <summary>
            /// Redraws first image as background before anything else.
            /// Header section is after first image.
            /// Header section is 502 bytes long.
            /// </summary>
            ClearToImage = 0x01,
            /// <summary>
            /// Clears screen to colour before anything else.
            /// Header section is before any image.
            /// Header section is 502 bytes long. First byte is background colour to clear to.
            /// </summary>
            ClearToColor = 0x02
        }

        public class AnimationInstruction
        {
            public enum AnimationOpcode
            {
                RawByte = -10, //fake instruction, allows piping raw byte into a file
                RawShort = -9, //fake instruction, allows piping 2 raw bytes into a file
                RawLabel = -8, //fake instruction, allows piping a calculated label straight into a file
                RawDataLabel = -7, //fake instruction, allows piping a calculated label straight into a file
                Unknown = -1,
                SetupSprite = 0, //00, loads 7 * 2 stack (pointer, index, follow, x, y, frame skip, flags), add active sprite
                RemoveSprite = 1, //01, loads 2 stack, stops/removes target sprite
                WaitForFrames = 2, //02, loads 2 stack, render for X frames
                TriggerAudio = 3, //03, loads 2 stack, triggers audio from engine global audio table
                StampSprite = 4, //04, loads 2 stack, saves a persistent copy of the sprite at the current position/image
                PushToStack = 5, //05 00 XX XX, push XX XX to stack
                PushRegisterToStack = 261, //05 01 XX XX, push register X to stack
                PopStackToRegister = 6, //06 XX XX, pops stack and sets register X to value
                PushCopyOfStackValue = 7, //07, pushes copy of top stack value
                CompareEqual = 8, //08, loads 2 stack, pops 2 values off stack, and pushes 1 if the first value is equal to the second one, or 0 otherwise
                CompareNotEqual = 9, //09, loads 2 stack, pops 2 values off stack, and pushes 1 if the first value is not equal to the second one, or 0 otherwise
                CompareGreaterThan = 10, //0A, loads 2 stack, pops 2 values off stack, and pushes 1 if the first value is greater than the second one, or 0 otherwise
                CompareLessThan = 11, //0B, loads 2 stack, pops 2 values off stack, and pushes 1 if the first value is less than the second one, or 0 otherwise
                CompareGreaterOrEqual = 12, //0C, loads 2 stack, pops 2 values off stack, and pushes 1 if the first value is greater or equal to the second one, or 0 otherwise
                CompareLessOrEqual = 13, //0D, loads 2 stack, pops 2 values off stack, and pushes 1 if the first value is less or equal to the second one, or 0 otherwise
                Add = 14, //0E, loads 2 stack, pops most recent two stack entries, adds them together and pushes it
                Subtract = 15, //0F, loads 2 stack, pops most recent two stack entries, subtracts the most recent from the previous and pushes it
                Multiply = 16, //10, loads 2 stack, pops most recent two stack entries, multiplies the most recent from the previous and pushes it 
                Divide = 17, //11, loads 2 stack, pops most recent two stack entries, divides the most recent from the previous and pushes it
                ConditionalJump = 18, //12 XX XX, jumps only if compare flag is set
                Jump = 19, //13 XX XX, always jumps, TODO: unclear why some existing files have two 13's one after the other
                End = 20, //14, acts as a WaitForFrames 1 that infinitely loops
                EndImmediate = 21, //15, acts as an immediate end to the animation
                
            }

            public AnimationOpcode Opcode { get; set; } = AnimationOpcode.Unknown;
            public byte[] Data { get; set; } = Array.Empty<byte>();
            
            /// <summary>
            /// Populated for jump instructions
            /// </summary>
            public string Label { get; set; } = string.Empty;
            
            /// <summary>
            /// Populated from preceding Push instructions, and removes those Push instructions
            /// </summary>
            public short[] StackParameters { get; set; } = Array.Empty<short>();

            /// <summary>
            /// Populated for SetupSprite instructions, points into data sub-section
            /// </summary>
            public string DataLabel { get; set; } = string.Empty;

            public string Comment { get; set; } = string.Empty;
        }

        public class AnimationStep
        {
            public enum StepType
            {
                RawByte = -10, //fake instruction, allows piping raw byte into a file
                RawShort = -9, //fake instruction, allows piping 2 raw bytes into a file
                RawLabel = -8, //fake instruction, allows piping a calculated label straight into a file
                Unknown = -1,
                /// <summary>
                /// Draw frame with current image, -1 for waiting a frame without drawing
                /// Does NOT reduce Counter
                /// </summary>
                DrawFrame = 0x00,
                /// <summary>
                /// Move to absolute position
                /// </summary>
                MoveAbsolute = 0x01,
                /// <summary>
                /// Move relative to current position
                /// </summary>
                MoveRelative = 0x02,
                /// <summary>
                /// Sets frame skip of sprite
                /// TODO: actual way value maps to behaviour is unclear
                /// </summary>
                SetFrameSkip = 0x03,
                /// <summary>
                /// TODO: this is related to frame skip but unclear exactly how it works
                /// </summary>
                SetFrameAdjustment = 0x04,
                /// <summary>
                /// Push value to Counter stack
                /// </summary>
                PushCounter = 0x05,
                /// <summary>
                /// Jump only if Counter is > 0
                /// Reduces Counter by 1 or pops Counter stack if 0 
                /// </summary>
                JumpIfCounter = 0x06,
                /// <summary>
                /// Reset simulation and state (position)
                /// </summary>
                Restart = 0x07,
                /// <summary>
                /// Reset simulation but do not reset state (position)
                /// </summary>
                Loop = 0x08,
                /// <summary>
                /// Stop simulating and continue drawing
                /// </summary>
                Pause = 0x09,
                /// <summary>
                /// Stop simulating and drawing
                /// </summary>
                Stop = 0x0A,
            }

            public StepType Type { get; set; } = StepType.Unknown;
            public byte[] Data { get; set; } = Array.Empty<byte>();
            /// <summary>
            /// Only populated for jump instructions
            /// </summary>
            public string Label { get; set; } = string.Empty;

            public string Comment { get; set; } = string.Empty;
        }

        public class Metadata
        {
            /// <summary>
            /// Actual name separate from key/filename, for development
            /// </summary>
            public string Name { get; set; } = string.Empty;
            /// <summary>
            /// Arbitrary comment, for development
            /// </summary>
            public string Comment { get; set; } = string.Empty;
            
            /// <summary>
            /// Width - 1
            /// </summary>
            public int BoundingWidth { get; set; }
            
            /// <summary>
            /// Height - 1
            /// </summary>
            public int BoundingHeight { get; set; }
            
            /// <summary>
            /// Global frame skip value, 1 means play normally, higher values skip increasing numbers of frames.
            /// Building animations in legacy data have non-1 values (3 or 4)
            /// </summary>
            public int GlobalFrameSkip { get; set; }

            /// <summary>
            /// How the game draws a background before updating or drawing images based on the animations
            /// </summary>
            public BackgroundType BackgroundType { get; set; } = BackgroundType.Unknown;
            
            /// <summary>
            /// Color mapping for the entire set of images in the file
            /// Not actually used by legacy game files, but engine supports it
            /// </summary>
            public Dictionary<byte, byte> ColorMapping { get; set; } = new();
            
            /// <summary>
            /// Only populated when BackgroundType is ClearToColor
            /// Represents the color to clear to before drawing anything
            /// </summary>
            public byte ClearColor { get; set; }
            /// <summary>
            /// Only populated when BackgroundType is ClearToColor
            /// </summary>
            public byte Unknown2 { get; set; }

            /// <summary>
            /// Each image ID is assigned an index; this ID can only increase monotonically but can have gaps. 
            /// In the file format, this looks like a series of u16, where any 00 00 represents a gap.
            /// With no gaps in 500 bytes that means the maximum number of images is 250.
            /// Example: the start of a header with data AA AA  00 00  00 00  BB BB
            /// means image 0 is index 0, image 1 (B) is index 3 (gap of 2)
            /// Important: the background image is not part of the IDs
            /// </summary>
            public Dictionary<int, int> ImageIdToIndex { get; set; } = new();

            /// <summary>
            /// Each image index has a set of data attached that seems arbitrary and does not relate to the file.
            /// This may be an authoring concern and not relevant to the file, except that 00 00 represents a gap.
            /// The data is encoded here as u16 but might be two separate u8s instead.
            /// Potentially this might be a grid position (X, Y) for display in the editor.
            /// Example: the start of a header with data AA AA  00 00  00 00  BB BB
            /// means index 0 has data AA AA, index 3 has data BB BB
            /// Important: the background image is not part of the IDs
            /// </summary>
            public Dictionary<int, int> ImageIndexToUnknownData { get; set; } = new();
            
            public List<AnimationInstruction> Instructions { get; set; } = new();
            public Dictionary<string, int> InstructionLabels { get; set; } = new();
            
            public string GetSerialisedInstructions()
            {
                var lines = new List<string>();
                for (var i = 0; i < Instructions.Count; i++)
                {
                    var labels = InstructionLabels
                        .Where(x => x.Value == i)
                        .Select(x => $"@{x.Key}:")
                        .ToList();
                    lines.AddRange(labels);
                    
                    var instruction = Instructions[i];
                    var instructionString = $"{instruction.Opcode}";
                    if (!string.IsNullOrEmpty(instruction.Label))
                    {
                        instructionString += $" {instruction.Label}";
                    }
                    if (!string.IsNullOrEmpty(instruction.DataLabel))
                    {
                        instructionString += $" {instruction.DataLabel}";
                    }
                    if (instruction.Data.Length > 0)
                    {
                        if (instruction.Data.Length == 2)
                        {
                            instructionString += $" {(short)(instruction.Data[0] | (instruction.Data[1] << 8))}";
                        }
                        else
                        {
                            instructionString += $" {string.Join(" ", instruction.Data.Select(x => $"{x:X2}"))}";
                        }
                    }
                    if (instruction.StackParameters.Length > 0)
                    {
                        instructionString += $" {string.Join(" ", instruction.StackParameters.Select(x => $"{x}"))}";
                    }

                    if (!string.IsNullOrEmpty(instruction.Comment))
                    {
                        instructionString += $"  ; {instruction.Comment}";
                    }
                    
                    lines.Add($"\t{instructionString}");
                }

                return string.Join("\n", lines);
            }

            public List<AnimationStep> Steps { get; set; } = new();
            public Dictionary<string, int> DataLabels { get; set; } = new();
            
            public string GetSerialisedSteps()
            {
                var lines = new List<string>();
                for (var i = 0; i < Steps.Count; i++)
                {
                    var dataLabelsOnLine = DataLabels
                        .Where(x => x.Value == i)
                        .Select(x => x.Key)
                        .ToList();
                    var labels = dataLabelsOnLine
                        .Select(x => $"@{x}:")
                        .ToList();
                    lines.AddRange(labels);

                    var step = Steps[i];
                    var stepString = $"{step.Type}";
                    if (!string.IsNullOrEmpty(step.Label))
                    {
                        stepString += $" {step.Label}";
                    }

                    if (step.Data.Length > 0)
                    {
                        if (step.Data.Length == 1)
                        {
                            stepString += $" {(sbyte)step.Data[0]}";
                        } else if (step.Data.Length == 2)
                        {
                            stepString += $" {(short)(step.Data[0] | (step.Data[1] << 8))}";
                        } else if (step.Data.Length == 4)
                        {
                            stepString += $" {(short)(step.Data[0] | (step.Data[1] << 8))}";
                            stepString += $" {(short)(step.Data[2] | (step.Data[3] << 8))}";
                        }
                    }

                    if (!string.IsNullOrEmpty(step.Comment))
                    {
                        stepString += $"  ; {step.Comment}";
                    }
                    
                    lines.Add($"\t{stepString}");
                }

                return string.Join("\n", lines);
            }

            public void ParseInstructionsAndSteps(string instructionsString, string stepsString)
            {
                try
                {
                    var instructions = new List<AnimationInstruction>();
                    var instructionLabels = new Dictionary<string, int>();
                    var steps = new List<AnimationStep>();
                    var stepLabels = new Dictionary<string, int>();

                    var instructionLines = instructionsString.Split('\n')
                        .Select(x => x.Trim().Trim('\r', '\t', '\n'))
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
                    var labelStack = new List<string>();
                    for (var i = 0; i < instructionLines.Count; i++)
                    {
                        var lineText = instructionLines[i];
                        var comment = string.Empty;
                        if (lineText.Contains(";"))
                        {
                            var separatorIndex = lineText.IndexOf(';');
                            comment = lineText
                                .Substring(separatorIndex)
                                .Trim('\r', '\t').Trim()
                                .Trim(';').Trim();
                            lineText = lineText
                                .Substring(0, separatorIndex)
                                .Trim('\r', '\t').Trim();
                        }

                        if (lineText.StartsWith("@"))
                        {
                            //it's a label
                            labelStack.Add(lineText.Replace("@", "").Replace(":", ""));
                            continue;
                        }

                        var opcodeEndIndex = lineText.IndexOf(' ');
                        if (opcodeEndIndex == -1)
                        {
                            opcodeEndIndex = lineText.Length;
                        }

                        var opcodeString = lineText.Substring(0, opcodeEndIndex);
                        if (!Enum.TryParse(opcodeString, out AnimationInstruction.AnimationOpcode opcode))
                        {
                            throw new Exception($"Unknown opcode: {instructionLines[i]}");
                        }

                        lineText = lineText.Substring(opcodeEndIndex)
                            .Trim('\r', '\t').Trim();
                        var stackParameters = new List<short>();
                        var data = new List<byte>();
                        var label = string.Empty;
                        var dataLabel = string.Empty;
                        switch (opcode)
                        {
                            case AnimationInstruction.AnimationOpcode.RawByte:
                                data.Add(byte.Parse(lineText, NumberStyles.HexNumber));
                                break;
                            case AnimationInstruction.AnimationOpcode.RawShort:
                                var parsedShort = short.Parse(lineText);
                                data.Add((byte)(parsedShort & 0x00FF));
                                data.Add((byte)((parsedShort & 0xFF00) >> 8));
                                break;
                            case AnimationInstruction.AnimationOpcode.RawLabel:
                                label = lineText.Trim('\r', '\n').Trim();
                                break;
                            case AnimationInstruction.AnimationOpcode.RawDataLabel:
                                dataLabel = lineText.Trim('\r', '\t').Trim();
                                break;
                            case AnimationInstruction.AnimationOpcode.SetupSprite:
                                var labelEndIndex = lineText.IndexOf(' ');
                                dataLabel = lineText.Substring(0, labelEndIndex)
                                    .Trim('\r', '\t').Trim();
                                lineText = lineText.Substring(labelEndIndex);
                                var remainingParameters = lineText.Split(' ')
                                    .Select(x => x.Trim('\r', '\t').Trim())
                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                    .Select(short.Parse).ToList();
                                if (remainingParameters.Count != 6)
                                {
                                    throw new Exception($"Incorrect number of parameters: {instructionLines[i]}");
                                }

                                stackParameters.AddRange(remainingParameters);
                                break;
                            case AnimationInstruction.AnimationOpcode.PushToStack:
                            case AnimationInstruction.AnimationOpcode.PushRegisterToStack:
                            case AnimationInstruction.AnimationOpcode.PopStackToRegister:
                                var n = short.Parse(lineText);
                                data.AddRange(new[] { (byte)(n & 0xFF), (byte)(((ushort)n & 0xFF00) >> 8) });
                                break;
                            case AnimationInstruction.AnimationOpcode.Jump:
                            case AnimationInstruction.AnimationOpcode.ConditionalJump:
                                label = lineText.Trim('\r', '\n').Trim();
                                break;
                            case AnimationInstruction.AnimationOpcode.WaitForFrames:
                            case AnimationInstruction.AnimationOpcode.StampSprite:
                            case AnimationInstruction.AnimationOpcode.CompareEqual:
                            case AnimationInstruction.AnimationOpcode.CompareLessThan:
                            case AnimationInstruction.AnimationOpcode.CompareNotEqual:
                            case AnimationInstruction.AnimationOpcode.CompareGreaterThan:
                            case AnimationInstruction.AnimationOpcode.CompareGreaterOrEqual:
                            case AnimationInstruction.AnimationOpcode.CompareLessOrEqual:
                            case AnimationInstruction.AnimationOpcode.Subtract:
                            case AnimationInstruction.AnimationOpcode.Multiply:
                            case AnimationInstruction.AnimationOpcode.Divide:
                            case AnimationInstruction.AnimationOpcode.Add:
                            case AnimationInstruction.AnimationOpcode.TriggerAudio:
                            case AnimationInstruction.AnimationOpcode.RemoveSprite:
                                stackParameters.Add(short.Parse(lineText));
                                break;
                            case AnimationInstruction.AnimationOpcode.PushCopyOfStackValue:
                            case AnimationInstruction.AnimationOpcode.EndImmediate:
                            case AnimationInstruction.AnimationOpcode.End:
                                break;

                            default:
                                throw new Exception($"Unhandled opcode: {instructionLines[i]}");
                        }

                        instructions.Add(new AnimationInstruction()
                        {
                            Opcode = opcode,
                            StackParameters = stackParameters.ToArray(),
                            Data = data.ToArray(),
                            Label = label,
                            DataLabel = dataLabel,
                            Comment = comment
                        });

                        //if there are any labels queued up, they take effect on the first instruction after
                        if (labelStack.Count != 0)
                        {
                            foreach (var queuedLabel in labelStack)
                            {
                                instructionLabels.Add(queuedLabel, instructions.Count - 1);
                            }

                            labelStack.Clear();
                        }
                    }

                    var stepLines = stepsString.Split('\n')
                        .Select(x => x.Trim().Trim('\r', '\t', '\n'))
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
                    var dataLabelStack = new List<string>();
                    for (var i = 0; i < stepLines.Count; i++)
                    {
                        var lineText = stepLines[i];
                        var comment = string.Empty;
                        if (lineText.Contains(";"))
                        {
                            var separatorIndex = lineText.IndexOf(';');
                            comment = lineText
                                .Substring(separatorIndex)
                                .Trim('\r', '\t').Trim()
                                .Trim(';').Trim();
                            lineText = lineText
                                .Substring(0, separatorIndex)
                                .Trim('\r', '\t').Trim();
                        }

                        if (lineText.StartsWith("@"))
                        {
                            //it's a label
                            dataLabelStack.Add(lineText.Replace("@", "").Replace(":", ""));
                            continue;
                        }

                        var typeEndIndex = lineText.IndexOf(' ');
                        if (typeEndIndex == -1)
                        {
                            typeEndIndex = lineText.Length;
                        }

                        var typeString = lineText.Substring(0, typeEndIndex);
                        if (!Enum.TryParse(typeString, out AnimationStep.StepType type))
                        {
                            throw new Exception($"Unknown type: {stepLines[i]}");
                        }

                        lineText = lineText.Substring(typeEndIndex)
                            .Trim('\r', '\t').Trim();
                        var data = new List<byte>();
                        var label = string.Empty;
                        switch (type)
                        {
                            case AnimationStep.StepType.RawByte:
                                data.Add((byte)sbyte.Parse(lineText));
                                break;
                            case AnimationStep.StepType.RawShort:
                                var value = (ushort)short.Parse(lineText);
                                data.Add((byte)(value & 0x00FF));
                                data.Add((byte)((value & 0xFF00) >> 8));
                                break;
                            case AnimationStep.StepType.RawLabel:
                                label = lineText;
                                break;
                            case AnimationStep.StepType.Loop:
                            case AnimationStep.StepType.Pause:
                            case AnimationStep.StepType.Restart:
                            case AnimationStep.StepType.Stop:
                                break;
                            case AnimationStep.StepType.DrawFrame:
                                data.Add((byte)sbyte.Parse(lineText));
                                break;
                            case AnimationStep.StepType.JumpIfCounter:
                                label = lineText;
                                break;
                            case AnimationStep.StepType.SetFrameSkip:
                            case AnimationStep.StepType.SetFrameAdjustment:
                            case AnimationStep.StepType.PushCounter:
                                var val = short.Parse(lineText);
                                data.AddRange(new[] { (byte)(val & 0xFF), (byte)((val & 0xFF00) >> 8) });
                                break;
                            case AnimationStep.StepType.MoveAbsolute:
                            case AnimationStep.StepType.MoveRelative:
                                var separator = lineText.IndexOf(' ');
                                var firstVal = short.Parse(lineText.Substring(0, separator));
                                var secondVal = short.Parse(lineText.Substring(separator));
                                data.AddRange(new[] { (byte)(firstVal & 0xFF), (byte)((firstVal & 0xFF00) >> 8) });
                                data.AddRange(new[] { (byte)(secondVal & 0xFF), (byte)((secondVal & 0xFF00) >> 8) });
                                break;
                            default:
                                throw new Exception($"Unhandled step type: {stepLines[i]}");
                        }

                        steps.Add(new AnimationStep()
                        {
                            Type = type,
                            Comment = comment,
                            Data = data.ToArray(),
                            Label = label,
                        });

                        //if there are any labels queued up, they take effect on the first instruction after
                        if (dataLabelStack.Count != 0)
                        {
                            foreach (var queuedLabel in dataLabelStack)
                            {
                                stepLabels.Add(queuedLabel, steps.Count - 1);
                            }

                            dataLabelStack.Clear();
                        }
                    }

                    Instructions = instructions;
                    InstructionLabels = instructionLabels;

                    Steps = steps;
                    DataLabels = stepLabels;

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        
        /// <summary>
        /// ID that also determines the filename
        /// </summary>
        public string Key { get; set; } = string.Empty;

        public Dictionary<int, SimpleImageModel> Images { get; set; } = new();
        public Metadata ExtraData { get; set; } = new();
    }
}