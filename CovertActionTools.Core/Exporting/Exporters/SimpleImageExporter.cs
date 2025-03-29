using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace CovertActionTools.Core.Exporting.Exporters
{
    /// <summary>
    /// Given a loaded model for a SimpleImage, returns multiple assets to save:
    ///   * PIC file (legacy image)
    ///   * JSON file (metadata)
    ///   * PNG file (modern image)
    ///   * PNG file VGA_ (VGA legacy image)
    ///   * PNG file CGA_ (CGA replacement image)
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

        public SimpleImageExporter(ILogger<SimpleImageExporter> logger)
        {
            _logger = logger;
        }

        public IDictionary<string, byte[]> Export(SimpleImageModel image)
        {
            var dict = new Dictionary<string, byte[]>
            {
                [$"{image.Key}.json"] = GetMetadata(image),
                [$"{image.Key}.png"] = GetModernImageData(image),
                [$"VGA_{image.Key}.png"] = GetVgaImageData(image),
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
    }
}