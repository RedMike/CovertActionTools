using System.IO;
using CovertActionTools.Core.Compression.Streams;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Compression
{
    public interface ILzwCompression
    {
        CompressionResult Compress(int width, int height, int maxWordWidth, byte[] data, bool collectMetrics = false);
    }

    internal class LzwCompression : ILzwCompression
    {
        private readonly ILogger _logger;

        public LzwCompression(ILogger<LzwCompression> logger)
        {
            _logger = logger;
        }

        public CompressionResult Compress(int width, int height, int maxWordWidth, byte[] data, bool collectMetrics = false)
        {
            _logger.LogInformation("Starting compression from {DataLength} bytes, max word width {MaxWordWidth}",
                data.Length, maxWordWidth);

            var packedByteCount = CalculatePackedByteCount(width, height);

            using var pixelStream = new MemoryStream(data);
            using var packStream = new PixelPackingStream(pixelStream, width, height);
            using var countPacked = new CountingStream(packStream);
            using var rleStream = new RleEncodingStream(countPacked, packedByteCount);
            using var countRle = new CountingStream(rleStream);
            using var lzwStream = new LzwCompressingStream(countRle, maxWordWidth);

            using var outputStream = new MemoryStream();
            lzwStream.CopyTo(outputStream);
            var compressedBytes = outputStream.ToArray();

            var stages = collectMetrics
                ? new CompressionStageMetrics(
                    rawPixels: data.Length,
                    packedBytes: (int)countPacked.BytesRead,
                    rleBytes: (int)countRle.BytesRead,
                    lzwBytes: compressedBytes.Length)
                : null;

            _logger.LogDebug("Compressed from {OriginalSize} bytes to {CompressedSize} bytes",
                data.Length, compressedBytes.Length);

            return new CompressionResult(compressedBytes, data.Length, stages);
        }

        private static int CalculatePackedByteCount(int width, int height)
        {
            var total = 0;
            for (var y = 0; y < height; y++)
            {
                var stride = width;
                if (y < height - 1 && width % 2 == 1)
                    stride = width + 1;
                total += (stride + 1) / 2;
            }
            return total;
        }
    }
}
