using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// 16 inventory item name slots referenced by the equipment pointer table at DS:0x20E0.
    /// Slots 12-14 are unused (single null bytes); slot 15 holds "Floor Plan". Field sizes
    /// are fixed so the pointer table values stay stable regardless of string edits.
    /// </summary>
    public class InventoryItemNamesSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[]
        {
            7,  // Pistol
            4,  // Uzi
            7,  // Camera
            5,  // Bugs
            19, // Safecracking Tools
            9,  // Gas Mask
            12, // Kevlar Vest
            16, // Motion Detector
            23, // Fragmentation Grenades
            13, // Gas Grenades
            14, // Stun Grenades
            18, // Assorted Grenades
            1,  // (unused)
            1,  // (unused)
            1,  // (unused)
            11, // Floor Plan
        };

        public InventoryItemNamesSection Clone()
        {
            return new InventoryItemNamesSection { Strings = Strings.ToList() };
        }
    }
}
