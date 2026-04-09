using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Models.Executables.Records.Tac;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class RoomTypeSection : ExactCountRecordTableSection<RoomTypeRecord>
    {
        public IReadOnlyList<RoomTypeRecord> RoomTypes => Records;
        
        protected override int RecordCount => 10;
        
        public override bool Viewable()
        {
            return true;
        }

        public override bool Editable()
        {
            return true;
        }

        public RoomTypeSection Clone()
        {
            return new RoomTypeSection()
            {
                Records = Records.Select(r => r.Clone()).ToList()
            };
        }
    }
}