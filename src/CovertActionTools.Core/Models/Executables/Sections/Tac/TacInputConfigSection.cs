using System;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// 20-byte block at DS:0x1C78..0x1C8B holding TAC's direction → BIOS scancode
    /// lookup table and the <see cref="CombatAlertedFlag"/> runtime-state word. The
    /// whole section is non-viewable and non-editable — the scancode table is loaded
    /// once into the keyboard-input dispatcher and the combat flag is runtime state
    /// that the guard/combat AI overwrites on every tick, so there is no meaningful
    /// static edit a user could make.
    ///
    ///   - <see cref="DirectionScanCodes"/> at DS:0x1C78 (9 × uint16 = 18 bytes):
    ///     direction index (0=Stationary, 1..8=N,NE,E,SE,S,SW,W,NW) → BIOS scan code.
    ///     Entry 0 is 0x0000 (no key). Entries 1..8 are Up(0x48), PgUp(0x49),
    ///     Right(0x4D), PgDn(0x51), Down(0x50), End(0x4F), Left(0x4B), Home(0x47).
    ///     Indexed by FUN_10e8_0f4c at
    ///     <c>local_16 = *(int *)(local_16 * 2 + 0x1c78) + 0x80</c>.
    ///   - <see cref="CombatAlertedFlag"/> at DS:0x1C8A (uint16): a boolean flag
    ///     toggled by FUN_10e8_1b32 via <c>MOV word ptr [0x1c8a], 0</c> and
    ///     <c>MOV word ptr [0x1c8a], 1</c>, and gated on with <c>CMP word ptr
    ///     [0x1c8a], 0</c>.
    /// </summary>
    public class TacInputConfigSection : IExecutableSection
    {
        public const int SectionSize = 20;
        public const int DirectionCount = 9;
        private const int DirectionBytes = DirectionCount * 2;

        public ushort[] DirectionScanCodes { get; set; } = new ushort[DirectionCount];
        public ushort CombatAlertedFlag { get; set; }

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
            DirectionScanCodes = new ushort[DirectionCount];
            for (var i = 0; i < DirectionCount; i++)
            {
                DirectionScanCodes[i] = BitConverter.ToUInt16(fullPayload, startingOffset + i * 2);
            }
            CombatAlertedFlag = BitConverter.ToUInt16(fullPayload, startingOffset + DirectionBytes);
            return SectionSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[SectionSize];
            for (var i = 0; i < DirectionCount; i++)
            {
                result[i * 2] = (byte)(DirectionScanCodes[i] & 0xFF);
                result[i * 2 + 1] = (byte)((DirectionScanCodes[i] >> 8) & 0xFF);
            }
            result[DirectionBytes] = (byte)(CombatAlertedFlag & 0xFF);
            result[DirectionBytes + 1] = (byte)((CombatAlertedFlag >> 8) & 0xFF);
            return result;
        }

        public TacInputConfigSection Clone()
        {
            var result = new TacInputConfigSection
            {
                CombatAlertedFlag = CombatAlertedFlag,
                DirectionScanCodes = new ushort[DirectionCount]
            };
            Array.Copy(DirectionScanCodes, result.DirectionScanCodes, DirectionCount);
            return result;
        }
    }
}
