using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Surname pool drawn from when the game generates a suspect of either
    /// gender. Stored as 64 variable-length slots packed contiguously; the
    /// trailing character-name pointer table at DS:0x346C indexes into this
    /// block. Slot sizes are frozen to the original ROM widths so the pointer
    /// table stays correct without having to be recomputed.
    /// </summary>
    public class LastNamesSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[]
        {
            7,  6, 8, 9, 8,  7, 7, 8, 8, 8, 6, 7, 7, 8, 10, 9,
            7,  5, 7, 6, 6,  6, 7, 7, 5, 7, 7, 6, 8, 8,  6, 7,
            6,  5, 6, 7, 6,  7, 7, 7, 5, 6, 6, 7, 8, 8,  5, 7,
            6,  5, 6, 6, 8,  8, 7, 6, 7, 6, 8, 7, 9, 9,  8, 9,
        };

        public LastNamesSection Clone()
        {
            return new LastNamesSection { Strings = Strings.ToList() };
        }
    }
}
