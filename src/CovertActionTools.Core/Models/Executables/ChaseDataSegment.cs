using System;
using System.Linq;

namespace CovertActionTools.Core.Models.Executables
{
    public class ChaseDataSegment
    {
        /// <summary>DS paragraph value for CHASE.EXE.</summary>
        public const int DsParagraph = 0x0663;

        #region Layout Constants (DS-relative offsets)
        private const int NarrativeTextOffset = 0x0089;    // 0x006859 - 0x06630 = 0x229...
        // Actually: 0x6859 - 0x6630 = 0x229. Let me recalculate.
        // Wait — the offsets in the doc are payload-relative, not DS-relative.
        // DS×16 = 0x06630. Narrative at payload 0x006859 → DS-relative = 0x6859 - 0x6630 = 0x0229.
        // BSS at 0x006859 → but that's the narrative start. BSS at 0x007C19.
        // Chase gameplay strings at 0x007E48 → DS+0x1818.
        // Let me just use the narrative + BSS boundary.

        // Correction: narrative is at 0x0066B9, not 0x006859
        // 0x66B9 - 0x6630 = 0x0089
        private const int NarrativeSize = 417;             // 0x0066B9 to 0x006859 (approx)
        // 0x6859 - 0x66B9 = 0x1A0 = 416... close to 417. Let me use the doc value.

        private const int GameplayStringsOffset = 0x1818;  // 0x007E48 - 0x06630
        private const int GameplayStringsSize = 72;
        #endregion

        #region Fields (in binary order)

        /// <summary>Data before chase narrative: MSC runtime, file refs.</summary>
        public byte[] PreNarrativeData { get; set; } = Array.Empty<byte>();

        /// <summary>Chase narrative text: gender-conditional outcome strings (~417 bytes).</summary>
        public byte[] ChaseNarrativeText { get; set; } = Array.Empty<byte>();

        /// <summary>BSS, image descriptors, nibble data, palette remap, coordinate data.</summary>
        public byte[] MidSection { get; set; } = Array.Empty<byte>();

        /// <summary>Chase gameplay strings: cars.pic, speed display, quality ratings (~72 bytes).</summary>
        public byte[] ChaseGameplayStrings { get; set; } = Array.Empty<byte>();

        /// <summary>Everything after: quit dialog, infrastructure, overlay, C runtime, BSS.</summary>
        public byte[] TrailingData { get; set; } = Array.Empty<byte>();

        #endregion

        public static ChaseDataSegment FromBytes(byte[] dataSegment)
        {
            var segment = new ChaseDataSegment();

            segment.PreNarrativeData = DataSegmentHelper.Slice(dataSegment, 0, NarrativeTextOffset);

            segment.ChaseNarrativeText = DataSegmentHelper.Slice(dataSegment, NarrativeTextOffset, NarrativeSize);

            var narrativeEnd = NarrativeTextOffset + NarrativeSize;
            segment.MidSection = DataSegmentHelper.Slice(dataSegment, narrativeEnd, GameplayStringsOffset - narrativeEnd);

            segment.ChaseGameplayStrings = DataSegmentHelper.Slice(dataSegment, GameplayStringsOffset, GameplayStringsSize);

            var gameplayEnd = GameplayStringsOffset + GameplayStringsSize;
            segment.TrailingData = DataSegmentHelper.Slice(dataSegment, gameplayEnd, dataSegment.Length - gameplayEnd);

            return segment;
        }

        public byte[] ToBytes()
        {
            return DataSegmentHelper.Concatenate(
                PreNarrativeData,
                ChaseNarrativeText,
                MidSection,
                ChaseGameplayStrings,
                TrailingData
            );
        }

        public ChaseDataSegment Clone()
        {
            return new ChaseDataSegment
            {
                PreNarrativeData = PreNarrativeData.ToArray(),
                ChaseNarrativeText = ChaseNarrativeText.ToArray(),
                MidSection = MidSection.ToArray(),
                ChaseGameplayStrings = ChaseGameplayStrings.ToArray(),
                TrailingData = TrailingData.ToArray()
            };
        }
    }
}
