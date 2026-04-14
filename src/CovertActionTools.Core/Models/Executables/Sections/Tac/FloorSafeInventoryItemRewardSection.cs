using System.Linq;
using CovertActionTools.Core.Models.Executables.Records.Tac;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class FloorSafeInventoryItemRewardSection : ExactCountRecordTableSection<FloorSafeInventoryItemRewardRecord>
    {
        protected override int RecordCount => 8;

        public override bool Viewable()
        {
            return true;
        }

        public override bool Editable()
        {
            return true;
        }

        public FloorSafeInventoryItemRewardSection Clone()
        {
            return new FloorSafeInventoryItemRewardSection
            {
                Records = Records.Select(r => r.Clone()).ToList()
            };
        }
    }
}
