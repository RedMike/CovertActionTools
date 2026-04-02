namespace CovertActionTools.Core.Compression
{
    internal class ExepackHeader
    {
        public ushort RealIP { get; set; }
        public ushort RealCS { get; set; }
        public ushort MemStart { get; set; }
        public ushort ExepackSize { get; set; }
        public ushort RealSP { get; set; }
        public ushort RealSS { get; set; }
        public ushort DestLen { get; set; }

        /// <summary>
        /// Byte offset of the EXEPACK segment within the file payload (CS * 16).
        /// </summary>
        public int SegmentOffset { get; set; }

        /// <summary>
        /// The packed data region (bytes before the EXEPACK segment).
        /// </summary>
        public byte[] PackedData { get; set; }

        /// <summary>
        /// The full EXEPACK segment (header + stub + error string + relocation table).
        /// </summary>
        public byte[] ExepackSegment { get; set; }
    }
}
