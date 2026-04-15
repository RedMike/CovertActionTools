using System.Linq;
using CovertActionTools.Core.Models.Executables.Records.Tac;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Equipment selection highlight rectangles at DS:0x220C: 12 screen-space
    /// TL/BR rectangles indexed by inventory slot. Slot 0 (Pistol) is a zeroed
    /// sentinel matching the unselectable row in the navigation table.
    /// </summary>
    public class InventoryItemSelectionRectanglesSection : IExecutableSection
    {
        public const int EntryCount = 12;

        public TacScreenRectRecord[] Rectangles { get; set; } = new TacScreenRectRecord[EntryCount];

        public bool Viewable() => true;
        public bool Editable() => true;

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            Rectangles = new TacScreenRectRecord[EntryCount];
            for (var i = 0; i < EntryCount; i++)
            {
                var record = new TacScreenRectRecord();
                record.ReadBytes(fullPayload, startingOffset + i * TacScreenRectRecord.RecordSize);
                Rectangles[i] = record;
            }
            return EntryCount * TacScreenRectRecord.RecordSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[EntryCount * TacScreenRectRecord.RecordSize];
            for (var i = 0; i < EntryCount; i++)
            {
                var bytes = (Rectangles[i] ?? new TacScreenRectRecord()).WriteBytes();
                System.Array.Copy(bytes, 0, result, i * TacScreenRectRecord.RecordSize, TacScreenRectRecord.RecordSize);
            }
            return result;
        }

        public InventoryItemSelectionRectanglesSection Clone()
        {
            return new InventoryItemSelectionRectanglesSection
            {
                Rectangles = Rectangles.Select(r => r?.Clone() ?? new TacScreenRectRecord()).ToArray()
            };
        }
    }
}
