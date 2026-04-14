using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class StatusLineActionStringsSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[] { 9, 11, 9, 23, 22, 15, 6, 8, 13, 1, 9, 21, 16 };

        public StatusLineActionStringsSection Clone()
        {
            return new StatusLineActionStringsSection { Strings = Strings.ToList() };
        }
    }
}
