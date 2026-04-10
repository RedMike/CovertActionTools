using System;

namespace CovertActionTools.Core.Models.Executables.Records.Tac
{
    [Flags]
    public enum RoomSizeConstraint
    {
        Unknown = 0,
        Small = 0b0001,
        Medium = 0b0010,
        Large = 0b0100,
    }
}