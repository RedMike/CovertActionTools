using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Exporters
{
    /// <summary>
    /// Given a loaded model for a SimpleImage, returns multiple assets to save:
    ///   * PIC file (legacy image)
    ///   * JSON file _image (metadata)
    ///   * PNG file (modern image)
    ///   * PNG file _VGA (VGA legacy image)
    ///   * PNG file _CGA (CGA replacement image)
    /// </summary>
    internal class SimpleImageExporter : BaseExporter<Dictionary<string, SimpleImageModel>>
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
        
        private readonly List<string> _keys = new();
        private int _index = 0;

        public SimpleImageExporter(ILogger<SimpleImageExporter> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        protected override string Message => "Processing simple images..";

        protected override int GetTotalItemCountInPath()
        {
            return _keys.Count;
        }

        protected override int RunExportStepInternal()
        {
            var nextKey = _keys[_index];

            var files = Export(Data[nextKey]);
            foreach (var pair in files)
            {
                File.WriteAllBytes(System.IO.Path.Combine(Path, pair.Key), pair.Value);
            }

            return _index++;
        }

        protected override void OnExportStart()
        {
            _keys.AddRange(GetKeys());
            _index = 0;
            _logger.LogInformation($"Starting export of images: {_keys.Count}");
        }
        
        private List<string> GetKeys()
        {
            return Data.Keys.ToList();
        }

        private IDictionary<string, byte[]> Export(SimpleImageModel image)
        {
            var dict = new Dictionary<string, byte[]>
            {
                [$"{image.Key}_image.json"] = GetMetadata(image),
                [$"{image.Key}.png"] = GetModernImageData(image),
                [$"{image.Key}_VGA.png"] = GetVgaImageData(image),
                [$"{image.Key}.PIC"] = GetLegacyFileData(image) 
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

        private byte[] GetLegacyFileData(SimpleImageModel image)
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