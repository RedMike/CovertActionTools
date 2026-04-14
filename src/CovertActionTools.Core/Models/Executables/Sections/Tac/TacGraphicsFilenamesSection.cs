using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class TacGraphicsFilenamesSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[] { 7, 13, 12 };

        public override bool Viewable()
        {
            return false;
        }

        public override bool Editable()
        {
            return false;
        }

        public TacGraphicsFilenamesSection Clone()
        {
            return new TacGraphicsFilenamesSection { Strings = Strings.ToList() };
        }
    }
}
