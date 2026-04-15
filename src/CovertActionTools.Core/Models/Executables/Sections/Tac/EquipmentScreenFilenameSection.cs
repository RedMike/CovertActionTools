using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Single 11-byte filename slot ("equip2.pic") immediately after the inventory item names.
    /// Loaded by the equipment screen; kept hidden from the editor to avoid accidental renames
    /// that would break the overlay loader lookup.
    /// </summary>
    public class EquipmentScreenFilenameSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[] { 11 };

        public override bool Viewable()
        {
            return false;
        }

        public override bool Editable()
        {
            return false;
        }

        public EquipmentScreenFilenameSection Clone()
        {
            return new EquipmentScreenFilenameSection { Strings = Strings.ToList() };
        }
    }
}
