using System.IO;
using CovertActionTools.Core.Compression.Streams;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Compression
{
    public interface ILzwDecompression
    {
        DecompressionResult Decompress(int width, int height, int maxWordWidth, BinaryReader reader, bool collectMetrics = false);
    }

    internal class LzwDecompression : ILzwDecompression
    {
        private readonly ILogger _logger;

        public LzwDecompression(ILogger<LzwDecompression> logger)
        {
            _logger = logger;
        }

        public DecompressionResult Decompress(int width, int height, int maxWordWidth, BinaryReader reader, bool collectMetrics = false)
        {
            var pixelCount = width * height;
            var pixels = new byte[pixelCount];

            int compressedSize;
            DecompressionStageMetrics stages = null;

            if (collectMetrics)
            {
                var countLzw = new CountingStream(reader.BaseStream);
                var lzwStream = new LzwDecompressingStream(countLzw, maxWordWidth);
                var countRle = new CountingStream(lzwStream);
                var rleStream = new RleDecodingStream(countRle);
                var countPacked = new CountingStream(rleStream);
                var unpackStream = new PixelUnpackingStream(countPacked, width, height);

                ReadFully(unpackStream, pixels, pixelCount);
                compressedSize = (int)countLzw.BytesRead;

                stages = new DecompressionStageMetrics(
                    lzwBytes: (int)countLzw.BytesRead,
                    rleBytes: (int)countRle.BytesRead,
                    packedBytes: (int)countPacked.BytesRead,
                    rawPixels: pixelCount
                );
            }
            else
            {
                var lzwStream = new LzwDecompressingStream(reader.BaseStream, maxWordWidth);
                var rleStream = new RleDecodingStream(lzwStream);
                var unpackStream = new PixelUnpackingStream(rleStream, width, height);

                ReadFully(unpackStream, pixels, pixelCount);
                compressedSize = (int)reader.BaseStream.Position;
            }

            return new DecompressionResult(pixels, compressedSize, stages);
        }

        private static void ReadFully(Stream stream, byte[] buffer, int count)
        {
            var totalRead = 0;
            while (totalRead < count)
            {
                var read = stream.Read(buffer, totalRead, count - totalRead);
                if (read == 0)
                    break;
                totalRead += read;
            }
        }
    }
}
