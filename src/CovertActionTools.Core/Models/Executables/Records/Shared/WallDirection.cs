using System;

namespace CovertActionTools.Core.Models.Executables.Records.Shared
{
    [Flags]
    public enum WallDirection : byte
    {
        Unknown = 0,
        North = 0b0001,
        South = 0b0010,
        West = 0b0100,
        East = 0b1000,
    }
}
