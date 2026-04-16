using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Single 21-byte "One moment please..." string shown as a loading indicator
    /// while the tactical engine streams map, sprite, or sound assets off disk.
    /// </summary>
    public class LoadingMessageStringSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[]
        {
            21, // "One moment please..."
        };

        public LoadingMessageStringSection Clone()
        {
            return new LoadingMessageStringSection { Strings = Strings.ToList() };
        }
    }
}
