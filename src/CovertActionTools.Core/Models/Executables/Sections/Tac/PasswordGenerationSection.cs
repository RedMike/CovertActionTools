using System;
using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class PasswordGenerationSection : IExecutableSection
    {
        public const int StringCount = 5;

        public string ReadFileMode { get; set; } = "";
        public string Filename { get; set; } = "";
        public string FileReadFormat1 { get; set; } = "";
        public string FileReadFormat2 { get; set; } = "";
        public string PasswordTemplateString { get; set; } = "";

        public int[] OriginalByteSizes { get; set; } = Array.Empty<int>();

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
            var (strings, sizes) = DataSegmentHelper.NullTerminatedStringsWithSizesFromBytes(
                fullPayload, startingOffset, StringCount);

            ReadFileMode = strings[0];
            Filename = strings[1];
            FileReadFormat1 = strings[2];
            FileReadFormat2 = strings[3];
            PasswordTemplateString = strings[4];
            OriginalByteSizes = sizes;

            var totalSize = 0;
            foreach (var s in sizes) totalSize += s;
            return totalSize;
        }

        public byte[] WriteBytes()
        {
            var strings = new[] { ReadFileMode, Filename, FileReadFormat1, FileReadFormat2, PasswordTemplateString };
            return DataSegmentHelper.NullTerminatedStringsToFixedBytes(strings, OriginalByteSizes);
        }

        public PasswordGenerationSection Clone()
        {
            return new PasswordGenerationSection
            {
                ReadFileMode = ReadFileMode,
                Filename = Filename,
                FileReadFormat1 = FileReadFormat1,
                FileReadFormat2 = FileReadFormat2,
                PasswordTemplateString = PasswordTemplateString,
                OriginalByteSizes = OriginalByteSizes.ToArray()
            };
        }
    }
}
