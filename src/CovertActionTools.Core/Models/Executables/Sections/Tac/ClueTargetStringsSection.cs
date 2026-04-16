using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Five fixed-size strings that the clue formatter uses when describing the
    /// subject of a clue: the "this face." phrase for photograph clues, two hard-
    /// coded agent names (Valkerie, Thunderbolt) referenced by some clue templates,
    /// and two placeholder number fragments ("00", "000") used to pad generated
    /// evidence IDs.
    /// </summary>
    public class ClueTargetStringsSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[]
        {
            11, // "this face."
            9,  // "Valkerie"
            12, // "Thunderbolt"
            3,  // "00"
            4,  // "000"
        };

        public ClueTargetStringsSection Clone()
        {
            return new ClueTargetStringsSection { Strings = Strings.ToList() };
        }
    }
}
