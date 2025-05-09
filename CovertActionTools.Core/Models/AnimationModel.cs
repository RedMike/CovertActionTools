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
                SetupSprite = 0, //00, loads 7 * 2 stack (pointer, index, u1, x, y, u2, u3), add active sprite
                RemoveSprite = 1, //01, loads 2 stack, stops/removes target sprite
                WaitForFrames = 2, //02, loads 2 stack, render for X frames
                Unknown03 = 3, //03, loads 2 stack, TODO: not in CA, in RRM, no apparent effect
                StampSprite = 4, //04, loads 2 stack, saves a persistent copy of the sprite at the current position/image
                PushToStack = 5, //05 00 XX XX, push XX XX to stack
                PushRegisterToStack = 261, //05 01 XX XX, push register X to stack
                PopStackToRegister = 6, //06 XX XX, pops stack and sets register X to value
                Unknown07 = 7, //07, TODO: no apparent effect/used in switch statements
                CompareEqual = 8, //08, loads 2 stack, sets compare flag if most recent two stack entries are equal
                CompareNotEqual = 11, //0B, loads 2 stack, sets compare flag if most recent two stack entries are not equal
                Add = 14, //0E, loads 2 stack, pops most recent two stack entries, adds them together and pushes it
                ConditionalJump = 12, //12 XX XX, jumps only if compare flag is set
                Jump = 19, //13 XX XX, always jumps, TODO: unclear why some existing files have two 13's one after the other
                End = 20, //14, TODO: wait behaviour? some animations do wait, others start a new animation immediately
                Unknown15 = 21, //15, TODO: related to wait behaviour? some animations use 15 right before 1; not loop because TITLE2 uses it
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
                Stop = 0x0A
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