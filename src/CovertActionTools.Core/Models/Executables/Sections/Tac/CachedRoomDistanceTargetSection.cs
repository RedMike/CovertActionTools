using CovertActionTools.Core.Models.Executables.Records.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    /// <summary>
    /// Cached "last target room" used by FUN_10e8_6cb1 (room-distance BFS). Initialised
    /// to 0xFFFF (-1, no room cached) so the first call always runs the relaxation loop.
    /// </summary>
    public class CachedRoomDistanceTargetSection : IExecutableSection
    {
        private const int ByteSize = 2;

        public BlobRecord Data { get; set; } = new(ByteSize);

        public bool Viewable()
        {
            return false;
        }

        public bool Editable()
        {
            return false;
        }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            Data = new BlobRecord(ByteSize);
            return Data.ReadBytes(fullPayload, startingOffset);
        }

        public byte[] WriteBytes()
        {
            return Data.WriteBytes();
        }

        public CachedRoomDistanceTargetSection Clone()
        {
            return new CachedRoomDistanceTargetSection
            {
                Data = Data.Clone()
            };
        }
    }
}
