using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// First-name pool drawn from when the game generates a female suspect.
    /// Stored as 64 variable-length slots packed contiguously; the trailing
    /// character-name pointer table at DS:0x346C indexes into this block. Slot
    /// sizes are frozen to the original ROM widths so the pointer table stays
    /// correct without having to be recomputed.
    /// </summary>
    public class FemaleFirstNamesSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[]
        {
            9, 7, 6, 8, 6, 7, 8, 6, 8, 9, 5, 9, 9, 9, 5, 6,
            6, 4, 6, 7, 6, 7, 6, 5, 8, 7, 5, 6, 4, 5, 8, 5,
            6, 7, 7, 6, 7, 6, 7, 6, 7, 8, 6, 7, 8, 7, 9, 5,
            9, 7, 8, 7, 6, 6, 8, 7, 7, 6, 6, 6, 7, 6, 8, 8,
        };

        public FemaleFirstNamesSection Clone()
        {
            return new FemaleFirstNamesSection { Strings = Strings.ToList() };
        }
    }
}
