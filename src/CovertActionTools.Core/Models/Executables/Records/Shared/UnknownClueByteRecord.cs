namespace CovertActionTools.Core.Models.Executables.Records.Shared
{
    /// <summary>
    /// One byte of the opaque UnknownClueData block preceding the popcount table in
    /// SharedClueAndIntel. Exposed as a per-byte record so the editor UI can test each
    /// slot individually while we hunt the reader -- see SharedClueAndIntelSection TODO.
    /// </summary>
    public class UnknownClueByteRecord : IExecutableRecord
    {
        public const int RecordSize = 1;

        public byte Value { get; set; }

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            Value = fullPayload[startingOffset];
            return RecordSize;
        }

        public byte[] WriteBytes()
        {
            return new[] { Value };
        }

        public UnknownClueByteRecord Clone()
        {
            return new UnknownClueByteRecord { Value = Value };
        }
    }
}
