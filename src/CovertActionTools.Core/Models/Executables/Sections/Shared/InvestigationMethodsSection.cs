using System.Linq;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    /// <summary>
    /// Eight investigation method names ("Clandestine Photo", "Electronic WireTap",
    /// ... "Local Authorities", "Clue") shown on the investigation choice menu.
    /// </summary>
    public class InvestigationMethodsSection : ExactCountFixedSizeStringTableSection
    {
        public const int MethodCount = 8;

        protected override int[] StringSizes => new[]
        {
            18, // "Clandestine Photo"
            19, // "Electronic WireTap"
            20, // "Covert Surveillance"
            19, // "File Record Search"
            16, // "Local Informant"
            19, // "INTERPOL Data Base"
            18, // "Local Authorities"
            5,  // "Clue"
        };

        public InvestigationMethodsSection Clone()
        {
            return new InvestigationMethodsSection { Strings = Strings.ToList() };
        }
    }
}
