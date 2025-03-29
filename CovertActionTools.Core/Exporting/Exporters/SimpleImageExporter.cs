using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Exporters
{
    /// <summary>
    /// Given a loaded model for a SimpleImage, returns multiple assets to save:
    ///   * PIC file (legacy image)
    ///   * JSON file (metadata)
    ///   * PNG file (modern image)
    ///   * PNG file _VGA (VGA legacy image)
    ///   * PNG file _CGA (CGA replacement image)
    /// </summary>
    public interface ISimpleImageExporter
    {
        IDictionary<string, byte[]> Export(SimpleImageModel image);
    }
    
    internal class SimpleImageExporter : ISimpleImageExporter
    {
        #if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
        #else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
        #endif
        
        private readonly ILogger<SimpleImageExporter> _logger;
        private readonly ILoggerFactory _loggerFactory;

        public SimpleImageExporter(ILogger<SimpleImageExporter> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public IDictionary<string, byte[]> Export(SimpleImageModel image)
        {
            var dict = new Dictionary<string, byte[]>
            {
                [$"{image.Key}.json"] = GetMetadata(image),
                [$"{image.Key}.png"] = GetModernImageData(image),
                [$"{image.Key}_VGA.png"] = GetVgaImageData(image),
                //TODO: CGA
                [$"{image.Key}.PIC"] = GetLegacyImageData(image) 
            };
            return dict;
        }

        private byte[] GetMetadata(SimpleImageModel image)
        {
            var serialisedMetadata = JsonSerializer.Serialize(image.ExtraData, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(serialisedMetadata);
            return bytes;
        }
        
        private byte[] GetVgaImageData(SimpleImageModel image)
        {
            var imageData = image.RawVgaImageData;
            var width = image.Width;
            var height = image.Height;

            return ImageConversion.VgaToTexture(width, height, imageData);
        }

        private byte[] GetModernImageData(SimpleImageModel image)
        {
            var imageData = image.ModernImageData;
            var width = image.Width;
            var height = image.Height;

            return ImageConversion.RgbaToTexture(width, height, imageData);
        }

        private byte[] GetLegacyImageData(SimpleImageModel image)
        {
            var imageData = image.RawVgaImageData;
            var width = image.Width;
            var height = image.Height;

            var compression = new LzwCompression(_loggerFactory.CreateLogger(typeof(LzwCompression)),
                image.ExtraData.CompressionDictionaryWidth, imageData);
            var bytes = compression.Compress(width * height);
            return bytes;
        }
    }
}