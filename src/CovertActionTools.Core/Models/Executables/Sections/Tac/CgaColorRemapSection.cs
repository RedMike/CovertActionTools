using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Models.Executables.Records.Tac;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// CGA color remap table region (14 × 16-byte records). Five records are directly
    /// referenced by main-code pointer loads and named via their DS offset: records 2
    /// and 3 (menu highlight / cursor) at DS 0x1B08/0x1B18, records 10/11/12 (entity
    /// card / dialog box / mission) at DS 0x1B88/0x1B98/0x1BA8. The remaining records
    /// are either scratch/working memory used by stub 27 or additional records accessed
    /// via relative offsets from the code-loaded anchors.
    /// </summary>
    public class CgaColorRemapSection : ExactCountRecordTableSection<CgaColorRemapRecord>
    {
        protected override int RecordCount => 14;

        public IReadOnlyList<CgaColorRemapRecord> Records16 => Records;

        public override bool Viewable()
        {
            return true;
        }

        public override bool Editable()
        {
            return true;
        }

        public CgaColorRemapSection Clone()
        {
            return new CgaColorRemapSection
            {
                Records = Records.Select(r => r.Clone()).ToList()
            };
        }
    }
}
