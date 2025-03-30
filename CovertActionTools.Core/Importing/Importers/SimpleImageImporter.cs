using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace CovertActionTools.Core.Importing.Importers
{
    public interface ISimpleImageImporter
    {
        SimpleImageModel Import(string path, string filename);
    }
    
    internal class SimpleImageImporter : ISimpleImageImporter
    {
        private readonly ILogger<SimpleImageImporter> _logger;

        public SimpleImageImporter(ILogger<SimpleImageImporter> logger)
        {
            _logger = logger;
        }

        public SimpleImageModel Import(string path, string filename)
        {
            var model = new SimpleImageModel();
            model.Key = filename;
            //TODO: arbitrary sizes?
            model.Width = 320; //legacy width
            model.Height = 200; //legacy height
            model.ExtraData = ReadMetadata(path, filename);
            (model.RawVgaImageData, model.VgaImageData) = ReadVgaImageData(path, filename, model.Width, model.Height);
            model.CgaImageData = Array.Empty<byte>();
            if (model.ExtraData.LegacyColorMappings != null)
            {
                var rawCgaImageData = ImageConversion.VgaToCgaTexture(model.Width, model.Height, model.RawVgaImageData, model.ExtraData.LegacyColorMappings);
                using var skBitmap = SKBitmap.Decode(rawCgaImageData, new SKImageInfo(model.Width, model.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
                var textureBytes = skBitmap.Bytes.ToArray();
                model.CgaImageData = textureBytes;
            }
            model.ModernImageData = ReadModernImageData(path, filename, model.ExtraData.Width, model.ExtraData.Height);
            return model;
        }
        
        private (byte[] raw, byte[] texture) ReadVgaImageData(string path, string filename, int width, int height)
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

        private byte[] ReadModernImageData(string path, string filename, int width, int height)
        {
            var filePath = Path.Combine(path, $"{filename}.png");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing PNG file: {filename}");
            }

            var bytes = File.ReadAllBytes(filePath);
            using var skBitmap = SKBitmap.Decode(bytes, new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var imageBytes = skBitmap.Bytes.ToArray();
            return imageBytes;
        }

        private SimpleImageModel.Metadata ReadMetadata(string path, string filename)
        {
            var filePath = Path.Combine(path, $"{filename}.json");
            if (!File.Exists(filePath))
            {
                throw new Exception($"Missing JSON file: {filename}");
            }

            var json = File.ReadAllText(filePath);
            var metadata = JsonSerializer.Deserialize<SimpleImageModel.Metadata>(json);
            if (metadata == null)
            {
                throw new Exception($"Unparseable JSON file: {filename}");
            }
            return metadata;
        }
    }
}