using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Shared
{
    internal class SharedImageExporter
    {
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif
        
        private readonly ILogger<SharedImageExporter> _logger;
        private readonly ILzwCompression _compression;

        public SharedImageExporter(ILogger<SharedImageExporter> logger, ILzwCompression compression)
        {
            _logger = logger;
            _compression = compression;
        }
        
        public byte[] GetVgaImageData(SharedImageModel image)
        {
            var imageData = image.RawVgaImageData;
            var width = image.Data.Width;
            var height = image.Data.Height;

            return ImageConversion.VgaToTexture(width, height, imageData);
        }

        public byte[] GetLegacyFileData(SharedImageModel image)
        {
            var imageData = image.RawVgaImageData;
            
            var collectMetrics = _logger.IsEnabled(LogLevel.Debug);
            var result = _compression.Compress(image.Data.Width, image.Data.Height,
                image.Data.CompressionDictionaryWidth, imageData, collectMetrics);
            var imageBytes = result.Data;

            if (result.Stages != null)
            {
                _logger.LogDebug(
                    "Compression stages: {RawPixels} raw → {PackedBytes} packed → {RleBytes} RLE → {LzwBytes} LZW ({Ratio:P1})",
                    result.Stages.RawPixels, result.Stages.PackedBytes,
                    result.Stages.RleBytes, result.Stages.LzwBytes,
                    result.CompressionRatio);
            }

            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            ushort formatFlag = 0x07;
            if (image.Data.LegacyColorMappings != null)
            {
                formatFlag = 0x0F;
            }

            writer.Write(formatFlag);
            writer.Write((ushort)image.Data.Width);
            writer.Write((ushort)image.Data.Height);
            if (image.Data.LegacyColorMappings != null)
            {
                var mappingBytes = image.Data.LegacyColorMappings
                    .OrderBy(x => x.Key)
                    .Select(x => x.Value)
                    .ToArray();
                writer.Write(mappingBytes);
            }
            writer.Write(image.Data.CompressionDictionaryWidth);
            writer.Write(imageBytes);
            
            return memStream.ToArray();
        }
        
        public byte[] GetImageData(SharedImageModel image)
        {
            var data = JsonSerializer.Serialize(image.Data, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(data);
            return bytes;
        }
    }
}