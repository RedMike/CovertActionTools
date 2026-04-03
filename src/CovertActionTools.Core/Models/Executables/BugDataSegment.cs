using System;
using System.Linq;

namespace CovertActionTools.Core.Models.Executables
{
    /// <summary>
    /// Structured data segment for BUG.EXE.
    /// Field boundaries and interpretations are based on reverse engineering and may not
    /// be fully accurate. Unknown regions are preserved as raw byte arrays.
    /// </summary>
    public class BugDataSegment
    {
        /// <summary>DS paragraph value for BUG.EXE.</summary>
        public const int DsParagraph = 0x0A63;

        #region Layout Constants (DS-relative offsets)
        private const int ClueRelPtrCount = 40;
        private const int CharNamePointerCount = 192;
        private const int RectDrawRecordCount = 49;

        // Original binary offsets (used for initial parse)
        private const int ClueRelPtrsOffset = 0x1A40;     // 0x00C070 - 0x0A630
        private const int CharNamePointersOffset = 0x296A; // 0x00CF9A - 0x0A630
        private const int RectDrawRecordsOffset = 0x2B62;  // 0x00D192 - 0x0A630
        #endregion

        #region Fields (in binary order)

        /// <summary>Data before clue relationship pointers: runtime, flag table, BSS, structured data, BUG-unique strings, clue phrases, item tables.</summary>
        public byte[] PreClueRelPtrData { get; set; } = Array.Empty<byte>();

        // TODO: ClueRelationshipPointers (40) have shared/duplicate string references.
        // Extracting and recomputing requires deduplication logic. For now, stored as-is.
        /// <summary>40 DS-relative pointers to clue relationship phrases.</summary>
        public ushort[] ClueRelationshipPointers { get; set; } = Array.Empty<ushort>();

        /// <summary>Clue relationship phrases (extracted from pointers, read-only convenience).</summary>
        public string[] ClueRelationshipPhrases { get; set; } = Array.Empty<string>();

        /// <summary>Data between clue pointers and character names: category table, lookup data, item/clue strings, investigation methods, intel text.</summary>
        public byte[] MidSectionPreCharNames { get; set; } = Array.Empty<byte>();

        /// <summary>192 character names (4 ethnic groups x female first / male first / male surname, 16 each).</summary>
        public string[] CharacterNames { get; set; } = Array.Empty<string>();

        /// <summary>Data between character names and character name pointer table.</summary>
        public byte[] PostCharNameData { get; set; } = Array.Empty<byte>();

        // CharacterNamePointers are computed at serialization time.

        /// <summary>Data between char name pointers and rect draw records: status labels.</summary>
        public byte[] PostCharNamePtrData { get; set; } = Array.Empty<byte>();

        /// <summary>49 rectangle drawing records (12 bytes each): flag + coordinates + colour for screen layout.</summary>
        public RectDrawRecord[] RectDrawRecords { get; set; } = Array.Empty<RectDrawRecord>();

        /// <summary>Trailer after rect draw records: FF FF sentinel + 8 zero bytes.</summary>
        public byte[] RectDrawTrailer { get; set; } = Array.Empty<byte>();

        /// <summary>Everything after: loading text, infrastructure, overlay, C runtime, BSS.</summary>
        public byte[] TrailingData { get; set; } = Array.Empty<byte>();

        #endregion

