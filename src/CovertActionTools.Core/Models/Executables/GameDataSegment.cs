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
        private const int CharNamePointerCount = 192;
        private const int ClueRelPtrCount = 40;
        private const int UnknownLookupSize = 48;
        private const int MonthNamePtrCount = 12;

        // Original binary offsets (used for initial parse)
        private const int CharNamePointersOffset = 0x1C2A; // 0x013C8A - 0x12060
        private const int ClueRelPtrsOffset = 0x2B3E;      // 0x014B9E - 0x12060
        private const int UnknownLookupOffset = 0x2B8E;     // 0x014BEE - 0x12060
        private const int MonthNamePtrsOffset = 0x2BBE;     // 0x014C1E - 0x12060
        #endregion

        #region Fields (in binary order)

        /// <summary>Data before character names: runtime, BSS, structured data, file refs.</summary>
        public byte[] PreCharNameData { get; set; } = Array.Empty<byte>();

        /// <summary>192 character names (4 ethnic groups x female first / male first / male surname, 16 each).</summary>
        public string[] CharacterNames { get; set; } = Array.Empty<string>();

        /// <summary>Data between character names and character name pointer table.</summary>
        public byte[] PostCharNameData { get; set; } = Array.Empty<byte>();

        // CharacterNamePointers are computed at serialization time.

        /// <summary>Data between char name pointer table and clue phrases: status labels, structured data, dialogue.</summary>
        public byte[] MidSectionPreClue { get; set; } = Array.Empty<byte>();

        /// <summary>40 clue relationship phrases (e.g. " tied to ", " registered to ").</summary>
        public string[] ClueRelationshipPhrases { get; set; } = Array.Empty<string>();

        /// <summary>12 month name abbreviations (Jan-Dec).</summary>
        public string[] MonthNames { get; set; } = Array.Empty<string>();

        /// <summary>Data between month names and clue relationship pointer table.</summary>
        public byte[] MidSectionPostMonth { get; set; } = Array.Empty<byte>();

        // ClueRelationshipPointers and MonthNamePointers are computed at serialization time.

        /// <summary>48-byte lookup table (values include 0,1,2,4,8 + popcount pattern), undecoded.</summary>
        public byte[] UnknownLookupTable { get; set; } = Array.Empty<byte>();

        /// <summary>Everything after month name pointers: item/clue strings, CIA strings, file management, overlay, C runtime, BSS.</summary>
        public byte[] TrailingData { get; set; } = Array.Empty<byte>();

        #endregion

        public static GameDataSegment FromBytes(byte[] dataSegment)
        {
            var segment = new GameDataSegment();

            // Read character name pointers and extract names
            var charPtrs = DataSegmentHelper.BytesToUInt16Array(dataSegment, CharNamePointersOffset, CharNamePointerCount);
            segment.CharacterNames = DataSegmentHelper.ExtractStringsFromPointers(charPtrs, dataSegment);

            var (charBlockStart, charBlockEnd) = DataSegmentHelper.FindStringBlockBounds(charPtrs, dataSegment);
            segment.PreCharNameData = DataSegmentHelper.Slice(dataSegment, 0, charBlockStart);
            var postCharLen = CharNamePointersOffset - charBlockEnd;
            segment.PostCharNameData = postCharLen > 0
                ? DataSegmentHelper.Slice(dataSegment, charBlockEnd, postCharLen)
                : Array.Empty<byte>();

            var charPtrsEnd = CharNamePointersOffset + CharNamePointerCount * 2;

            // Extract clue phrases and month names from MidSection using their pointer tables
            var cluePtrs = DataSegmentHelper.BytesToUInt16Array(dataSegment, ClueRelPtrsOffset, ClueRelPtrCount);
            segment.ClueRelationshipPhrases = DataSegmentHelper.ExtractStringsFromPointers(cluePtrs, dataSegment);
            var (clueBlockStart, clueBlockEnd) = DataSegmentHelper.FindStringBlockBounds(cluePtrs, dataSegment);

            var monthPtrs = DataSegmentHelper.BytesToUInt16Array(dataSegment, MonthNamePtrsOffset, MonthNamePtrCount);
            segment.MonthNames = DataSegmentHelper.ExtractStringsFromPointers(monthPtrs, dataSegment);
            var (_, monthBlockEnd) = DataSegmentHelper.FindStringBlockBounds(monthPtrs, dataSegment);

            // Split MidSection: pre-clue | clue phrases | month names | post-month
            segment.MidSectionPreClue = DataSegmentHelper.Slice(dataSegment, charPtrsEnd, clueBlockStart - charPtrsEnd);
            segment.MidSectionPostMonth = DataSegmentHelper.Slice(dataSegment, monthBlockEnd, ClueRelPtrsOffset - monthBlockEnd);

            // Clue pointer table, unknown lookup, month pointer table
            segment.UnknownLookupTable = DataSegmentHelper.Slice(dataSegment, UnknownLookupOffset, UnknownLookupSize);

            var monthPtrsEnd = MonthNamePtrsOffset + MonthNamePtrCount * 2;
            segment.TrailingData = DataSegmentHelper.Slice(dataSegment, monthPtrsEnd, dataSegment.Length - monthPtrsEnd);

            return segment;
        }

        public byte[] ToBytes()
        {
            var charNamesBytes = DataSegmentHelper.NullTerminatedStringsToBytes(CharacterNames);

            // Compute character name pointers
            var charNamesBase = PreCharNameData.Length;
            var charNamePointers = DataSegmentHelper.ComputeStringPointers(CharacterNames, charNamesBase);

            var cluePhrasesBytes = DataSegmentHelper.NullTerminatedStringsToBytes(ClueRelationshipPhrases);
            var monthNamesBytes = DataSegmentHelper.NullTerminatedStringsToBytes(MonthNames);

            // Compute clue and month pointer values
            var clueBase = charNamesBase + charNamesBytes.Length + PostCharNameData.Length
                + CharNamePointerCount * 2 + MidSectionPreClue.Length;
            var cluePointers = DataSegmentHelper.ComputeStringPointers(ClueRelationshipPhrases, clueBase);

            var monthBase = clueBase + cluePhrasesBytes.Length;
            var monthPointers = DataSegmentHelper.ComputeStringPointers(MonthNames, monthBase);

            return DataSegmentHelper.Concatenate(
                PreCharNameData,
                charNamesBytes,
                PostCharNameData,
                DataSegmentHelper.UInt16ArrayToBytes(charNamePointers),
                MidSectionPreClue,
                cluePhrasesBytes,
                monthNamesBytes,
                MidSectionPostMonth,
                DataSegmentHelper.UInt16ArrayToBytes(cluePointers),
                UnknownLookupTable,
                DataSegmentHelper.UInt16ArrayToBytes(monthPointers),
                TrailingData
            );
        }

        public GameDataSegment Clone()
        {
            return new GameDataSegment
            {
                PreCharNameData = PreCharNameData.ToArray(),
                CharacterNames = CharacterNames.Select(s => s).ToArray(),
                PostCharNameData = PostCharNameData.ToArray(),
                MidSectionPreClue = MidSectionPreClue.ToArray(),
                ClueRelationshipPhrases = ClueRelationshipPhrases.Select(s => s).ToArray(),
                MonthNames = MonthNames.Select(s => s).ToArray(),
                MidSectionPostMonth = MidSectionPostMonth.ToArray(),
                UnknownLookupTable = UnknownLookupTable.ToArray(),
                TrailingData = TrailingData.ToArray()
            };
        }
    }
}
