namespace CovertActionTools.Core.Compression
{
    internal class MzHeader
    {
        public ushort LastPageBytes { get; set; }
        public ushort Pages { get; set; }
        public ushort RelocCount { get; set; }
        public ushort HeaderParagraphs { get; set; }
        public ushort MinExtra { get; set; }
        public ushort MaxExtra { get; set; }
        public ushort InitSS { get; set; }
        public ushort InitSP { get; set; }
        public ushort Checksum { get; set; }
        public ushort InitIP { get; set; }
        public ushort InitCS { get; set; }
        public ushort RelocOffset { get; set; }
        public ushort Overlay { get; set; }

        public int HeaderSize => HeaderParagraphs * 16;
    }
}
