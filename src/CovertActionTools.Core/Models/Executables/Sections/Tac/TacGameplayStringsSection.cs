using System;
using System.Linq;
using CovertActionTools.Core.Models.Executables;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Gameplay / dialogue strings used by the TAC mission runtime. Covers action-feedback
    /// fragments, environmental prompts, terminal interaction messages, and mission-end
    /// dialogue. The anchor scan finds roughly 30 unique DS-relative pointer targets in
    /// this range (one per string), all loaded from TAC's own code segment.
    ///
    /// The section is stored as a fixed-size byte block (677 bytes, 0x2A5) parsed as
    /// null-terminated control-byte-aware strings, with per-slot original byte sizes
    /// preserved so variable-length edits still roundtrip in the original slots (the
    /// CodeSegment-references-DS-by-immediate problem documented on
    /// <see cref="DataSegmentHelper"/>).
    /// </summary>
    public class TacGameplayStringsSection : IExecutableSection
    {
        // 0x1C91..0x1F35 inclusive — ends at the null terminator that follows the final
        // "into a double agent!\n" message in the binary.
        public const int SectionSize = 0x2A5;

        public string[] Strings { get; set; } = Array.Empty<string>();
        public int[] StringSizes { get; set; } = Array.Empty<int>();

        public bool Viewable()
        {
            return true;
        }

        public bool Editable()
        {
            return false;
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var (strings, sizes) = DataSegmentHelper.ControlStringsFromBytes(
                fullPayload, startingOffset, SectionSize);
            Strings = strings;
            StringSizes = sizes;
            return SectionSize;
        }

        public byte[] WriteBytes()
        {
            var encoded = DataSegmentHelper.ControlStringsToFixedBytes(Strings, StringSizes);
            if (encoded.Length == SectionSize)
            {
                return encoded;
            }

            var result = new byte[SectionSize];
            Array.Copy(encoded, result, Math.Min(encoded.Length, SectionSize));
            return result;
        }

        public TacGameplayStringsSection Clone()
        {
            return new TacGameplayStringsSection
            {
                Strings = Strings.ToArray(),
                StringSizes = StringSizes.ToArray()
            };
        }
    }
}
