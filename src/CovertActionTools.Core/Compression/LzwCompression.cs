using System.IO;
using CovertActionTools.Core.Compression.Streams;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Compression
{
    public class LzwCompression
    {
        private readonly ILogger _logger;
        private readonly int _maxWordWidth;
        private readonly byte[] _data;

        public LzwCompression(ILogger logger, int maxWordWidth, byte[] data)
        {
            _logger = logger;
            _maxWordWidth = maxWordWidth;
            _data = data;
            _logger.LogInformation($"Starting compression from {data.Length} bytes, max word width {maxWordWidth}");
        }

        public byte[] Compress(int width, int height)
        {
            // Stream pipeline: raw pixels → pack → RLE encode → LZW compress
            using var pixelStream = new MemoryStream(_data);
            using var packStream = new PixelPackingStream(pixelStream, width, height);
            using var rleStream = new RleEncodingStream(packStream, CalculatePackedByteCount(width, height));
            using var lzwStream = new LzwCompressingStream(rleStream, _maxWordWidth);

            using var outputStream = new MemoryStream();
            lzwStream.CopyTo(outputStream);

            var compressedBytes = outputStream.ToArray();
            _logger.LogDebug($"Compressed from {_data.Length} bytes to {compressedBytes.Length}");
            return compressedBytes;
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
