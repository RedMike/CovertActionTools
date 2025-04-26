using System;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>
        /// Record that sets up some data or an animation loop of some sort, the type is identified by the number
        /// of bytes it contains. Each record is separated by 05 00, and 05 05 00 signals the end of these records.
        /// Not fully understood, but at least one type seems to set up an animation loop with a pointer to
        /// an InstructionRecord.
        /// </summary>
        [JsonDerivedType(typeof(SetupAnimationRecord))]
        [JsonDerivedType(typeof(UnknownRecord))]
        public abstract class SetupRecord
        {
            public enum SetupType
            {
                Unknown = -1,
                Unknown1 = 1,
                Animation = 2, //2 bytes between separators
                Unknown3 = 3,
                Unknown5 = 5,
                Unknown7 = 7,
                Unknown9 = 9,
                Unknown14 = 14,
            }

            public SetupType Type { get; set; } = SetupType.Unknown;

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
                    do
                    {
                        type = (InstructionRecord.InstructionType)dataReader.ReadByte();
                        if (type == InstructionRecord.InstructionType.End)
                        {
                            continue;
                        }

                        InstructionRecord instruction;
                        byte[] data;
                        switch (type)
                        {
                            case InstructionRecord.InstructionType.ImageChange:
                                instruction = InstructionRecord.AsImageChange(dataReader);
                                break;
                            case InstructionRecord.InstructionType.PositionChange:
                                instruction = InstructionRecord.AsPositionChange(dataReader);
                                break;
                            case InstructionRecord.InstructionType.Delay:
                                instruction = InstructionRecord.AsDelay(dataReader);
                                break;
                            case InstructionRecord.InstructionType.Pointer:
                                instruction = InstructionRecord.AsPointer(dataReader);
                                break;
                            case InstructionRecord.InstructionType.Unknown8:
                                instruction = new UnknownInstruction()
                                {
                                    Type = type,
                                    Data = dataReader.ReadBytes(6)
                                };
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
                        
                    } while (type != InstructionRecord.InstructionType.End && dataStream.Position < dataSection.Length);
                }

                return new SetupAnimationRecord()
                {
                    Type = SetupType.Animation,
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

        public class UnknownRecord : SetupRecord
        {
            public byte[] Data { get; set; } = Array.Empty<byte>();
        }

        /// <summary>
        /// Record referenced at least from the SetupAnimationRecord, the type is determined by the first byte,
        /// and the number of bytes that follow depends on the type. A type of 0B determines the end of the instructions.
        /// Not fully understood, but at least two types are known: a wait type and a change to positions.
        /// </summary>
        [JsonDerivedType(typeof(DelayInstruction))]
        [JsonDerivedType(typeof(PositionChangeInstruction))]
        [JsonDerivedType(typeof(UnknownInstruction))]
        [JsonDerivedType(typeof(PointerInstruction))]
        [JsonDerivedType(typeof(ImageChangeInstruction))]
        public abstract class InstructionRecord
        {
            public enum InstructionType
            {
                Unknown = -1,
                ImageChange = 0,
                PositionChange = 2,
                Delay = 5,
                Pointer = 6, //maybe a 'continue from here'?
                Unknown8 = 8, //6 bytes?
                Unknown9 = 9, //2 bytes?
                Unknown20 = 20, //0 bytes?
                Unknown21 = 21, //0 bytes?
                
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

            public static PointerInstruction AsPointer(BinaryReader reader)
            {
                return new PointerInstruction()
                {
                    Type = InstructionType.Pointer,
                    Pointer = reader.ReadUInt16()
                };
            }
        }

        public class DelayInstruction : InstructionRecord
        {
            public int Frames { get; set; }
        }
        
        public class ImageChangeInstruction : InstructionRecord
        {
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

        public class PointerInstruction : InstructionRecord
        {
            //TODO: this shouldn't be a pointer because that context gets lost, but recursion?
            public int Pointer { get; set; }
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
            /// Unknown data that serves as a header to the image.
            /// Seems to contain some info about setting up the animation loops, but changes either have no effect or
            /// break the animation entirely and fails to load.
            /// When background type is ClearToColor, first byte is the background color to clear to.
            /// When background type is PreviousAnimation, length 500; otherwise length 502.
            /// </summary>
            public byte[] HeaderData { get; set; } = Array.Empty<byte>();

            public List<SetupRecord> Records { get; set; } = new();
        }
        
        /// <summary>
        /// ID that also determines the filename
        /// </summary>
        public string Key { get; set; } = string.Empty;

        public Dictionary<int, SimpleImageModel> Images { get; set; } = new();
        public Metadata ExtraData { get; set; } = new();
    }
}