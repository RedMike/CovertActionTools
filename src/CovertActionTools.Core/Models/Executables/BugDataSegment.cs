using System;
using System.Linq;

namespace CovertActionTools.Core.Models.Executables
{
    public class BugDataSegment
    {
        /// <summary>DS paragraph value for BUG.EXE.</summary>
        public const int DsParagraph = 0x0A63;

        #region Layout Constants (DS-relative offsets)
        private const int ClueRelPtrsOffset = 0x1A40;     // 0x00C070 - 0x0A630
        private const int ClueRelPtrCount = 40;
        private const int CharNamePointersOffset = 0x296C; // 0x00CF9C - 0x0A630
        private const int CharNamePointerCount = 191;
        private const int UnknownStructOffset = 0x2B62;    // 0x00D192 - 0x0A630
        private const int UnknownStructSize = 588;
        #endregion

        #region Fields (in binary order)

        /// <summary>Data before clue relationship pointers: runtime, flag table, BSS, structured data, BUG-unique strings, clue phrases, item tables.</summary>
        public byte[] PreClueRelPtrData { get; set; } = Array.Empty<byte>();

        /// <summary>40 DS-relative pointers to clue relationship phrases.</summary>
        public ushort[] ClueRelationshipPointers { get; set; } = Array.Empty<ushort>();

        /// <summary>Data between clue pointers and character name pointers: category table, lookup data, item/clue strings, investigation methods, intel text, character names.</summary>
        public byte[] MidSection { get; set; } = Array.Empty<byte>();

        /// <summary>191 DS-relative pointers into character name strings.</summary>
        public ushort[] CharacterNamePointers { get; set; } = Array.Empty<ushort>();

        /// <summary>Data between char name pointers and unknown structured block: status labels.</summary>
        public byte[] PostCharNamePtrData { get; set; } = Array.Empty<byte>();

        /// <summary>588-byte structured block of 12-byte records (flag + coordinates + colour), undecoded.</summary>
        public byte[] UnknownStructuredBlock { get; set; } = Array.Empty<byte>();

        /// <summary>Everything after: loading text, infrastructure, overlay, C runtime, BSS.</summary>
        public byte[] TrailingData { get; set; } = Array.Empty<byte>();

        #endregion

        public static BugDataSegment FromBytes(byte[] dataSegment)
        {
            var segment = new BugDataSegment();

            segment.PreClueRelPtrData = DataSegmentHelper.Slice(dataSegment, 0, ClueRelPtrsOffset);

            segment.ClueRelationshipPointers = DataSegmentHelper.BytesToUInt16Array(dataSegment, ClueRelPtrsOffset, ClueRelPtrCount);

            var clueRelEnd = ClueRelPtrsOffset + ClueRelPtrCount * 2;
            segment.MidSection = DataSegmentHelper.Slice(dataSegment, clueRelEnd, CharNamePointersOffset - clueRelEnd);

            segment.CharacterNamePointers = DataSegmentHelper.BytesToUInt16Array(dataSegment, CharNamePointersOffset, CharNamePointerCount);

            var charPtrsEnd = CharNamePointersOffset + CharNamePointerCount * 2;
            segment.PostCharNamePtrData = DataSegmentHelper.Slice(dataSegment, charPtrsEnd, UnknownStructOffset - charPtrsEnd);

            segment.UnknownStructuredBlock = DataSegmentHelper.Slice(dataSegment, UnknownStructOffset, UnknownStructSize);

            var structEnd = UnknownStructOffset + UnknownStructSize;
            segment.TrailingData = DataSegmentHelper.Slice(dataSegment, structEnd, dataSegment.Length - structEnd);

            return segment;
        }

        public byte[] ToBytes()
        {
            return DataSegmentHelper.Concatenate(
                PreClueRelPtrData,
                DataSegmentHelper.UInt16ArrayToBytes(ClueRelationshipPointers),
                MidSection,
                DataSegmentHelper.UInt16ArrayToBytes(CharacterNamePointers),
                PostCharNamePtrData,
                UnknownStructuredBlock,
                TrailingData
            );
        }

        public BugDataSegment Clone()
        {
            return new BugDataSegment
            {
                PreClueRelPtrData = PreClueRelPtrData.ToArray(),
                ClueRelationshipPointers = ClueRelationshipPointers.ToArray(),
                MidSection = MidSection.ToArray(),
                CharacterNamePointers = CharacterNamePointers.ToArray(),
                PostCharNamePtrData = PostCharNamePtrData.ToArray(),
                UnknownStructuredBlock = UnknownStructuredBlock.ToArray(),
                TrailingData = TrailingData.ToArray()
            };
        }
    }
}
