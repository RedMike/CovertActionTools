using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Single fixed-size string shown when the player tries to open a file their
    /// security clearance does not permit: "This information requires security
    /// clearance: ". The trailing space is intentional -- the game appends the
    /// required clearance level onto the end of the message at render time.
    /// </summary>
    public class LoadFailedStringSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[]
        {
            47, // "This information requires security clearance: "
        };

        public LoadFailedStringSection Clone()
        {
            return new LoadFailedStringSection { Strings = Strings.ToList() };
        }
    }
}
