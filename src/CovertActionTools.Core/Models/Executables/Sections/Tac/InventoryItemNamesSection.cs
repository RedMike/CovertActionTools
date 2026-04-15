using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// 16 inventory item name slots plus the "equip2.pic" filename used by the equipment screen,
    /// followed by the 16-entry equipment pointer table at DS:0x20E0. Field sizes are fixed so
    /// the pointer values stay stable regardless of string edits; the pointer table is
    /// regenerated from the slot layout on write and discarded on read.
    /// </summary>
    public class InventoryItemNamesSection : ExactCountFixedSizeStringTableSection
    {
        /// <summary>DS-relative base offset of this section (first inventory item name slot).</summary>
        public const int BaseOffset = 0x2034;

        /// <summary>Number of slots covered by the equipment pointer table. The trailing
        /// "equip2.pic" slot is not referenced by the pointer table.</summary>
        public const int PointeredSlotCount = 16;

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
            11, // "equip2.pic" -- equipment screen filename (not in pointer table)
        };

        private const int PointerTableSizeBytes = PointeredSlotCount * 2;

        public override int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var stringsRead = base.ReadBytes(fullPayload, startingOffset);
            return stringsRead + PointerTableSizeBytes;
        }

        public override byte[] WriteBytes()
        {
            var stringsBytes = base.WriteBytes();
            var pointers = ComputePointers(BaseOffset);
            var result = new byte[stringsBytes.Length + PointerTableSizeBytes];
            System.Array.Copy(stringsBytes, 0, result, 0, stringsBytes.Length);
            for (var i = 0; i < PointeredSlotCount; i++)
            {
                var pos = stringsBytes.Length + i * 2;
                result[pos] = (byte)(pointers[i] & 0xFF);
                result[pos + 1] = (byte)((pointers[i] >> 8) & 0xFF);
            }
            return result;
        }

        public InventoryItemNamesSection Clone()
        {
            return new InventoryItemNamesSection { Strings = Strings.ToList() };
        }
    }
}
