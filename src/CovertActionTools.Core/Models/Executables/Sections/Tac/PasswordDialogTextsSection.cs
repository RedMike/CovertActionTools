using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class PasswordDialogTextsSection : ExactCountFixedSizeStringTableSection, IPaddedToWord
    {
        protected override int[] StringSizes => new[]
        {
            16, 1, 17, 14, 12, 6, 2, 14, 5, 2, 19, 5, 2, 9, 3, 1, 8
        };

        public PasswordDialogTextsSection Clone()
        {
            return new PasswordDialogTextsSection { Strings = Strings.ToList() };
        }
    }
}
