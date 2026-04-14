using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class GameplayEndingStringsSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[] { 39, 51, 57, 42, 39 };

        public GameplayEndingStringsSection Clone()
        {
            return new GameplayEndingStringsSection { Strings = Strings.ToList() };
        }
    }
}
