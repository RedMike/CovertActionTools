using System.Linq;

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

        public TacScreenRect[] Rectangles { get; set; } = new TacScreenRect[EntryCount];

        public bool Viewable() => true;
        public bool Editable() => true;

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            Rectangles = new TacScreenRect[EntryCount];
            for (var i = 0; i < EntryCount; i++)
            {
                Rectangles[i] = TacScreenRect.FromBytes(fullPayload, startingOffset + i * TacScreenRect.RecordSize);
            }
            return EntryCount * TacScreenRect.RecordSize;
        }

        public byte[] WriteBytes()
        {
            var result = new byte[EntryCount * TacScreenRect.RecordSize];
            for (var i = 0; i < EntryCount; i++)
            {
                var bytes = (Rectangles[i] ?? new TacScreenRect()).ToBytes();
                System.Array.Copy(bytes, 0, result, i * TacScreenRect.RecordSize, TacScreenRect.RecordSize);
            }
            return result;
        }

        public InventoryItemSelectionRectanglesSection Clone()
        {
            return new InventoryItemSelectionRectanglesSection
            {
                Rectangles = Rectangles.Select(r => r?.Clone() ?? new TacScreenRect()).ToArray()
            };
        }
    }
}
