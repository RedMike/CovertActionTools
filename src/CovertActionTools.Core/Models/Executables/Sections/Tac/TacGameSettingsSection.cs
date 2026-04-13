using System;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Three word-sized game setting variables sitting between the CGA color remap table
    /// and the VGA palette remap tables. All three are directly code-referenced (anchor
    /// scan hits MOV/CMP instructions against 0x1BC8/0x1BCA/0x1BCC):
    ///   - <see cref="Speed"/> (DS:0x1BC8): game speed level (0..3). FUN_10e8_0018 sets
    ///     it at startup from <c>(uRam0001d430 &gt;&gt; 8) &amp; 3</c>; FUN_10e8_0f4c
    ///     rewrites it from the 'd'/'f'/'s' keyboard keys (values 1/2/0).
    ///   - <see cref="RoomEnabledMask"/> (DS:0x1BCA): bitmask ANDed against each room
    ///     type's <c>Enabled</c> field in FUN_10e8_4f8d's room-placement loop
    ///     (<c>(RoomType[i].Enabled &amp; uRam000129fa) != 0</c>) — controls which of the
    ///     room-type Enabled bits are considered active. Initial value 1 (matches the
    ///     vanilla hardcoded mask-of-1 documented on RoomTypeRecord).
    ///   - <see cref="FallbackFileIndex"/> (DS:0x1BCC): when FUN_10e8_cf7c's file load
    ///     returns -1, the function recursively calls itself with this index as the new
    ///     filename-table index to retry. Also used by FUN_10e8_bae6 on the disk-retry
    ///     prompt. Initial value 3.
    /// </summary>
    public class TacGameSettingsSection : IExecutableSection
    {
        public const int SectionSize = 6;

        private ushort Speed { get; set; }
        private ushort RoomEnabledMask { get; set; }
        private ushort FallbackFileIndex { get; set; }

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
            Speed = BitConverter.ToUInt16(fullPayload, startingOffset);
            RoomEnabledMask = BitConverter.ToUInt16(fullPayload, startingOffset + 2);
            FallbackFileIndex = BitConverter.ToUInt16(fullPayload, startingOffset + 4);
            return SectionSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[SectionSize];
            result[0] = (byte)(Speed & 0xFF);
            result[1] = (byte)((Speed >> 8) & 0xFF);
            result[2] = (byte)(RoomEnabledMask & 0xFF);
            result[3] = (byte)((RoomEnabledMask >> 8) & 0xFF);
            result[4] = (byte)(FallbackFileIndex & 0xFF);
            result[5] = (byte)((FallbackFileIndex >> 8) & 0xFF);
            return result;
        }

        public TacGameSettingsSection Clone()
        {
            return new TacGameSettingsSection
            {
                Speed = Speed,
                RoomEnabledMask = RoomEnabledMask,
                FallbackFileIndex = FallbackFileIndex
            };
        }
    }
}
