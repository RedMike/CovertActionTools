using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class PasswordGenerationSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[] { 3, 9, 5, 5, 11 };

        public override bool Viewable()
        {
            return false;
        }

        public override bool Editable()
        {
            return false;
        }

        public PasswordGenerationSection Clone()
        {
            return new PasswordGenerationSection { Strings = Strings.ToList() };
        }
    }
}
