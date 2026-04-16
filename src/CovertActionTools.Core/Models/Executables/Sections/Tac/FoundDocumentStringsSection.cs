using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Nine fixed-size strings rendered when the player recovers a document from
    /// a safe or desk: the document title ("Master Plan", "Personnel File"), the
    /// "--SECRET--" banner stamped on classified documents, the suspect-status
    /// tags ("ARRESTED", "IN HIDING", " TURNED"), an "Action Team" label, a ", "
    /// separator used when concatenating names, and the "(No activity)" fallback
    /// for an empty activity log.
    /// </summary>
    public class FoundDocumentStringsSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[]
        {
            12, // "Master Plan"
            11, // "--SECRET--"
            9,  // "ARRESTED"
            10, // "IN HIDING"
            8,  // " TURNED"
            15, // "Personnel File"
            13, // " Action Team"
            3,  // ", "
            14, // "(No activity)"
        };

        public FoundDocumentStringsSection Clone()
        {
            return new FoundDocumentStringsSection { Strings = Strings.ToList() };
        }
    }
}
