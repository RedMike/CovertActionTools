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
                KeepSpriteDrawn = 12, //04, loads 2 stack, persist sprite X at current position after end
                PushParameterToStack = 1, //05 00 XX XX, push XX XX to stack
                PushRegisterToStack = 2, //05 01 XX XX, push register X to stack
                PopStackToRegister = 8, //06 XX XX, pops stack and sets register X to it
                Unknown07 = 6, //07
                CompareNotEqual = 7, //08, loads 2 stack, sets compare flag if most recent two stack entries are not equal
                CompareEqual = 11, //0B, loads 2 stack, sets compare flag if most recent two stack entries are equal
                Add = 13, //0E, loads 2 stack, adds most recent two stack entries together
                ConditionalJump = 3, //12 XX XX, conditional jump?
                Jump = 4, //13 XX XX, unconditional jump?
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
                Jump = 0x06,
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