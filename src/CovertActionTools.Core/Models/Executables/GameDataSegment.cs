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

        /// <summary>Data between char name pointer table and clue relationship pointers: status labels, structured data, dialogue, month data, clue/month strings.</summary>
        public byte[] MidSection { get; set; } = Array.Empty<byte>();

        // TODO: ClueRelationshipPointers (40) and MonthNamePointers (12) have shared/duplicate
        // string references — multiple pointers point to the same physical string. Extracting
        // and recomputing requires deduplication logic. For now, stored as-is.
        /// <summary>40 DS-relative pointers to clue relationship phrases.</summary>
        public ushort[] ClueRelationshipPointers { get; set; } = Array.Empty<ushort>();

        /// <summary>48-byte lookup table (values include 0,1,2,4,8 + popcount pattern), undecoded.</summary>
        public byte[] UnknownLookupTable { get; set; } = Array.Empty<byte>();

        /// <summary>12 DS-relative pointers to month name abbreviations.</summary>
        public ushort[] MonthNamePointers { get; set; } = Array.Empty<ushort>();

        /// <summary>Clue relationship phrases (extracted from pointers, read-only convenience).</summary>
        public string[] ClueRelationshipPhrases { get; set; } = Array.Empty<string>();

        /// <summary>Month name abbreviations (extracted from pointers, read-only convenience).</summary>
        public string[] MonthNames { get; set; } = Array.Empty<string>();

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
            segment.MidSection = DataSegmentHelper.Slice(dataSegment, charPtrsEnd, ClueRelPtrsOffset - charPtrsEnd);

            segment.ClueRelationshipPointers = DataSegmentHelper.BytesToUInt16Array(dataSegment, ClueRelPtrsOffset, ClueRelPtrCount);
            segment.ClueRelationshipPhrases = DataSegmentHelper.ExtractStringsFromPointers(segment.ClueRelationshipPointers, dataSegment);

            segment.UnknownLookupTable = DataSegmentHelper.Slice(dataSegment, UnknownLookupOffset, UnknownLookupSize);

            segment.MonthNamePointers = DataSegmentHelper.BytesToUInt16Array(dataSegment, MonthNamePtrsOffset, MonthNamePtrCount);
            segment.MonthNames = DataSegmentHelper.ExtractStringsFromPointers(segment.MonthNamePointers, dataSegment);

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

            return DataSegmentHelper.Concatenate(
                PreCharNameData,
                charNamesBytes,
                PostCharNameData,
                DataSegmentHelper.UInt16ArrayToBytes(charNamePointers),
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
                PreCharNameData = PreCharNameData.ToArray(),
                CharacterNames = CharacterNames.Select(s => s).ToArray(),
                PostCharNameData = PostCharNameData.ToArray(),
                MidSection = MidSection.ToArray(),
                ClueRelationshipPointers = ClueRelationshipPointers.ToArray(),
                ClueRelationshipPhrases = ClueRelationshipPhrases.Select(s => s).ToArray(),
                UnknownLookupTable = UnknownLookupTable.ToArray(),
                MonthNamePointers = MonthNamePointers.ToArray(),
                MonthNames = MonthNames.Select(s => s).ToArray(),
                TrailingData = TrailingData.ToArray()
            };
        }
    }
}
