using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// List of filenames referenced by the Code Segment, contains the files to load initially
    /// </summary>
    public class TacHeaderFilenamesSection : ExactCountStringTableSection
    {
        protected override int StringCount => 7;
        protected override int? StringLength => null;
        
        public override bool Viewable()
        {
            return true;
        }

        public override bool Editable()
        {
            return false;
        }

        public TacHeaderFilenamesSection Clone()
        {
            return new TacHeaderFilenamesSection()
            {
                Strings = Strings.ToList()
            };
        }
    }
}