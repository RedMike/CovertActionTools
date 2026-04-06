using System;
using System.Linq;

namespace CovertActionTools.Core.Models.Executables
{
    /// <summary>
    /// Structured data segment for CHASE.EXE.
    /// Field boundaries and interpretations are based on reverse engineering and may not
    /// be fully accurate. Unknown regions are preserved as raw byte arrays.
    /// </summary>
    public class ChaseDataSegment
    {
        /// <summary>DS paragraph value for CHASE.EXE.</summary>
        public const int DsParagraph = 0x0663;

        #region Layout Constants (DS-relative offsets)
        private const int NarrativeTextOffset = 0x0089;    // 0x0066B9 - 0x06630
        private const int NarrativeSize = 417;
        private const int GameplayStringsOffset = 0x1818;  // 0x007E48 - 0x06630
        private const int GameplayStringsSize = 72;
        #endregion

        #region Fields (in binary order)

        /// <summary>Data before chase narrative: MSC runtime, file refs.</summary>
        public byte[] PreNarrativeData { get; set; } = Array.Empty<byte>();

        /// <summary>Chase narrative text: gender-conditional outcome strings.</summary>
        public string[] ChaseNarrativeStrings { get; set; } = Array.Empty<string>();

        /// <summary>Original per-string byte sizes for narrative strings (prevents pointer drift).</summary>
        public int[] ChaseNarrativeByteSizes { get; set; } = Array.Empty<int>();

        /// <summary>BSS, image descriptors, nibble data, palette remap, coordinate data.</summary>
        public byte[] MidSection { get; set; } = Array.Empty<byte>();

        /// <summary>Chase gameplay strings: cars.pic, speed display, quality ratings.</summary>
        public string[] ChaseGameplayStrings { get; set; } = Array.Empty<string>();

        /// <summary>Original per-string byte sizes for gameplay strings (prevents pointer drift).</summary>
        public int[] ChaseGameplayByteSizes { get; set; } = Array.Empty<int>();

        /// <summary>Everything after: quit dialog, infrastructure, overlay, C runtime, BSS.</summary>
        public byte[] TrailingData { get; set; } = Array.Empty<byte>();

        #endregion

        public static ChaseDataSegment FromBytes(byte[] dataSegment)
        {
            var segment = new ChaseDataSegment();

            segment.PreNarrativeData = DataSegmentHelper.Slice(dataSegment, 0, NarrativeTextOffset);

            var (narrativeStrings, narrativeSizes) = DataSegmentHelper.ControlStringsFromBytes(dataSegment, NarrativeTextOffset, NarrativeSize);
            segment.ChaseNarrativeStrings = narrativeStrings;
            segment.ChaseNarrativeByteSizes = narrativeSizes;

            var narrativeEnd = NarrativeTextOffset + NarrativeSize;
            segment.MidSection = DataSegmentHelper.Slice(dataSegment, narrativeEnd, GameplayStringsOffset - narrativeEnd);

            var (gameplayStrings, gameplaySizes) = DataSegmentHelper.ControlStringsFromBytes(dataSegment, GameplayStringsOffset, GameplayStringsSize);
            segment.ChaseGameplayStrings = gameplayStrings;
            segment.ChaseGameplayByteSizes = gameplaySizes;

            var gameplayEnd = GameplayStringsOffset + GameplayStringsSize;
            segment.TrailingData = DataSegmentHelper.Slice(dataSegment, gameplayEnd, dataSegment.Length - gameplayEnd);

            return segment;
        }

        public byte[] ToBytes()
        {
            return DataSegmentHelper.Concatenate(
                PreNarrativeData,
                DataSegmentHelper.PadToSize(
                    DataSegmentHelper.ControlStringsToFixedBytes(ChaseNarrativeStrings, ChaseNarrativeByteSizes),
                    NarrativeSize),
                MidSection,
                DataSegmentHelper.PadToSize(
                    DataSegmentHelper.ControlStringsToFixedBytes(ChaseGameplayStrings, ChaseGameplayByteSizes),
                    GameplayStringsSize),
                TrailingData
            );
        }

        public ChaseDataSegment Clone()
        {
            return new ChaseDataSegment
            {
                PreNarrativeData = PreNarrativeData.ToArray(),
                ChaseNarrativeStrings = ChaseNarrativeStrings.Select(s => s).ToArray(),
                ChaseNarrativeByteSizes = ChaseNarrativeByteSizes.ToArray(),
                MidSection = MidSection.ToArray(),
                ChaseGameplayStrings = ChaseGameplayStrings.Select(s => s).ToArray(),
                ChaseGameplayByteSizes = ChaseGameplayByteSizes.ToArray(),
                TrailingData = TrailingData.ToArray()
            };
        }
    }
}
