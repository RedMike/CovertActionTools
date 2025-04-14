using System.IO;
using System.Linq;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Exporters
{
    internal class SharedImageExporter
    {
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
            var width = image.Width;
            var height = image.Height;

            return ImageConversion.VgaToTexture(width, height, imageData);
        }

        public byte[] GetModernImageData(SimpleImageModel image)
        {
            var imageData = image.ModernImageData;
            var width = image.Width;
            var height = image.Height;

            return ImageConversion.RgbaToTexture(width, height, imageData);
        }

        public byte[] GetLegacyFileData(SimpleImageModel image)
        {
            var imageData = image.RawVgaImageData;

            var compression = new LzwCompression(_loggerFactory.CreateLogger(typeof(LzwCompression)),
                image.ExtraData.CompressionDictionaryWidth, imageData, image.Key);
            var imageBytes = compression.Compress();

            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            ushort formatFlag = 0x07;
            if (image.ExtraData.LegacyColorMappings != null)
            {
                formatFlag = 0x0F;
            }

            writer.Write(formatFlag);
            writer.Write((ushort)image.Width);
            writer.Write((ushort)image.Height);
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
    }
}