using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Single 7-byte room-name label ("Street") displayed when the tactical
    /// screen shows the open-street ambush map -- the one location that has no
    /// entry in the regular RoomTypes table, since it isn't part of any
    /// organisation building.
    /// </summary>
    public class AmbushLocationRoomNameSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => new[]
        {
            7, // "Street"
        };

        public AmbushLocationRoomNameSection Clone()
        {
            return new AmbushLocationRoomNameSection { Strings = Strings.ToList() };
        }
    }
}
