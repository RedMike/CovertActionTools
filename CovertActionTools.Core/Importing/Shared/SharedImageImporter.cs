using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace CovertActionTools.Core.Importing.Shared
{
    public class SharedImageImporter
    {
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif
        
        private readonly ILogger<SharedImageImporter> _logger;

        public SharedImageImporter(ILogger<SharedImageImporter> logger)
        {
            _logger = logger;
        }

        public SimpleImageModel Import(string path, string filename, SimpleImageModel.ImageData imageData)
        {
            var model = new SimpleImageModel();
            model.Key = filename;
            model.ExtraData = imageData;
            (model.RawVgaImageData, model.VgaImageData) = ReadVgaImageData(path, filename, model.ExtraData.LegacyWidth, model.ExtraData.LegacyHeight);
            model.CgaImageData = Array.Empty<byte>();
            if (model.ExtraData.LegacyColorMappings != null)
            {
                var rawCgaImageData = ImageConversion.VgaToCgaTexture(model.ExtraData.LegacyWidth, model.ExtraData.LegacyHeight, model.RawVgaImageData, model.ExtraData.LegacyColorMappings);
                using var skBitmap = SKBitmap.Decode(rawCgaImageData, new SKImageInfo(model.ExtraData.LegacyWidth, model.ExtraData.LegacyHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
                var textureBytes = skBitmap.Bytes.ToArray();
                model.CgaImageData = textureBytes;
            }
            model.ModernImageData = ReadModernImageData(path, filename, model.ExtraData.Width, model.ExtraData.Height);
            return model;
        }
        
        public (byte[] raw, byte[] texture) ReadVgaImageData(string path, string filename, int width, int height)
        {
            var filePath = Path.Combine(path, $"{filename}_VGA.png");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing PNG file: {filename}");
            }

            var rawImageBytes = File.ReadAllBytes(filePath);
            using var skBitmap = SKBitmap.Decode(rawImageBytes, new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var textureBytes = skBitmap.Bytes.ToArray();
            var rawBytes = ImageConversion.TextureToVga(width, height, textureBytes);

            return (rawBytes, textureBytes);
        }

        public byte[] ReadModernImageData(string path, string filename, int width, int height)
        {
            var filePath = Path.Combine(path, $"{filename}_modern.png");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing PNG file: {filename}");
            }

            var bytes = File.ReadAllBytes(filePath);
            using var skBitmap = SKBitmap.Decode(bytes, new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var imageBytes = skBitmap.Bytes.ToArray();
            return imageBytes;
        }
        
        public SharedMetadata ReadMetadata(string path, string key, string suffix)
        {
            var filePath = System.IO.Path.Combine(path, $"{key}_{suffix}.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {key}");
            }

            var json = File.ReadAllText(filePath);
            var metadata = JsonSerializer.Deserialize<SharedMetadata>(json);
            if (metadata == null)
            {
                throw new Exception($"Unparseable JSON file: {key}");
            }
            return metadata;
        }
        
        public SimpleImageModel.ImageData ReadImageData(string path, string key, string suffix)
        {
            var filePath = System.IO.Path.Combine(path, $"{key}_{suffix}.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {key}");
            }

            var json = File.ReadAllText(filePath);
            var metadata = JsonSerializer.Deserialize<SimpleImageModel.ImageData>(json);
            if (metadata == null)
            {
                throw new Exception($"Unparseable JSON file: {key}");
            }
            return metadata;
        }
    }
}