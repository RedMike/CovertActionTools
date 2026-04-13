using System;
using CovertActionTools.Core.Models.Executables.Records.Shared;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// 18-byte block at DS:0x1C78..0x1C89 holding TAC's direction → BIOS scancode
    /// lookup table. Each of the 9 entries maps a compass direction
    /// (0=Stationary, 1..8=N,NE,E,SE,S,SW,W,NW) to a BIOS keyboard scan code.
    /// Indexed by FUN_10e8_0f4c at
    /// <c>local_16 = *(int *)(local_16 * 2 + 0x1c78) + 0x80</c>.
    /// </summary>
    public class TacInputConfigSection : IExecutableSection
    {
        public const int SectionSize = 18;

        public BiosKeyboardScanCode StationaryKey { get; set; }
        public BiosKeyboardScanCode NorthKey { get; set; }
        public BiosKeyboardScanCode NorthEastKey { get; set; }
        public BiosKeyboardScanCode EastKey { get; set; }
        public BiosKeyboardScanCode SouthEastKey { get; set; }
        public BiosKeyboardScanCode SouthKey { get; set; }
        public BiosKeyboardScanCode SouthWestKey { get; set; }
        public BiosKeyboardScanCode WestKey { get; set; }
        public BiosKeyboardScanCode NorthWestKey { get; set; }

        public bool Viewable()
        {
            return true;
        }

        public bool Editable()
        {
            return true;
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            StationaryKey = ReadScanCode(fullPayload, startingOffset, 0);
            NorthKey = ReadScanCode(fullPayload, startingOffset, 1);
            NorthEastKey = ReadScanCode(fullPayload, startingOffset, 2);
            EastKey = ReadScanCode(fullPayload, startingOffset, 3);
            SouthEastKey = ReadScanCode(fullPayload, startingOffset, 4);
            SouthKey = ReadScanCode(fullPayload, startingOffset, 5);
            SouthWestKey = ReadScanCode(fullPayload, startingOffset, 6);
            WestKey = ReadScanCode(fullPayload, startingOffset, 7);
            NorthWestKey = ReadScanCode(fullPayload, startingOffset, 8);
            return SectionSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[SectionSize];
            WriteScanCode(result, 0, StationaryKey);
            WriteScanCode(result, 1, NorthKey);
            WriteScanCode(result, 2, NorthEastKey);
            WriteScanCode(result, 3, EastKey);
            WriteScanCode(result, 4, SouthEastKey);
            WriteScanCode(result, 5, SouthKey);
            WriteScanCode(result, 6, SouthWestKey);
            WriteScanCode(result, 7, WestKey);
            WriteScanCode(result, 8, NorthWestKey);
            return result;
        }

        public TacInputConfigSection Clone()
        {
            return new TacInputConfigSection
            {
                StationaryKey = StationaryKey,
                NorthKey = NorthKey,
                NorthEastKey = NorthEastKey,
                EastKey = EastKey,
                SouthEastKey = SouthEastKey,
                SouthKey = SouthKey,
                SouthWestKey = SouthWestKey,
                WestKey = WestKey,
                NorthWestKey = NorthWestKey
            };
        }

        private static BiosKeyboardScanCode ReadScanCode(byte[] data, int baseOffset, int index)
        {
            return (BiosKeyboardScanCode)BitConverter.ToUInt16(data, baseOffset + index * 2);
        }

        private static void WriteScanCode(byte[] dest, int index, BiosKeyboardScanCode code)
        {
            var value = (ushort)code;
            dest[index * 2] = (byte)(value & 0xFF);
            dest[index * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }
    }
}
