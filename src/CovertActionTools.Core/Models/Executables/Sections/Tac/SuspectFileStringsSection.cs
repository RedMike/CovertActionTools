using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// 29 fixed-size strings used by the suspect-file / clue-formatting code: the
    /// labelled field headers on the suspect dossier screen ("Name:", "Rank:",
    /// "Org:", "City:", "Role:" variants), the connective phrases that stitch
    /// rendered clue sentences together (" by ", " in ", " on ", " someone",
    /// " of the "), the suspect-status parentheticals ("(Under Arrest)",
    /// "(In Hiding)", "(Turned)"), quote marks, the "(message not decoded)"
    /// placeholder, and the "Rcvd msg fm" / "Sent msg to" wiretap log prefixes.
    /// Slots marked [0x80] carry a color control byte; empty slots are 1-byte
    /// formatter resets used between sentence builds.
    /// </summary>
    public class SuspectFileStringsSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[]
        {
            7,  // "Name:[0x80]"
            7,  // "Rank:[0x80]"
            7,  // "Org: [0x80]"
            8,  // "City: [0x80]"
            10, // "Recruited"
            5,  // " by "
            5,  // " in "
            2,  // "."
            8,  // "Role: [0x80]"
            20, // "Role: [0x80]NOT INVOLVED"
            15, // "Role: [0x80]unknown"
            15, // "(Under Arrest)"
            12, // "(In Hiding)"
            9,  // "(Turned)"
            1,  // ""
            2,  // "'"
            2,  // "'"
            22, // "(message not decoded)"
            8,  // "...more"
            1,  // ""
            12, // "Rcvd msg fm"
            12, // "Sent msg to"
            2,  // " "
            9,  // " someone"
            9,  // " of the "
            5,  // " in "
            5,  // " on "
            2,  // " "
            2,  // "."
        };

        public SuspectFileStringsSection Clone()
        {
            return new SuspectFileStringsSection { Strings = Strings.ToList() };
        }
    }
}
