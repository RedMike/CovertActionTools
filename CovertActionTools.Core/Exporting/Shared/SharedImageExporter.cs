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
        private readonly ILoggerFactory _loggerFactory;

        public SharedImageExporter(ILogger<SharedImageExporter> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
        }
        
        public byte[] GetVgaImageData(SimpleImageModel image)
        {
            var imageData = image.RawVgaImageData;
            var width = image.ExtraData.LegacyWidth;
            var height = image.ExtraData.LegacyHeight;

            return ImageConversion.VgaToTexture(width, height, imageData);
        }

        public byte[] GetModernImageData(SimpleImageModel image)
        {
            var imageData = image.ModernImageData;
            var width = image.ExtraData.LegacyWidth;
            var height = image.ExtraData.LegacyHeight;

            return ImageConversion.RgbaToTexture(width, height, imageData);
        }

        public byte[] GetLegacyFileData(SimpleImageModel image)
        {
            var imageData = image.RawVgaImageData;
            
            var compression = new LzwCompression(_loggerFactory.CreateLogger(typeof(LzwCompression)),
                image.ExtraData.CompressionDictionaryWidth, imageData, image.Key);
            var imageBytes = compression.Compress(image.ExtraData.LegacyWidth, image.ExtraData.LegacyHeight);

            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            ushort formatFlag = 0x07;
            if (image.ExtraData.LegacyColorMappings != null)
            {
                formatFlag = 0x0F;
            }

            writer.Write(formatFlag);
            writer.Write((ushort)image.ExtraData.LegacyWidth);
            writer.Write((ushort)image.ExtraData.LegacyHeight);
            if (image.ExtraData.LegacyColorMappings != null)
            {
                var mappingBytes = image.ExtraData.LegacyColorMappings
                    .OrderBy(x => x.Key)
                    .Select(x => x.Value)
                    .ToArray();
                writer.Write(mappingBytes);
            }
            writer.Write(image.ExtraData.CompressionDictionaryWidth);
            writer.Write(imageBytes);
            
            return memStream.ToArray();
        }
        
        public byte[] GetMetadata(SimpleImageModel image)
        {
            var serialisedMetadata = JsonSerializer.Serialize(image.ExtraData, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(serialisedMetadata);
            return bytes;
        }
    }
}