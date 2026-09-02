using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CovertActionTools.Core.Models
{
    /// <summary>
    /// Represents a PAN file animation, which combines a set of images with a two-level control system:
    /// a stack-based instruction VM that orchestrates sprites, and per-sprite step sequences that drive movement and drawing.
    /// </summary>
    public class AnimationModel
    {
        public enum BackgroundType
        {
            Unknown = -1,
            /// <summary>
            /// Keeps the background page left behind by the previous animation.
            /// </summary>
            PreviousAnimation = 0x00,
            /// <summary>
            /// Draws the background image (stored before the index table) onto the background page.
            /// </summary>
            ClearToImage = 0x01,
            /// <summary>
            /// Fills the background page with ClearColor.
            /// </summary>
            ClearToColor = 0x02
        }

        public enum ImageFormat
        {
            /// <summary>
            /// Images are stored as uncompressed rows, one byte per pixel.
            /// Note: not used in retail game versions but supported by the legacy game engine
            /// </summary>
            Raw = 0x00,
            /// <summary>
            /// Images are stored in the standard shared image format.
            /// </summary>
            Compressed = 0x01
        }

        public enum ColorMappingType
        {
            /// <summary>
            /// No colour block, the palette is left untouched.
            /// Note: not used in retail game versions but supported by the legacy game engine
            /// </summary>
            None = -1,
            /// <summary>
            /// A 17-byte colour block is embedded in the header.
            /// </summary>
            Embedded = 0x00,
            /// <summary>
            /// No colour block, the last loaded block is applied again.
            /// Note: not used in retail game versions but supported by the legacy game engine
            /// </summary>
            Previous = 0x01,
            /// <summary>
            /// A 774-byte palette block is embedded in the header.
            /// Note: not used in retail game versions but supported by the legacy game engine
            /// </summary>
            Palette = 0x02
        }

        public class AnimationInstruction
        {
            public enum AnimationOpcode
            {
                RawByte = -10, //fake instruction, allows piping raw byte into a file
                RawShort = -9, //fake instruction, allows piping 2 raw bytes into a file
                RawLabel = -8, //fake instruction, allows piping a calculated label straight into a file
                RawDataLabel = -7, //fake instruction, allows piping a calculated label straight into a file
                Comment = -2, //used for lines that have only comment
                Unknown = -1,
                SetupSprite = 0, //00, pops 7 (pointer, index, follow, x, y, rate, flags), sets up a sprite slot
                RemoveSprite = 1, //01, pops 1, deactivates target sprite
                WaitForFrames = 2, //02, pops 1, render for X frames
                TriggerAudio = 3, //03, pops 1, triggers audio from engine global audio table
                StampSprite = 4, //04, pops 1, draws the sprite into the background page at the end of the frame
                PushToStack = 5, //05 00 XX XX, push XX XX to stack (or a step label pointer)
                PushRegisterToStack = 261, //05 01 XX XX, push register X to stack
                PopStackToRegister = 6, //06 XX XX, pops stack and sets register X to value
                PushCopyOfStackValue = 7, //07, pushes copy of top stack value
                CompareEqual = 8, //08, pops 2 values off stack, and pushes 1 if the first value is equal to the second one, or 0 otherwise
                CompareNotEqual = 9, //09, pops 2 values off stack, and pushes 1 if the first value is not equal to the second one, or 0 otherwise
                CompareGreaterThan = 10, //0A, pops 2 values off stack, and pushes 1 if the first value is greater than the second one, or 0 otherwise
                CompareLessThan = 11, //0B, pops 2 values off stack, and pushes 1 if the first value is less than the second one, or 0 otherwise
                CompareGreaterOrEqual = 12, //0C, pops 2 values off stack, and pushes 1 if the first value is greater or equal to the second one, or 0 otherwise
                CompareLessOrEqual = 13, //0D, pops 2 values off stack, and pushes 1 if the first value is less or equal to the second one, or 0 otherwise
                Add = 14, //0E, pops most recent two stack entries, adds them together and pushes it
                Subtract = 15, //0F, pops most recent two stack entries, subtracts the most recent from the previous and pushes it
                Multiply = 16, //10, pops most recent two stack entries, multiplies the most recent from the previous and pushes it
                Divide = 17, //11, pops most recent two stack entries, divides the most recent from the previous and pushes it
                ConditionalJump = 18, //12 XX XX, pops 1, jumps only if the value is non-zero
                Jump = 19, //13 XX XX, always jumps
                End = 20, //14, never ends the animation: the pointer stays here and sprites keep stepping and drawing until the game stops it
                EndImmediate = 21, //15, ends the animation at once, the current frame is not stepped or drawn
                //Note: not used in retail game versions but supported by the legacy game engine
                Return = 22, //16, pops a return address and jumps to it
                //Note: not used in retail game versions but supported by the legacy game engine
                Call = 23, //17 XX XX, pushes the address of the next instruction and jumps
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
            /// Populated for SetupSprite instructions and step pointer pushes, points into data sub-section
            /// Note: step pointer pushes are not used in retail game versions but supported by the legacy game engine
            /// </summary>
            public string StepLabel { get; set; } = string.Empty;

            public string Comment { get; set; } = string.Empty;

            public AnimationInstruction Clone()
            {
                return new AnimationInstruction()
                {
                    Opcode = Opcode,
                    Data = Data.ToArray(),
                    Label = Label,
                    StackParameters = StackParameters.ToArray(),
                    StepLabel = StepLabel,
                    Comment = Comment
                };
            }
        }

        public class AnimationStep
        {
            public enum StepType
            {
                RawByte = -10, //fake instruction, allows piping raw byte into a file
                RawShort = -9, //fake instruction, allows piping 2 raw bytes into a file
                RawLabel = -8, //fake instruction, allows piping a calculated label straight into a file

                Comment = -2, //used for lines that have no instruction
                Unknown = -1,
                /// <summary>
                /// Draw frame with given image ID, -1 for waiting a frame without drawing
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
                /// Sets the speed of the sprite (255 steps every frame)
                /// </summary>
                SetSpeed = 0x03,
                /// <summary>
                /// Adds to the speed of the sprite
                /// </summary>
                AddSpeed = 0x04,
                /// <summary>
                /// Push value to Counter stack
                /// </summary>
                PushCounter = 0x05,
                /// <summary>
                /// Decrements Counter and jumps if it is not 0, otherwise pops it
                /// </summary>
                JumpIfCounter = 0x06,
                /// <summary>
                /// Reset simulation and state (position, speed)
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
            public string StepLabel { get; set; } = string.Empty;

            public string Comment { get; set; } = string.Empty;

            public AnimationStep Clone()
            {
                return new AnimationStep()
                {
                    Type = Type,
                    Data = Data.ToArray(),
                    Comment = Comment,
                    StepLabel = StepLabel
                };
            }
        }

        public class GlobalData
        {
            /// <summary>
            /// Width - 1
            /// </summary>
            public int BoundingWidth { get; set; }

            /// <summary>
            /// Height - 1
            /// </summary>
            public int BoundingHeight { get; set; }

            /// <summary>
            /// Minimum number of system timer ticks (18.2 per second) each frame lasts, 1 plays at full speed.
            /// Building animations in legacy data have higher values (3 to 5)
            /// </summary>
            public int FrameDelay { get; set; }

            /// <summary>
            /// Older exports stored FrameDelay under this name, only read during import
            /// </summary>
            [Obsolete("Use FrameDelay")]
            public int GlobalFrameSkip
            {
                set => FrameDelay = value;
            }

            /// <summary>
            /// How the game draws a background before updating or drawing images based on the animations
            /// </summary>
            public BackgroundType BackgroundType { get; set; } = BackgroundType.Unknown;

            /// <summary>
            /// How the images in the file are stored
            /// Note: only Compressed is used in retail game versions, Raw is supported by the legacy game engine
            /// </summary>
            public ImageFormat ImageFormat { get; set; } = ImageFormat.Compressed;

            /// <summary>
            /// Which colour block the header carries
            /// Note: only Embedded is used in retail game versions, the others are supported by the legacy game engine
            /// </summary>
            public ColorMappingType ColorMappingType { get; set; } = ColorMappingType.Embedded;

            /// <summary>
            /// Palette applied while the animation plays, maps colour indices 0-15 to displayed colours
            /// Entry 0 gets overwritten with 0 at runtime; legacy game files store 3 there and map colour 5 to 0
            /// </summary>
            public Dictionary<byte, byte> ColorMapping { get; set; } = new();

            /// <summary>
            /// Last byte of the embedded colour block, colours the screen border outside the 320x200 picture while the animation plays
            /// EGA writes it to the overscan register and Tandy to its border register, MCGA/CGA hand it to their palette routine (unverified), and emulators may not show it
            /// Note: always 0 in retail game versions but other values are supported by the legacy game engine
            /// </summary>
            public byte BorderColor { get; set; }

            /// <summary>
            /// Only populated when ColorMappingType is Palette, the raw 774-byte block
            /// Note: not used in retail game versions but supported by the legacy game engine
            /// </summary>
            public byte[] PaletteData { get; set; } = Array.Empty<byte>();

            /// <summary>
            /// Default screen position of the animation, normally overridden by the game at runtime
            /// Note: always 0 in retail game versions but other values are supported by the legacy game engine
            /// </summary>
            public int PositionX { get; set; }

            /// <summary>
            /// Default screen position of the animation, normally overridden by the game at runtime
            /// Note: always 0 in retail game versions but other values are supported by the legacy game engine
            /// </summary>
            public int PositionY { get; set; }

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

            public GlobalData Clone()
            {
                return new GlobalData()
                {
                    BoundingWidth = BoundingWidth,
                    BoundingHeight = BoundingHeight,
                    FrameDelay = FrameDelay,
                    BackgroundType = BackgroundType,
                    ImageFormat = ImageFormat,
                    ColorMappingType = ColorMappingType,
                    ColorMapping = ColorMapping.ToDictionary(x => x.Key, x => x.Value),
                    BorderColor = BorderColor,
                    PaletteData = PaletteData.ToArray(),
                    PositionX = PositionX,
                    PositionY = PositionY,
                    ClearColor = ClearColor,
                    Unknown2 = Unknown2,
                    ImageIdToIndex = ImageIdToIndex.ToDictionary(x => x.Key, x => x.Value),
                    ImageIndexToUnknownData = ImageIndexToUnknownData.ToDictionary(x => x.Key, x => x.Value)
                };
            }
        }

        public class ControlData
        {
            public List<AnimationInstruction> Instructions { get; set; } = new();
            public Dictionary<string, int> InstructionLabels { get; set; } = new();

            public List<AnimationStep> Steps { get; set; } = new();
            public Dictionary<string, int> StepLabels { get; set; } = new();

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
                    if (!string.IsNullOrEmpty(instruction.StepLabel))
                    {
                        instructionString += $" {instruction.StepLabel}";
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

            public string GetSerialisedSteps()
            {
                var lines = new List<string>();
                for (var i = 0; i < Steps.Count; i++)
                {
                    var dataLabelsOnLine = StepLabels
                        .Where(x => x.Value == i)
                        .Select(x => x.Key)
                        .ToList();
                    var labels = dataLabelsOnLine
                        .Select(x => $"@{x}:")
                        .ToList();
                    lines.AddRange(labels);

                    var step = Steps[i];
                    var stepString = $"{step.Type}";
                    if (!string.IsNullOrEmpty(step.StepLabel))
                    {
                        stepString += $" {step.StepLabel}";
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

                        if (string.IsNullOrWhiteSpace(lineText))
                        {
                            //it's a pure comment
                            instructions.Add(new AnimationInstruction()
                            {
                                Opcode = AnimationInstruction.AnimationOpcode.Comment,
                                Comment = comment
                            });
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
                                //either bare (everything comes from the stack) or a step label plus 6 parameters
                                if (!string.IsNullOrWhiteSpace(lineText))
                                {
                                    var labelEndIndex = lineText.IndexOf(' ');
                                    if (labelEndIndex == -1)
                                    {
                                        throw new Exception($"Incorrect number of parameters: {instructionLines[i]}");
                                    }
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
                                }
                                break;
                            case AnimationInstruction.AnimationOpcode.PushToStack:
                                //a non-numeric argument is a step label pointer
                                if (short.TryParse(lineText, out var pushValue))
                                {
                                    data.AddRange(new[] { (byte)(pushValue & 0xFF), (byte)(((ushort)pushValue & 0xFF00) >> 8) });
                                }
                                else
                                {
                                    dataLabel = lineText;
                                }
                                break;
                            case AnimationInstruction.AnimationOpcode.PushRegisterToStack:
                            case AnimationInstruction.AnimationOpcode.PopStackToRegister:
                                var n = short.Parse(lineText);
                                data.AddRange(new[] { (byte)(n & 0xFF), (byte)(((ushort)n & 0xFF00) >> 8) });
                                break;
                            case AnimationInstruction.AnimationOpcode.Jump:
                            case AnimationInstruction.AnimationOpcode.ConditionalJump:
                            case AnimationInstruction.AnimationOpcode.Call:
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
                                //the parameter is optional, without it the value comes from the stack
                                if (!string.IsNullOrWhiteSpace(lineText))
                                {
                                    stackParameters.Add(short.Parse(lineText));
                                }
                                break;
                            case AnimationInstruction.AnimationOpcode.PushCopyOfStackValue:
                            case AnimationInstruction.AnimationOpcode.EndImmediate:
                            case AnimationInstruction.AnimationOpcode.End:
                            case AnimationInstruction.AnimationOpcode.Return:
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
                            StepLabel = dataLabel,
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

                        if (string.IsNullOrWhiteSpace(lineText))
                        {
                            //it's a pure comment
                            steps.Add(new AnimationStep()
                            {
                                Type = AnimationStep.StepType.Comment,
                                Comment = comment
                            });
                            continue;
                        }

                        var typeEndIndex = lineText.IndexOf(' ');
                        if (typeEndIndex == -1)
                        {
                            typeEndIndex = lineText.Length;
                        }

                        var typeString = lineText.Substring(0, typeEndIndex);
                        //older exports used the frame skip names for the speed steps
                        typeString = typeString switch
                        {
                            "SetFrameSkip" => "SetSpeed",
                            "SetFrameAdjustment" => "AddSpeed",
                            _ => typeString
                        };
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
                            case AnimationStep.StepType.SetSpeed:
                            case AnimationStep.StepType.AddSpeed:
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
                            StepLabel = label,
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
                    StepLabels = stepLabels;

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            public ControlData Clone()
            {
                return new ControlData()
                {
                    Instructions = Instructions.Select(x => x.Clone()).ToList(),
                    InstructionLabels = InstructionLabels.ToDictionary(x => x.Key, x => x.Value),
                    Steps = Steps.Select(x => x.Clone()).ToList(),
                    StepLabels = StepLabels.ToDictionary(x => x.Key, x => x.Value)
                };
            }
        }

        /// <summary>
        /// ID that also determines the filename
        /// </summary>
        public string Key { get; set; } = string.Empty;

        public Dictionary<int, SharedImageModel> Images { get; set; } = new();
        public GlobalData Data { get; set; } = new();
        public ControlData Control { get; set; } = new();
        public SharedMetadata Metadata { get; set; } = new();

        public AnimationModel Clone()
        {
            return new AnimationModel()
            {
                Key = Key,
                Metadata = Metadata.Clone(),
                Data = Data.Clone(),
                Control = Control.Clone(),
                Images = Images.ToDictionary(x => x.Key,
                    x => x.Value.Clone())
            };
        }
    }
}
