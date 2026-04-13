using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class GameplayPopupStringsSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[] { 18, 22, 9, 3, 10, 3, 1, 52, 49, 23 };

        public GameplayPopupStringsSection Clone()
        {
            return new GameplayPopupStringsSection { Strings = Strings.ToList() };
        }
    }
}
