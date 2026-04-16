using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// First-name pool drawn from when the game generates a male suspect.
    /// Stored as 64 variable-length slots packed contiguously; the trailing
    /// character-name pointer table at DS:0x346C indexes into this block. Slot
    /// sizes are frozen to the original ROM widths so the pointer table stays
    /// correct without having to be recomputed.
    /// </summary>
    public class MaleFirstNamesSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[]
        {
            7, 7, 5, 7, 7, 7, 7, 5, 6, 8, 8, 6, 7, 7, 9, 8,
            5, 4, 5, 4, 4, 5, 5, 6, 8, 9, 5, 7, 6, 7, 7, 7,
            5, 4, 6, 6, 5, 4, 6, 6, 7, 8, 6, 7, 8, 8, 9, 6,
            7, 5, 7, 6, 5, 6, 5, 6, 8, 8, 7, 6, 7, 7, 7, 6,
        };

        public MaleFirstNamesSection Clone()
        {
            return new MaleFirstNamesSection { Strings = Strings.ToList() };
        }
    }
}
