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
            // Pack every two pixels into a single byte
            var packedBytes = PixelPackingUtility.PackPixels(width, height, _data);

            // Stream pipeline: packed pixels → RLE encode → LZW compress
            using var packedStream = new MemoryStream(packedBytes);
            using var rleStream = new RleEncodingStream(packedStream, packedBytes.Length);
            using var lzwStream = new LzwCompressingStream(rleStream, _maxWordWidth);

            using var outputStream = new MemoryStream();
            lzwStream.CopyTo(outputStream);

            var compressedBytes = outputStream.ToArray();
            _logger.LogDebug($"Compressed from {_data.Length} bytes to {compressedBytes.Length}");
            return compressedBytes;
        }
    }
}
