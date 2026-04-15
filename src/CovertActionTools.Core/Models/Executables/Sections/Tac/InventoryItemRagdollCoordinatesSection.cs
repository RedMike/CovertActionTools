using System.Linq;
using CovertActionTools.Core.Models.Executables.Records.Tac;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Ragdoll sprite placement coordinates at DS:0x2160: 43 screen-space points used
    /// to anchor equipment icons (bullets, magazine, selected item silhouette, etc.)
    /// when the inventory screen is drawn. Entry count is fixed by the surrounding
    /// data layout (next field is the selection rectangles table at 0x220C).
    /// </summary>
    public class InventoryItemRagdollCoordinatesSection : IExecutableSection
    {
        public const int EntryCount = 43;

        public TacScreenCoordinateRecord[] Coordinates { get; set; } = new TacScreenCoordinateRecord[EntryCount];

        public bool Viewable() => true;
        public bool Editable() => true;

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            Coordinates = new TacScreenCoordinateRecord[EntryCount];
            for (var i = 0; i < EntryCount; i++)
            {
                var record = new TacScreenCoordinateRecord();
                record.ReadBytes(fullPayload, startingOffset + i * TacScreenCoordinateRecord.RecordSize);
                Coordinates[i] = record;
            }
            return EntryCount * TacScreenCoordinateRecord.RecordSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[EntryCount * TacScreenCoordinateRecord.RecordSize];
            for (var i = 0; i < EntryCount; i++)
            {
                var bytes = (Coordinates[i] ?? new TacScreenCoordinateRecord()).WriteBytes();
                System.Array.Copy(bytes, 0, result, i * TacScreenCoordinateRecord.RecordSize, TacScreenCoordinateRecord.RecordSize);
            }
            return result;
        }

        public InventoryItemRagdollCoordinatesSection Clone()
        {
            return new InventoryItemRagdollCoordinatesSection
            {
                Coordinates = Coordinates.Select(c => c?.Clone() ?? new TacScreenCoordinateRecord()).ToArray()
            };
        }
    }
}
