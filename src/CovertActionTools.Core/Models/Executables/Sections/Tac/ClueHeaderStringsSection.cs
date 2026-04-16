using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Seven header/separator strings composed by the clue formatter when rendering
    /// a clue screen: the "Source:" / "Method:" / "Related Clues:" labels, the "/"
    /// separator between two linked clues, and the "...none" fallback when no
    /// related clues exist. All use color control bytes (0x87 = light grey,
    /// 0x8C = light red) embedded at the start of the string.
    /// </summary>
    public class ClueHeaderStringsSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[]
        {
            10, // "[0x87]Source: "
            2,  // "/"
            2,  // "\n"
            11, // "\n[0x87]Method: "
            16, // "[0x8C]Related Clues:"
            2,  // "\n"
            8,  // "...none"
        };

        public ClueHeaderStringsSection Clone()
        {
            return new ClueHeaderStringsSection { Strings = Strings.ToList() };
        }
    }
}
