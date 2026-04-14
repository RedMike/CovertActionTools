using System;

namespace CovertActionTools.Core.Models.Executables.Records.Tac
{
    public class FloorSafeInventoryItemRewardRecord
    {
        public const int RecordSize = 2;

        public ushort InventoryItemIndex { get; set; }

        public static FloorSafeInventoryItemRewardRecord FromBytes(byte[] data, int offset)
        {
            return new FloorSafeInventoryItemRewardRecord
            {
                InventoryItemIndex = BitConverter.ToUInt16(data, offset)
            };
        }

        public byte[] WriteBytes()
        {
            return new[]
            {
                (byte)(InventoryItemIndex & 0xFF),
                (byte)((InventoryItemIndex >> 8) & 0xFF)
            };
        }

        public FloorSafeInventoryItemRewardRecord Clone()
        {
            return new FloorSafeInventoryItemRewardRecord
            {
                InventoryItemIndex = InventoryItemIndex
            };
        }
    }
}
