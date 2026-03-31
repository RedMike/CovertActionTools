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
            // Stream pipeline: compressed bytes → LZW decompress → RLE decode
            using var lzwStream = new LzwDecompressingStream(reader.BaseStream, maxWordWidth);
            using var rleStream = new RleDecodingStream(lzwStream);

            // Read exactly the number of packed bytes we expect
            var packedByteCount = CalculatePackedByteCount(width, height);
            var packedBytes = new byte[packedByteCount];
            var totalRead = 0;
            while (totalRead < packedByteCount)
            {
                var read = rleStream.Read(packedBytes, totalRead, packedByteCount - totalRead);
                if (read == 0)
                    break;
                totalRead += read;
            }

            // Unpack pixels: each packed byte → two 4-bit pixels
            return PixelPackingUtility.UnpackPixels(width, height, packedBytes);
        }

        private static int CalculatePackedByteCount(int width, int height)
        {
            var total = 0;
            for (var y = 0; y < height; y++)
            {
                var stride = width;
                if (y < height - 1 && width % 2 == 1)
                    stride = width + 1;
                // Each packed byte holds 2 pixel positions; stride is always even here
                total += (stride + 1) / 2;
            }
            return total;
        }
    }
}
