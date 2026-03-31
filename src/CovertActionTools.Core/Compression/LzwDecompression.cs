using System.IO;
using CovertActionTools.Core.Compression.Streams;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Compression
{
    public interface ILzwDecompression
    {
        byte[] Decompress(int width, int height, int maxWordWidth, BinaryReader reader);
    }

    internal class LzwDecompression : ILzwDecompression
    {
        private readonly ILogger _logger;

        public LzwDecompression(ILogger<LzwDecompression> logger)
        {
            _logger = logger;
        }

        public byte[] Decompress(int width, int height, int maxWordWidth, BinaryReader reader)
        {
            // Stream pipeline: compressed bytes → LZW decompress → RLE decode → unpack pixels
            using var lzwStream = new LzwDecompressingStream(reader.BaseStream, maxWordWidth);
            using var rleStream = new RleDecodingStream(lzwStream);
            using var unpackStream = new PixelUnpackingStream(rleStream, width, height);

            // Read all decompressed pixels
            var pixelCount = width * height;
            var pixels = new byte[pixelCount];
            var totalRead = 0;
            while (totalRead < pixelCount)
            {
                var read = unpackStream.Read(pixels, totalRead, pixelCount - totalRead);
                if (read == 0)
                    break;
                totalRead += read;
            }

            return pixels;
        }
    }
}
