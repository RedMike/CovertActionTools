using System;

namespace CovertActionTools.Core.Models.Executables.Records.Tac
{
    [Flags]
    public enum MapObjectBehavior
    {
        Unknown = 0,
        BlocksMovement = 0b0000_0000_0001,
        Openable = 0b0000_0000_0010,
        Buggable = 0b0000_0000_0100,
        Photographable = 0b0000_0000_1000,
        IsDoor = 0b0000_0001_0000,
        BlocksLineOfSight = 0b0000_0010_0000,
        MultiTileHorizontal = 0b0000_0100_0000,
        Unused = 0b0000_1000_0000, //never set in the legacy exe
        WallAdjacent = 0b0001_0000_0000,
        PasswordTerminal = 0b0010_0000_0000,
    }
}