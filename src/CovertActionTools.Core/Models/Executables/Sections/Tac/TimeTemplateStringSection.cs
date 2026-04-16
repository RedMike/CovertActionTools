using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Single 9-byte time-of-day template string ("00:00:00") overwritten in
    /// place by the chronology/status-line formatter each time the clock is
    /// rendered. The zero digits are placeholders for hours/minutes/seconds.
    /// </summary>
    public class TimeTemplateStringSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[]
        {
            9, // "00:00:00"
        };

        public TimeTemplateStringSection Clone()
        {
            return new TimeTemplateStringSection { Strings = Strings.ToList() };
        }
    }
}
