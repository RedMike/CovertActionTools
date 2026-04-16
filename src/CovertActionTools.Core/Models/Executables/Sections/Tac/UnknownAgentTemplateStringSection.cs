using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Single 8-byte template string "Agent A" used as the prefix when the game
    /// generates a placeholder identity for an as-yet-unnamed agent. The final
    /// byte of the slot is the null terminator; the game overwrites the trailing
    /// character ('A') with the generated letter suffix at runtime.
    /// </summary>
    public class UnknownAgentTemplateStringSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[]
        {
            8, // "Agent A"
        };

        public UnknownAgentTemplateStringSection Clone()
        {
            return new UnknownAgentTemplateStringSection { Strings = Strings.ToList() };
        }
    }
}
