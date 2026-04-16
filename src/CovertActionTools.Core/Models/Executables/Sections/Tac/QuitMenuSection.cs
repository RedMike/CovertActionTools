using CovertActionTools.Core.Models.Executables.Records.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Quit confirmation dialog stored as a 41-byte menu string with a two-line
    /// prompt ("Are you sure\nyou want to Quit?") and two options (" No", " Yes").
    /// This is a second copy of the same dialog that lives inside
    /// <see cref="TacMenuStringsSection"/> at an earlier DS offset; both are read
    /// at different points by the tactical UI code and kept in sync by the
    /// original build.
    /// </summary>
    public class QuitMenuSection : IExecutableSection
    {
        public const int SlotSize = 41;
        public const int OptionCount = 2;

        public MenuStringRecord Menu { get; set; } = new MenuStringRecord(SlotSize, OptionCount);

        public bool Viewable() => true;
        public bool Editable() => true;

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            return Menu.ReadBytes(fullPayload, startingOffset);
        }

        public byte[] WriteBytes()
        {
            return Menu.WriteBytes();
        }

        public QuitMenuSection Clone()
        {
            return new QuitMenuSection { Menu = Menu.Clone() };
        }
    }
}
