using System;

namespace CovertActionTools.Core.Models.Executables.Records.Shared
{
    [Flags]
    public enum WallDirection : byte
    {
        Unknown = 0,
        North = 0b0001,
        South = 0b0010,
        NorthSouth = North | South,
        West = 0b0100,
        NorthWest = North | West,
        SouthWest = South | West,
        NorthSouthWest = North | South | West,
        East = 0b1000,
        NorthEast = North | East,
        SouthEast = South | East,
        NorthSouthEast = North | South | East,
        WestEast = West | East,
        NorthWestEast = North | West | East,
        SouthWestEast = South | West | East,
        All = North | South | West | East,
    }
}
