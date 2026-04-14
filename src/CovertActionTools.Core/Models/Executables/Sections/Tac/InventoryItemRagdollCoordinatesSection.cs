using System.Linq;

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

        public TacScreenCoordinate[] Coordinates { get; set; } = new TacScreenCoordinate[EntryCount];

        public bool Viewable() => true;
        public bool Editable() => true;

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            Coordinates = new TacScreenCoordinate[EntryCount];
            for (var i = 0; i < EntryCount; i++)
            {
                Coordinates[i] = TacScreenCoordinate.FromBytes(fullPayload, startingOffset + i * TacScreenCoordinate.RecordSize);
            }
            return EntryCount * TacScreenCoordinate.RecordSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[EntryCount * TacScreenCoordinate.RecordSize];
            for (var i = 0; i < EntryCount; i++)
            {
                var bytes = (Coordinates[i] ?? new TacScreenCoordinate()).ToBytes();
                System.Array.Copy(bytes, 0, result, i * TacScreenCoordinate.RecordSize, TacScreenCoordinate.RecordSize);
            }
            return result;
        }

        public InventoryItemRagdollCoordinatesSection Clone()
        {
            return new InventoryItemRagdollCoordinatesSection
            {
                Coordinates = Coordinates.Select(c => c?.Clone() ?? new TacScreenCoordinate()).ToArray()
            };
        }
    }
}
