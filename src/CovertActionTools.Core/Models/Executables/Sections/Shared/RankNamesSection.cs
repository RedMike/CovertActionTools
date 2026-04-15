using System.Linq;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    /// <summary>
    /// Eight agent rank names ("Recruit", "Operative", ... "MasterMind") shown on
    /// agent status screens. Each slot is a fixed null-terminated size; the layout
    /// is reproduced byte-for-byte across TAC, FINAL, and GAME.
    /// </summary>
    public class RankNamesSection : ExactCountFixedSizeStringTableSection
    {
        public const int RankCount = 8;

        protected override int[] StringSizes => new[]
        {
            8,  // "Recruit"
            10, // "Operative"
            11, // "Technician"
            6,  // "Agent"
            10, // "Organizer"
            14, // "Special Agent"
            13, // "Group Leader"
            11, // "MasterMind"
        };

        public RankNamesSection Clone()
        {
            return new RankNamesSection { Strings = Strings.ToList() };
        }
    }
}
