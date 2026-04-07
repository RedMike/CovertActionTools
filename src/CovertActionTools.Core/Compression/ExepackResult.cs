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
        /// Note: may be inflated by paragraph rounding — use PackedDeadZoneSize
        /// for the actual dead zone byte count in the packed data.
        /// </summary>
        public int DeadZoneBoundary { get; }

        /// <summary>
        /// The actual number of dead zone bytes in the packed data, determined by
        /// where the decompressor's read position ended (after the last command).
        /// This is independent of paragraph rounding and gives the true dead zone size.
        /// </summary>
        public int PackedDeadZoneSize { get; }

        public ExepackDecompressionResult(byte[] data, int deadZoneBoundary, int packedDeadZoneSize)
        {
            Data = data;
            DeadZoneBoundary = deadZoneBoundary;
            PackedDeadZoneSize = packedDeadZoneSize;
        }
    }
}
