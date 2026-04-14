using System;
using System.Linq;
using CovertActionTools.Core.Models.Executables.Records.Tac;
using CovertActionTools.Core.Models.Executables.Sections;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class FloorSafeInventoryItemRewardSection : IExecutableSection
    {
        public const int RecordCount = 8;
        public const int SectionSize = RecordCount * FloorSafeInventoryItemRewardRecord.RecordSize;

        public FloorSafeInventoryItemRewardRecord[] Records { get; set; } = Array.Empty<FloorSafeInventoryItemRewardRecord>();

        public bool Viewable()
        {
            return true;
        }

        public bool Editable()
        {
            return true;
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            Records = new FloorSafeInventoryItemRewardRecord[RecordCount];
            for (var i = 0; i < RecordCount; i++)
            {
                Records[i] = FloorSafeInventoryItemRewardRecord.FromBytes(
                    fullPayload, startingOffset + i * FloorSafeInventoryItemRewardRecord.RecordSize);
            }
            return SectionSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[SectionSize];
            for (var i = 0; i < Records.Length; i++)
            {
                Array.Copy(Records[i].WriteBytes(), 0, result,
                    i * FloorSafeInventoryItemRewardRecord.RecordSize,
                    FloorSafeInventoryItemRewardRecord.RecordSize);
            }
            return result;
        }

        public FloorSafeInventoryItemRewardSection Clone()
        {
            return new FloorSafeInventoryItemRewardSection
            {
                Records = Records.Select(r => r.Clone()).ToArray()
            };
        }
    }
}
