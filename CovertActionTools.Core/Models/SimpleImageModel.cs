using System;
using System.Collections.Generic;

namespace CovertActionTools.Core.Models
{
    public class SimpleImageModel
    {
        /// <summary>
        /// ID that also determines the filename
        /// </summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>
        /// Legacy: usually 320
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// Legacy: usually 200
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// Byte-byte mapping of VGA -> CGA colours
        /// If null, saved in specific format (0x7 instead of 0xF)
        /// </summary>
        public Dictionary<byte, byte>? LegacyColorMappings { get; set; }
        /// <summary>
        /// Legacy: only 11
        /// </summary>
        public byte CompressionDictionaryWidth { get; set; }
        /// <summary>
        /// Stored as 1 byte index into VGA palette, left-to-right, top-to-bottom.
        /// </summary>
        public byte[] RawImageData { get; set; } = Array.Empty<byte>();
        /// <summary>
        /// Stored as 4 bytes RGBA left-to-right, top-to-bottom.
        /// </summary>
        public byte[] ModernImageData { get; set; } = Array.Empty<byte>();
    }
}