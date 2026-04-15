using System.Linq;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    /// <summary>
    /// Intel report text fragments concatenated by the clue/intel formatter to build
    /// rendered messages. Slots are paired with the intel-event opcodes the formatter
    /// dispatches on; many entries are short connective fragments ("\n", "\0",
    /// ".\n") rather than full sentences. Slot 0 is a deliberately empty string used
    /// by the formatter to reset the destination buffer before appending a suffix.
    /// </summary>
    public class IntelReportTextsSection : ExactCountFixedSizeStringTableSection
    {
        public const int TextCount = 21;

        protected override int[] StringSizes => new[]
        {
            1,  // "\0" (empty -- buffer reset slot, formerly modeled as IntelMidPadding)
            52, // " has been\nidentified as a participant\nin the plot.\n"
            1,  // "\0"
            36, // " has been positively\nidentified as "
            2,  // "\n"
            34, // "We have acquired a\nphotograph of "
            3,  // ".\n"
            1,  // "\0"
            22, // " has been\nspotted in "
            2,  // "\n"
            1,  // "\0"
            41, // " has been\nidentified as a member\nof the "
            2,  // "\n"
            52, // "We have obtained\nadditional information\nconcerning "
            2,  // "\n"
            32, // "We have determined\nthe rank of\n"
            2,  // "\n"
            39, // "We found recruiting\ninformation about\n"
            2,  // "\n"
            39, // "We found recruiting\ninformation about\n"
            2,  // "\n"
        };

        public IntelReportTextsSection Clone()
        {
            return new IntelReportTextsSection { Strings = Strings.ToList() };
        }
    }
}
