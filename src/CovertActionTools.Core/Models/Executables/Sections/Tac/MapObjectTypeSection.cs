using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Models.Executables.Records.Tac;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class MapObjectTypeSection : ExactCountRecordTableSection<MapObjectTypeRecord>
    {
        protected override int RecordCount => 62;
        
        public IReadOnlyList<MapObjectTypeRecord> MapObjectTypes => Records;
        
        public override bool Viewable()
        {
            return true;
        }

        public override bool Editable()
        {
            return true;
        }

        public MapObjectTypeSection Clone()
        {
            return new MapObjectTypeSection()
            {
                Records = Records.Select(r => r.Clone()).ToList()
            };
        }
    }
}