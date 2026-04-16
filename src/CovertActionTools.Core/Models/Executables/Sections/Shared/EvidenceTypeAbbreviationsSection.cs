using System.Linq;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    /// <summary>
    /// Eight three-letter evidence type abbreviations ("CAR", "WPN", "ADR", "TKT",
    /// "MSG", "$", "$", "FCE") used as column headers in evidence listings. The two
    /// "$" slots are intentional duplicates from the original data.
    /// </summary>
    public class EvidenceTypeAbbreviationsSection : ExactCountFixedSizeStringTableSection
    {
        public const int TypeCount = 8;

        protected override int[] StringSizes => new[]
        {
            4, // "CAR"
            4, // "WPN"
            4, // "ADR"
            4, // "TKT"
            4, // "MSG"
            2, // "$"
            2, // "$"
            4, // "FCE"
        };

        public EvidenceTypeAbbreviationsSection Clone()
        {
            return new EvidenceTypeAbbreviationsSection { Strings = Strings.ToList() };
        }
    }
}
