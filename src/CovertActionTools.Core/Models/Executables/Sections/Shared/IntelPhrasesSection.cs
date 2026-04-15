using System.Linq;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    /// <summary>
    /// Small connective phrase fragments used when composing intel report sentences
    /// (" ", "\n... ", "on ", " ", "of the ", "in ", "\n", "", " ", " in "). Each slot
    /// is a fixed null-terminated size from the original binary.
    /// </summary>
    public class IntelPhrasesSection : ExactCountFixedSizeStringTableSection
    {
        public const int PhraseCount = 10;

        protected override int[] StringSizes => new[]
        {
            2, // " "
            6, // "\n... "
            4, // "on "
            2, // " "
            8, // "of the "
            4, // "in "
            2, // "\n"
            1, // ""
            2, // " "
            5, // " in "
        };

        public IntelPhrasesSection Clone()
        {
            return new IntelPhrasesSection { Strings = Strings.ToList() };
        }
    }
}
