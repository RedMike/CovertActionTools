namespace CovertActionTools.Core.Compression
{
    internal class ExepackDecompressionResult
    {
        /// <summary>
        /// The full decompressed output (dest_len * 16 bytes).
        /// Bytes 0..DeadZoneBoundary-1 are not written by decompression commands;
        /// the caller must copy the dead zone bytes from the original packed data.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// The lowest output position that was written by decompression commands.
        /// Bytes before this boundary are the "dead zone" (overlay manager stubs).
        /// </summary>
        public int DeadZoneBoundary { get; }

        public ExepackDecompressionResult(byte[] data, int deadZoneBoundary)
        {
            Data = data;
            DeadZoneBoundary = deadZoneBoundary;
        }
    }
}
