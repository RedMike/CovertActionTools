using System.Linq;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    /// <summary>
    /// Four intel report header strings ("       CODED MESSAGE:", "       MEETING NOTES:",
    /// "       NEW INFORMATION", "    ADDITIONAL INFORMATION") used as titles when intel
    /// reports are rendered. Each slot is a fixed null-terminated size; the leading spaces
    /// and trailing line-feeds are part of the original data.
    /// </summary>
    public class IntelHeadersSection : ExactCountFixedSizeStringTableSection
    {
        public const int HeaderCount = 4;

        protected override int[] StringSizes => new[]
        {
            23, // "       CODED MESSAGE:\n"
            23, // "       MEETING NOTES:\n"
            24, // "       NEW INFORMATION\n"
            28, // "    ADDITIONAL INFORMATION\n"
        };

        public IntelHeadersSection Clone()
        {
            return new IntelHeadersSection { Strings = Strings.ToList() };
        }
    }
}
