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
            _logger.LogInformation($"Starting compression from {data.Length} bytes, max word width {maxWordWidth}");

            var packedByteCount = CalculatePackedByteCount(width, height);

            using var pixelStream = new MemoryStream(data);
            using var packStream = new PixelPackingStream(pixelStream, width, height);

            byte[] compressedBytes;
            CompressionStageMetrics stages = null;

            if (collectMetrics)
            {
                var countPacked = new CountingStream(packStream);
                var rleStream = new RleEncodingStream(countPacked, packedByteCount);
                var countRle = new CountingStream(rleStream);
                var lzwStream = new LzwCompressingStream(countRle, maxWordWidth);

                using var outputStream = new MemoryStream();
                lzwStream.CopyTo(outputStream);
                compressedBytes = outputStream.ToArray();

                stages = new CompressionStageMetrics(
                    rawPixels: data.Length,
                    packedBytes: (int)countPacked.BytesRead,
                    rleBytes: (int)countRle.BytesRead,
                    lzwBytes: compressedBytes.Length
                );
            }
            else
            {
                var rleStream = new RleEncodingStream(packStream, packedByteCount);
                var lzwStream = new LzwCompressingStream(rleStream, maxWordWidth);

                using var outputStream = new MemoryStream();
                lzwStream.CopyTo(outputStream);
                compressedBytes = outputStream.ToArray();
            }

            _logger.LogDebug($"Compressed from {data.Length} bytes to {compressedBytes.Length}");
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
