using System.Linq;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    /// <summary>
    /// 12 three-letter month abbreviations ("Jan", "Feb", ..., "Dec"), each stored in a
    /// fixed 4-byte null-terminated slot. Appears identically in multiple game EXEs.
    /// </summary>
    public class MonthAbbreviationsSection : ExactCountFixedSizeStringTableSection
    {
        public const int MonthCount = 12;
        public const int SlotSize = 4;

        protected override int[] StringSizes => Enumerable.Repeat(SlotSize, MonthCount).ToArray();

        public MonthAbbreviationsSection Clone()
        {
            return new MonthAbbreviationsSection { Strings = Strings.ToList() };
        }
    }
}