        public static BugDataSegment FromBytes(byte[] dataSegment)
        {
            var segment = new BugDataSegment();

            segment.PreClueRelPtrData = DataSegmentHelper.Slice(dataSegment, 0, ClueRelPtrsOffset);

            segment.ClueRelationshipPointers = DataSegmentHelper.BytesToUInt16Array(dataSegment, ClueRelPtrsOffset, ClueRelPtrCount);
            segment.ClueRelationshipPhrases = DataSegmentHelper.ExtractStringsFromPointers(segment.ClueRelationshipPointers, dataSegment);

            var clueRelEnd = ClueRelPtrsOffset + ClueRelPtrCount * 2;

            // Extract character names using pointer table
            var charPtrs = DataSegmentHelper.BytesToUInt16Array(dataSegment, CharNamePointersOffset, CharNamePointerCount);
            segment.CharacterNames = DataSegmentHelper.ExtractStringsFromPointers(charPtrs, dataSegment);

            var (charBlockStart, charBlockEnd) = DataSegmentHelper.FindStringBlockBounds(charPtrs, dataSegment);
            segment.MidSectionPreCharNames = DataSegmentHelper.Slice(dataSegment, clueRelEnd, charBlockStart - clueRelEnd);
            var postCharLen = CharNamePointersOffset - charBlockEnd;
            segment.PostCharNameData = postCharLen > 0
                ? DataSegmentHelper.Slice(dataSegment, charBlockEnd, postCharLen)
                : Array.Empty<byte>();

            var charPtrsEnd = CharNamePointersOffset + CharNamePointerCount * 2;
            segment.PostCharNamePtrData = DataSegmentHelper.Slice(dataSegment, charPtrsEnd, RectDrawRecordsOffset - charPtrsEnd);

            segment.RectDrawRecords = new RectDrawRecord[RectDrawRecordCount];
            for (var i = 0; i < RectDrawRecordCount; i++)
            {
                segment.RectDrawRecords[i] = RectDrawRecord.FromBytes(dataSegment, RectDrawRecordsOffset + i * RectDrawRecord.RecordSize);
            }

            var rectEnd = RectDrawRecordsOffset + RectDrawRecordCount * RectDrawRecord.RecordSize;
            var trailerSize = 10; // FF FF sentinel + 8 zero bytes
            segment.RectDrawTrailer = DataSegmentHelper.Slice(dataSegment, rectEnd, trailerSize);

            var structEnd = rectEnd + trailerSize;
            segment.TrailingData = DataSegmentHelper.Slice(dataSegment, structEnd, dataSegment.Length - structEnd);

            return segment;
        }

        public byte[] ToBytes()
        {
            var charNamesBytes = DataSegmentHelper.NullTerminatedStringsToBytes(CharacterNames);

            // Compute character name pointers
            var charNamesBase = PreClueRelPtrData.Length + ClueRelPtrCount * 2
                + MidSectionPreCharNames.Length;
            var charNamePointers = DataSegmentHelper.ComputeStringPointers(CharacterNames, charNamesBase);

            var rectBytes = new byte[RectDrawRecords.Length * RectDrawRecord.RecordSize];
            for (var i = 0; i < RectDrawRecords.Length; i++)
            {
                Array.Copy(RectDrawRecords[i].ToBytes(), 0, rectBytes, i * RectDrawRecord.RecordSize, RectDrawRecord.RecordSize);
            }

            return DataSegmentHelper.Concatenate(
                PreClueRelPtrData,
                DataSegmentHelper.UInt16ArrayToBytes(ClueRelationshipPointers),
                MidSectionPreCharNames,
                charNamesBytes,
                PostCharNameData,
                DataSegmentHelper.UInt16ArrayToBytes(charNamePointers),
                PostCharNamePtrData,
                rectBytes,
                RectDrawTrailer,
                TrailingData
            );
        }

        public BugDataSegment Clone()
        {
            return new BugDataSegment
            {
                PreClueRelPtrData = PreClueRelPtrData.ToArray(),
                ClueRelationshipPointers = ClueRelationshipPointers.ToArray(),
                ClueRelationshipPhrases = ClueRelationshipPhrases.Select(s => s).ToArray(),
                MidSectionPreCharNames = MidSectionPreCharNames.ToArray(),
                CharacterNames = CharacterNames.Select(s => s).ToArray(),
                PostCharNameData = PostCharNameData.ToArray(),
                PostCharNamePtrData = PostCharNamePtrData.ToArray(),
                RectDrawRecords = RectDrawRecords.Select(r => r.Clone()).ToArray(),
                RectDrawTrailer = RectDrawTrailer.ToArray(),
                TrailingData = TrailingData.ToArray()
            };
        }
    }
}
