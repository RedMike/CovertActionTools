using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Five building-type suffix strings appended after an organisation name when
    /// the game labels a mission location ("&lt;org&gt; hideout", "&lt;org&gt; agent", etc.).
    /// Each starts with a leading space that acts as the separator. " active cel"
    /// is the spelling actually used in-game -- that truncation is the original
    /// label, not a storage artefact.
    /// </summary>
    public class BuildingNamesSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[]
        {
            9,  // " hideout"
            7,  // " agent"
            11, // " safehouse"
            12, // " active cel"
            8,  // " office"
        };

        public BuildingNamesSection Clone()
        {
            return new BuildingNamesSection { Strings = Strings.ToList() };
        }
    }
}
