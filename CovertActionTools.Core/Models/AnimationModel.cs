using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

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
                Unknown = -1,
                SetupSprite = 5, //00, loads 7 * 2 stack (pointer, index, u1, x, y, u2, u3), add active sprite
                UnknownSprite01 = 14, //01, loads 2 stack, do something to sprite X?
                WaitForFrames = 9, //02, loads 2 stack, render for X frames
                KeepSpriteDrawn = 12, //04, loads 2 stack, persist sprite X after end?
                Push = 1, //05 00 XX XX, push to stack?
                Unknown0501 = 2, //05 01 XX XX, LDA?
                Unknown06 = 8, //06 XX XX, loads 2 stack?
                Unknown07 = 6, //07
                Unknown08 = 7, //08, ADD?
                Unknown0B = 11, //0B, CMP?
                Unknown0E = 13, //0E, CMP?
                Jump12 = 3, //12 XX XX, conditional jump?
                Jump13 = 4, //13 XX XX, unconditional jump?
                End = 0, //14, end or return?
                Unknown15 = 10, //15
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
        }

        public class AnimationStep
        {
            public enum StepType
            {
                Unknown = -1,
                SetImage = 0x00,
                Move1 = 0x01,
                Move = 0x02,
                SetCounter = 0x05,
                JumpAndReduceCounter = 0x06,
                End7 = 0x07,
                Restart = 0x08,
                End9 = 0x09,
                End = 0x0A
            }

            public StepType Type { get; set; } = StepType.Unknown;
            public byte[] Data { get; set; } = Array.Empty<byte>();
            /// <summary>
            /// Only populated for jump instructions
            /// </summary>
            public string Label { get; set; } = string.Empty;
        }

        /// <summary>
        /// Record that sets up some data or an animation loop of some sort, the type is identified by the number
        /// of bytes it contains. Each record is separated by 05 00, and 05 05 00 signals the end of these records.
        /// Not fully understood, but at least one type seems to set up an animation loop with a pointer to
        /// an InstructionRecord.
        /// </summary>
        [JsonDerivedType(typeof(SetupAnimationRecord), "setup")]
        [JsonDerivedType(typeof(Instruction3Record), "i3")]
        [JsonDerivedType(typeof(UnknownRecord), "unknown")]
        [JsonDerivedType(typeof(EndRecord), "end")]
        public abstract class SetupRecord
        {
            public enum SetupType
            {
                Unknown = -1,
                Unknown1 = 1,
                Animation = 2,
                Instruction3 = 3, 
                Unknown2 = 4,
                Unknown5 = 5,
                Unknown7 = 7,
                Unknown9 = 9,
                Unknown14 = 14,
                Repeat = 30,
                Unknown12 = 100,
                Unknown13 = 110,
                Unknown6 = 120,
                End = -2,
            }

            public SetupType RecordType { get; set; } = SetupType.Unknown;

            public static SetupRecord ParseNextRecord(MemoryStream stream, BinaryReader reader, byte[] dataSectionBytes)
            {
                //read the separator
                var separator = reader.ReadBytes(2);
                if (separator[1] == 0x14)
                {
                    //it's an end record
                    return new EndRecord()
                    {
                        RecordType = SetupType.End,
                        Type = (EndRecord.EndType)separator[0]
                    };
                }

                if (separator[0] != 0x05 && 
                    separator[0] != 0x06 &&
                    separator[0] != 0x07 &&
                    separator[0] != 0x12 && 
                    separator[0] != 0x13)
                {
                    throw new Exception($"Invalid separator: {separator[0]:X2} {separator[1]:X2}");
                }
                
                //07 is a record that is a single byte
                if (separator[0] == 0x07)
                {
                    stream.Seek(-1, SeekOrigin.Current);
                    return new UnknownRecord()
                    {
                        RecordType = SetupType.Unknown7
                    };
                }
                
                //at this point we have to assemble the record until it's a recognisable shape
                var recordStack = new List<byte>();
                var recordStart = stream.Position - 1; //starting at XX
                recordStack.Add(separator[1]);
                
                //there is no record that has less than 2 bytes before a separator
                recordStack.AddRange(reader.ReadBytes(1));

                var iterations = 0;
                while(iterations++ < 64000) //larger than any feasible file
                {
                    if (recordStack[recordStack.Count-1] == 0x14 &&
                        recordStack.Count > 1 &&
                        (
                            recordStack[recordStack.Count-2] == 0x14 ||
                            recordStack[recordStack.Count-2] == 0x15
                        ))
                    {
                        //we missed an ending
                        stream.Seek(-2, SeekOrigin.Current); //reverse the ending
                        return new UnknownRecord()
                        {
                            Data = recordStack.Take(recordStack.Count - 2).ToArray()
                        };
                    }
                    
                    //Unknown2 is XX YY ZZ
                    if (separator[0] == 0x05 &&
                        recordStack.Count == 3 &&
                        (recordStack[0] == 0x01))
                    {
                        stream.Seek(recordStart, SeekOrigin.Begin);
                        return new UnknownRecord()
                        {
                            RecordType = SetupType.Unknown2,
                            Data = reader.ReadBytes(3)
                        };
                    }
                    
                    //SetupAnimation starts 00 PP PP 05 00 XX XX 05 00 YY YY 05 00 ZZ ZZ 05 00 but continues longer
                    if (separator[0] == 0x05 &&
                        recordStack.Count == 17 &&
                        recordStack[0] == 0x00 &&
                        recordStack[3] == 0x05 &&
                        recordStack[4] == 0x00 && 
                        recordStack[7] == 0x05 && 
                        recordStack[8] == 0x00 &&
                        recordStack[11] == 0x05 &&
                        recordStack[12] == 0x00 &&
                        recordStack[15] == 0x05 &&
                        recordStack[16] == 0x00)
                    {
                        stream.Seek(recordStart + 1, SeekOrigin.Begin);
                        return AsAnimation(reader, dataSectionBytes);
                    }
                    
                    //Instruction3 is 00 XX YY TT
                    if (separator[0] == 0x05 &&
                        recordStack.Count == 4 &&
                        recordStack[0] == 0x00 &&
                        (recordStack[3] == 0x01 ||
                         recordStack[3] == 0x02 ||
                         recordStack[3] == 0x04 ||
                         recordStack[3] == 0x0B ||
                         recordStack[3] == 0x0E ||
                         recordStack[3] == 0x00))
                    {
                        stream.Seek(recordStart + 1, SeekOrigin.Begin);
                        return AsInstruction3(reader);
                    }
                    
                    //Unknown5 is 00 XX YY ZZ QQ 00
                    if (separator[0] == 0x05 &&
                        recordStack.Count == 6 &&
                        recordStack[0] == 0x00 &&
                        recordStack[5] == 0x00)
                    {
                        stream.Seek(recordStart + 1, SeekOrigin.Begin);
                        return new UnknownRecord()
                        {
                            RecordType = SetupType.Unknown5,
                            Data = reader.ReadBytes(5)
                        };
                    }
                    
                    //RepeatRecord is 00 XX YY ZZ ..followed by a repeated 
                    // if (recordStack.Count == 10 &&
                    //     recordStack[0] == 0x00)
                    // {
                    //     stream.Seek(recordStart + 1, SeekOrigin.Begin);
                    //     return new UnknownRecord()
                    //     {
                    //         RecordType = SetupType.Repeat,
                    //         Data = reader.ReadBytes(9)
                    //     };
                    // }
                    
                    //Unknown12 is 12 PP PP
                    if (separator[0] == 0x12 &&
                        recordStack.Count == 2)
                    {
                        stream.Seek(recordStart, SeekOrigin.Begin);
                        return new UnknownRecord()
                        {
                            RecordType = SetupType.Unknown12,
                            Data = reader.ReadBytes(2)
                        };
                    }
                    
                    //Unknown13 is 13 PP PP
                    if (separator[0] == 0x13 &&
                        recordStack.Count == 2)
                    {
                        stream.Seek(recordStart, SeekOrigin.Begin);
                        return new UnknownRecord()
                        {
                            RecordType = SetupType.Unknown13,
                            Data = reader.ReadBytes(2)
                        };
                    }
                    
                    //Unknown6 is 06 XX YY
                    if (separator[0] == 0x06 &&
                        recordStack.Count == 2)
                    {
                        stream.Seek(recordStart, SeekOrigin.Begin);
                        return new UnknownRecord()
                        {
                            RecordType = SetupType.Unknown6,
                            Data = reader.ReadBytes(2)
                        };
                    }
                    
                    recordStack.Add(reader.ReadByte());   
                }

                throw new Exception("Failed to find record");
            }

            public static Instruction3Record AsInstruction3(BinaryReader reader)
            {
                var data = reader.ReadUInt16();
                var type = (Instruction3Record.Instruction3Type)reader.ReadByte();
                return new Instruction3Record()
                {
                    RecordType = SetupType.Instruction3,
                    Type = type,
                    Data = data
                };
            }
            
            public static SetupAnimationRecord AsAnimation(BinaryReader reader, byte[] dataSection)
            {
                var pointer = reader.ReadUInt16();
                if (reader.ReadUInt16() != 0x05)
                {
                    throw new Exception("Expected separator 0x05");
                }

                var index = reader.ReadUInt16();
                if (reader.ReadUInt16() != 0x05)
                {
                    throw new Exception("Expected separator 0x05");
                }

                var u1 = reader.ReadUInt16();
                if (reader.ReadUInt16() != 0x05)
                {
                    throw new Exception("Expected separator 0x05");
                }

                var dx = reader.ReadInt16();
                if (reader.ReadUInt16() != 0x05)
                {
                    throw new Exception("Expected separator 0x05");
                }

                var dy = reader.ReadInt16();
                if (reader.ReadUInt16() != 0x05)
                {
                    throw new Exception("Expected separator 0x05");
                }

                var u2 = reader.ReadUInt16();

                var instructions = new List<InstructionRecord>();
                {
                    using var dataStream = new MemoryStream(dataSection);
                    using var dataReader = new BinaryReader(dataStream);

                    dataStream.Seek(pointer, SeekOrigin.Begin);
                    var type = InstructionRecord.InstructionType.Unknown;
                    var i = 0;
                    var instructionOffsets = new Dictionary<long, int>();
                    var end = false;
                    do
                    {
                        i++;
                        var startOffset = dataStream.Position;
                        instructionOffsets[startOffset] = i;
                        type = (InstructionRecord.InstructionType)dataReader.ReadByte();

                        InstructionRecord instruction;
                        switch (type)
                        {
                            case InstructionRecord.InstructionType.End:
                                instruction = new EndInstruction()
                                {
                                    Type = InstructionRecord.InstructionType.End
                                };
                                end = true;
                                break;
                            case InstructionRecord.InstructionType.ImageChange:
                                instruction = InstructionRecord.AsImageChange(dataReader);
                                break;
                            case InstructionRecord.InstructionType.PositionChange:
                                instruction = InstructionRecord.AsPositionChange(dataReader);
                                break;
                            case InstructionRecord.InstructionType.Delay:
                                instruction = InstructionRecord.AsDelay(dataReader);
                                break;
                            case InstructionRecord.InstructionType.Jump:
                                var offset = dataStream.Position;
                                var p = dataReader.ReadUInt16();
                                var n = false;
                                var delta = 0;
                                
                                //it's a jump to another set of instructions, usually a loop (so jumping backwards)
                                if (p > offset)
                                {
                                    //TODO: handle this case
                                    delta = -999;
                                }
                                else
                                {
                                    if (p == 0)
                                    {
                                        //it's a null pointer
                                        n = true;
                                    }
                                    else
                                    {
                                        if (!instructionOffsets.TryGetValue(p, out var targetInstructionIndex))
                                        {
                                            //TODO: handle this case same as forward instruction
                                            targetInstructionIndex = -999;
                                        }

                                        delta = targetInstructionIndex - i; //negative index
                                    }
                                }
                                instruction = new JumpInstruction()
                                {
                                    Type = InstructionRecord.InstructionType.Jump,
                                    Null = n,
                                    IndexDelta = delta 
                                };
                                break;
                            case InstructionRecord.InstructionType.Unknown8:
                                instruction = new Unknown8Instruction()
                                {
                                    Type = type
                                };
                                end = true;
                                break;
                            case InstructionRecord.InstructionType.Unknown9:
                                instruction = new UnknownInstruction()
                                {
                                    Type = type,
                                    Data = dataReader.ReadBytes(2)
                                };
                                break;
                            default:
                                instruction = new UnknownInstruction()
                                {
                                    Type = type
                                };
                                //throw new Exception("Unhandled type: " + type);
                                break;
                        }
                                                
                        instructions.Add(instruction);
                        
                    } while (!end && dataStream.Position < dataSection.Length);
                }

                return new SetupAnimationRecord()
                {
                    RecordType = SetupType.Animation,
                    Index = index,
                    PositionX = dx,
                    PositionY = dy,
                    Unknown1 = u1,
                    Unknown2 = u2,
                    Instructions = instructions
                };
            }
        }

        /// <summary>
        /// SetupRecord of type Animation.
        /// 2 bytes between separators. 6 groups, each 2 bytes (or a ushort).
        /// First group seems to mean 'start an animation starting at this pointer'
        /// Second group seems to be a numeric index of when to run?
        /// Third group is unclear but usually FF FF (Unknown1)
        /// Fourth group is the X position to use as the initial position
        /// Fifth group is the Y position to use as the initial position
        /// Sixth group is unclear but usually FF 00 (Unknown2)
        /// Seventh group is unclear but usually 00 00 00 or 01 00 00 (Unknown3/4/5) - subanimation?
        ///
        /// Instructions from pointer are parsed into this class.
        /// Position changes come from instructions.
        /// Which image to display comes from instructions.
        /// Multiple SetupAnimationRecords may reference the same list or subsets of the list. Part of the publish
        /// should involve correctly sharing a pointer for them where possible.
        /// </summary>
        public class SetupAnimationRecord : SetupRecord
        {
            public int Index { get; set; }
            public ushort Unknown1 { get; set; }
            public short PositionX { get; set; }
            public short PositionY { get; set; }
            public ushort Unknown2 { get; set; }

            public List<InstructionRecord> Instructions { get; set; } = new();
        }

        public class Instruction3Record : SetupRecord
        {
            public enum Instruction3Type
            {
                Unknown = -1,
                Padding = 0,
                Index1 = 1, //something to do with a draw instruction ID?
                WaitFrames = 2,
                KeepDrawingAfterEnd = 4,
            }
            
            public int Data { get; set; }

            public Instruction3Type Type { get; set; } = Instruction3Type.Unknown;
        }

        public class EndRecord : SetupRecord
        {
            public enum EndType
            {
                Unknown = -1,
                Loop = 0x14,
                Continue = 0x15
            }

            public EndType Type { get; set; } = EndType.Unknown;
        }

        public class UnknownRecord : SetupRecord
        {
            public byte[] Data { get; set; } = Array.Empty<byte>();
        }

        /// <summary>
        /// Record referenced at least from the SetupAnimationRecord, the type is determined by the first byte,
        /// and the number of bytes that follow depends on the type. A type of 0B determines the end of the instructions.
        /// Not fully understood, but at least two types are known: a wait type and a change to positions.
        /// </summary>
        [JsonDerivedType(typeof(DelayInstruction), "delay")]
        [JsonDerivedType(typeof(PositionChangeInstruction), "position")]
        [JsonDerivedType(typeof(UnknownInstruction), "unknown")]
        [JsonDerivedType(typeof(JumpInstruction), "pointer")]
        [JsonDerivedType(typeof(ImageChangeInstruction), "image")]
        [JsonDerivedType(typeof(Unknown8Instruction), "u8")]
        [JsonDerivedType(typeof(EndInstruction), "end")]
        public abstract class InstructionRecord
        {
            public enum InstructionType
            {
                Unknown = -1,
                ImageChange = 0,
                PositionChange = 2,
                Delay = 5,
                Jump = 6,
                Unknown8 = 8,
                Unknown9 = 9, //2 bytes?
                
                End = 0x0A,
            }

            public InstructionType Type { get; set; } = InstructionType.Unknown;
            
            public static ImageChangeInstruction AsImageChange(BinaryReader reader)
            {
                return new ImageChangeInstruction()
                {
                    Type = InstructionType.ImageChange,
                    Id = reader.ReadSByte()
                };
            }
            
            public static DelayInstruction AsDelay(BinaryReader reader)
            {
                return new DelayInstruction()
                {
                    Type = InstructionType.Delay,
                    Frames = reader.ReadInt16()
                };
            }

            public static PositionChangeInstruction AsPositionChange(BinaryReader reader)
            {
                return new PositionChangeInstruction()
                {
                    Type = InstructionType.PositionChange,
                    PositionX = reader.ReadInt16(),
                    PositionY = reader.ReadInt16()
                };
            }
        }

        public class DelayInstruction : InstructionRecord
        {
            public int Frames { get; set; }
        }
        
        public class ImageChangeInstruction : InstructionRecord
        {
            /// <summary>
            /// This is not the sequential image index (in the file), but
            /// the ID assigned based on the header information.
            /// </summary>
            public int Id { get; set; }
        }

        public class PositionChangeInstruction : InstructionRecord
        {
            /// <summary>
            /// DX every frame
            /// </summary>
            public int PositionX { get; set; }
            /// <summary>
            /// DY every frame
            /// </summary>
            public int PositionY { get; set; }
        }

        public class JumpInstruction : InstructionRecord
        {
            public bool Null { get; set; }
            public int IndexDelta { get; set; }
        }

        public class Unknown8Instruction : InstructionRecord
        {
        }
        
        public class EndInstruction : InstructionRecord
        {
        }
        
        public class UnknownInstruction : InstructionRecord
        {
            public byte[] Data { get; set; } = Array.Empty<byte>();
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
            /// Used by the game to enable/disable certain animation loops (alarm level in buildings)
            /// Not fully confirmed but tracks with some of the data
            /// Normally 1 for most animations.
            /// </summary>
            public int SubAnimationCount { get; set; }

            /// <summary>
            /// How the game draws a background before updating or drawing images based on the animations
            /// </summary>
            public BackgroundType BackgroundType { get; set; } = BackgroundType.Unknown;
            
            /// <summary>
            /// Color mapping for the entire set of images in the file 
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
            public byte Unknown1 { get; set; }

            /// <summary>
            /// Each image ID is assigned an index; this ID can only increase monotonically but can have gaps. 
            /// In the file format, this looks like a series of u16, where any 00 00 represents a gap.
            /// With no gaps in 500 bytes that means the maximum number of images is 250.
            /// Example: the start of a header with data AA AA  00 00  00 00  BB BB
            /// means image 0 is index 0, image 1 (B) is index 3 (gap of 2) 
            /// </summary>
            public Dictionary<int, int> ImageIdToIndex { get; set; } = new();

            /// <summary>
            /// Each image index has a set of data attached that seems arbitrary and does not relate to the file.
            /// This may be an authoring concern and not relevant to the file, except that 00 00 represents a gap.
            /// The data is encoded here as u16 but might be two separate u8s instead.
            /// Potentially this might be a grid position (X, Y) for display in the editor.
            /// Example: the start of a header with data AA AA  00 00  00 00  BB BB
            /// means index 0 has data AA AA, index 3 has data BB BB
            /// </summary>
            public Dictionary<int, int> ImageIndexToUnknownData { get; set; } = new();

            public List<SetupRecord> Records { get; set; } = new();

            public List<AnimationInstruction> Instructions { get; set; } = new();
            public Dictionary<string, int> InstructionLabels { get; set; } = new();
            public List<AnimationStep> Steps { get; set; } = new();
            public Dictionary<string, int> DataLabels { get; set; } = new();
        }
        
        /// <summary>
        /// ID that also determines the filename
        /// </summary>
        public string Key { get; set; } = string.Empty;

        public Dictionary<int, SimpleImageModel> Images { get; set; } = new();
        public Metadata ExtraData { get; set; } = new();
    }
}