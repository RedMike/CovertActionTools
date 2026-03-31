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
            _logger.LogDebug("Starting decompression for {Width}x{Height} image, max word width {MaxWordWidth}",
                width, height, maxWordWidth);

            var pixelCount = width * height;
            var pixels = new byte[pixelCount];

            CountingStream? countLzw = null;
            CountingStream? countRle = null;
            CountingStream? countPacked = null;

            Stream lzwInput = reader.BaseStream;
            if (collectMetrics) { countLzw = new CountingStream(lzwInput); lzwInput = countLzw; }

            using var lzwStream = new LzwDecompressingStream(lzwInput, maxWordWidth);

            Stream rleInput = lzwStream;
            if (collectMetrics) { countRle = new CountingStream(rleInput); rleInput = countRle; }

            using var rleStream = new RleDecodingStream(rleInput);

            Stream unpackInput = rleStream;
            if (collectMetrics) { countPacked = new CountingStream(unpackInput); unpackInput = countPacked; }

            using var unpackStream = new PixelUnpackingStream(unpackInput, width, height);

            ReadFully(unpackStream, pixels, pixelCount);

            var compressedSize = countLzw != null
                ? (int)countLzw.BytesRead
                : (int)reader.BaseStream.Position;

            var stages = collectMetrics
                ? new DecompressionStageMetrics(
                    lzwBytes: (int)countLzw!.BytesRead,
                    rleBytes: (int)countRle!.BytesRead,
                    packedBytes: (int)countPacked!.BytesRead,
                    rawPixels: pixelCount)
                : null;

            _logger.LogDebug("Decompressed from {CompressedSize} bytes to {PixelCount} pixels",
                compressedSize, pixelCount);

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
