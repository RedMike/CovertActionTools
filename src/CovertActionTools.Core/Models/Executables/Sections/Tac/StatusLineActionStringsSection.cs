using System;
using System.Linq;
using CovertActionTools.Core.Models.Executables;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class StatusLineActionStringsSection : IExecutableSection
    {
        public const int SectionSize = 0xA3;

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

        public StatusLineActionStringsSection Clone()
        {
            return new StatusLineActionStringsSection
            {
                Strings = Strings.ToArray(),
                StringSizes = StringSizes.ToArray()
            };
        }
    }
}
