using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class StatusLineStatusStringsSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[] { 4, 9 };

        public StatusLineStatusStringsSection Clone()
        {
            return new StatusLineStatusStringsSection { Strings = Strings.ToList() };
        }
    }
}
