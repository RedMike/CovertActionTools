using System;

namespace CovertActionTools.Core.Models.Executables.Records.Tac
{
    public class FloorSafeInventoryItemRewardRecord : IExecutableRecord
    {
        public const int RecordSize = 2;

        public ushort InventoryItemIndex { get; set; }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            InventoryItemIndex = BitConverter.ToUInt16(fullPayload, startingOffset);
            return RecordSize;
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
