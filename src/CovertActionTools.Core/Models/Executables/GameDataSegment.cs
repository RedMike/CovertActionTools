using System;
using System.Linq;

namespace CovertActionTools.Core.Models.Executables
{
    /// <summary>
    /// Structured data segment for GAME.EXE.
    /// Field boundaries and interpretations are based on reverse engineering and may not
    /// be fully accurate. Unknown regions are preserved as raw byte arrays.
    /// </summary>
    public class GameDataSegment
    {
        /// <summary>DS paragraph value for GAME.EXE.</summary>
        public const int DsParagraph = 0x1206;

        #region Layout Constants (DS-relative offsets)
        private const int CharNamePointersOffset = 0x1C2A; // 0x013C8A - 0x12060
        private const int CharNamePointerCount = 192;
        private const int ClueRelPtrsOffset = 0x2B3E;      // 0x014B9E - 0x12060
        private const int ClueRelPtrCount = 40;
        private const int UnknownLookupOffset = 0x2B8E;     // 0x014BEE - 0x12060
        private const int UnknownLookupSize = 48;
        private const int MonthNamePtrsOffset = 0x2BBE;     // 0x014C1E - 0x12060
        private const int MonthNamePtrCount = 12;
        #endregion

        #region Fields (in binary order)

        /// <summary>Data before character name pointers: runtime, BSS, structured data, file refs, character names.</summary>
        public byte[] PreCharNamePtrData { get; set; } = Array.Empty<byte>();

        /// <summary>192 DS-relative pointers into character name strings.</summary>
        public ushort[] CharacterNamePointers { get; set; } = Array.Empty<ushort>();

        /// <summary>Data between char name pointers and clue relationship pointers: status labels, structured data, dialogue, month data.</summary>
        public byte[] MidSection { get; set; } = Array.Empty<byte>();

        /// <summary>40 DS-relative pointers to clue relationship phrases.</summary>
        public ushort[] ClueRelationshipPointers { get; set; } = Array.Empty<ushort>();

        /// <summary>48-byte lookup table (values include 0,1,2,4,8 + popcount pattern), undecoded.</summary>
        public byte[] UnknownLookupTable { get; set; } = Array.Empty<byte>();

        /// <summary>12 DS-relative pointers to month name abbreviations.</summary>
        public ushort[] MonthNamePointers { get; set; } = Array.Empty<ushort>();

        /// <summary>Everything after month name pointers: item/clue strings, CIA strings, file management, overlay, C runtime, BSS.</summary>
        public byte[] TrailingData { get; set; } = Array.Empty<byte>();

        #endregion

        public static GameDataSegment FromBytes(byte[] dataSegment)
        {
            var segment = new GameDataSegment();

            segment.PreCharNamePtrData = DataSegmentHelper.Slice(dataSegment, 0, CharNamePointersOffset);

            segment.CharacterNamePointers = DataSegmentHelper.BytesToUInt16Array(dataSegment, CharNamePointersOffset, CharNamePointerCount);

            var charPtrsEnd = CharNamePointersOffset + CharNamePointerCount * 2;
            segment.MidSection = DataSegmentHelper.Slice(dataSegment, charPtrsEnd, ClueRelPtrsOffset - charPtrsEnd);

            segment.ClueRelationshipPointers = DataSegmentHelper.BytesToUInt16Array(dataSegment, ClueRelPtrsOffset, ClueRelPtrCount);

            segment.UnknownLookupTable = DataSegmentHelper.Slice(dataSegment, UnknownLookupOffset, UnknownLookupSize);

            segment.MonthNamePointers = DataSegmentHelper.BytesToUInt16Array(dataSegment, MonthNamePtrsOffset, MonthNamePtrCount);

            var monthPtrsEnd = MonthNamePtrsOffset + MonthNamePtrCount * 2;
            segment.TrailingData = DataSegmentHelper.Slice(dataSegment, monthPtrsEnd, dataSegment.Length - monthPtrsEnd);

            return segment;
        }

        public byte[] ToBytes()
        {
            return DataSegmentHelper.Concatenate(
                PreCharNamePtrData,
                DataSegmentHelper.UInt16ArrayToBytes(CharacterNamePointers),
                MidSection,
                DataSegmentHelper.UInt16ArrayToBytes(ClueRelationshipPointers),
                UnknownLookupTable,
                DataSegmentHelper.UInt16ArrayToBytes(MonthNamePointers),
                TrailingData
            );
        }

        public GameDataSegment Clone()
        {
            return new GameDataSegment
            {
                PreCharNamePtrData = PreCharNamePtrData.ToArray(),
                CharacterNamePointers = CharacterNamePointers.ToArray(),
                MidSection = MidSection.ToArray(),
                ClueRelationshipPointers = ClueRelationshipPointers.ToArray(),
                UnknownLookupTable = UnknownLookupTable.ToArray(),
                MonthNamePointers = MonthNamePointers.ToArray(),
                TrailingData = TrailingData.ToArray()
            };
        }
    }
}
