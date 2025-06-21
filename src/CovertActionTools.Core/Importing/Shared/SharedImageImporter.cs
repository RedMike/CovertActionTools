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

        public SharedImageModel ReadImage(string path, string filename, string suffix)
        {
            var model = new SharedImageModel();
            model.Data = ReadImageData(path, filename, suffix);
            (model.RawVgaImageData, model.VgaImageData) = ReadVgaImageData(path, filename, model.Data.Width, model.Data.Height);
            model.CgaImageData = Array.Empty<byte>();
            if (model.Data.LegacyColorMappings != null)
            {
                var rawCgaImageData = ImageConversion.VgaToCgaTexture(model.Data.Width, model.Data.Height, model.RawVgaImageData, model.Data.LegacyColorMappings);
                using var skBitmap = SKBitmap.Decode(rawCgaImageData, new SKImageInfo(model.Data.Width, model.Data.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
                var textureBytes = skBitmap.Bytes.ToArray();
                model.CgaImageData = textureBytes;
            }
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
        
        public SharedImageModel.ImageData ReadImageData(string path, string key, string suffix)
        {
            var filePath = System.IO.Path.Combine(path, $"{key}_{suffix}.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {key}");
            }

            var json = File.ReadAllText(filePath);
            var metadata = JsonSerializer.Deserialize<SharedImageModel.ImageData>(json);
            if (metadata == null)
            {
                throw new Exception($"Unparseable JSON file: {key}");
            }
            return metadata;
        }
    }
}