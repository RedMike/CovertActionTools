namespace CovertActionTools.Core.Models.Executables.Records.Shared
{
    /// <summary>
    /// Arrow-key direction used when navigating the inventory selection screen.
    /// Serialization order matches the in-game 8-byte-per-row table layout (Up/Down/Left/Right).
    /// </summary>
    public enum MenuNavigationDirection : ushort
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3,
    }
}
