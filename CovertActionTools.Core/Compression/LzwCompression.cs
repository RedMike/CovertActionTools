using System.IO;
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

        public byte[] Compress(int length)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);
            
            
            
            
            var compressedBytes = memStream.ToArray();
            _logger.LogInformation($"Compressed from {_data.Length} bytes to {compressedBytes.Length}");
            return compressedBytes;
        }
    }
}