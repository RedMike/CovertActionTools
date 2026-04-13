using System;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Two player compass-direction word variables at DS:0x1C42..0x1C45. Held in their
    /// own section because they are pure runtime state — FUN_10e8_070e writes
    /// <see cref="PlayerFacingDirection"/> at mission setup and FUN_10e8_0f4c rewrites
    /// both words on every keyboard tick, so the EXE's stored bytes are not meaningful
    /// to view or edit.
    ///
    ///   <see cref="PlayerFacingDirection"/>: FUN_10e8_0f4c's active compass direction
    ///     used to index the 9-entry MovementPixel and JumpingTile arrays at
    ///     0x666/0x678/0x68a/0x69c. Values 0..8 (0 = stationary; 1..8 = N,NE,E,SE,S,SW,W,NW).
    ///   <see cref="PlayerInputDirection"/>: the direction the player just requested via
    ///     keyboard input. FUN_10e8_0f4c copies it into <see cref="PlayerFacingDirection"/>
    ///     once the movement commits.
    /// </summary>
    public class TacPlayerDirectionStateSection : IExecutableSection
    {
        public const int SectionSize = 4;

        public ushort PlayerFacingDirection { get; set; }
        public ushort PlayerInputDirection { get; set; }

        public bool Viewable()
        {
            return false;
        }

        public bool Editable()
        {
            return false;
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            PlayerFacingDirection = BitConverter.ToUInt16(fullPayload, startingOffset);
            PlayerInputDirection = BitConverter.ToUInt16(fullPayload, startingOffset + 2);
            return SectionSize;
        }

        public byte[] WriteBytes()
        {
            return new[]
            {
                (byte)(PlayerFacingDirection & 0xFF),
                (byte)((PlayerFacingDirection >> 8) & 0xFF),
                (byte)(PlayerInputDirection & 0xFF),
                (byte)((PlayerInputDirection >> 8) & 0xFF)
            };
        }

        public TacPlayerDirectionStateSection Clone()
        {
            return new TacPlayerDirectionStateSection
            {
                PlayerFacingDirection = PlayerFacingDirection,
                PlayerInputDirection = PlayerInputDirection
            };
        }
    }
}
