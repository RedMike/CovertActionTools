using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
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
    ///   * PNG file _VGA (VGA legacy image)
    ///   * PNG file _CGA (CGA replacement image)
    /// </summary>
    public interface ISimpleImageExporter
    {
        IDictionary<string, byte[]> Export(SimpleImageModel image);
    }
    
    internal class SimpleImageExporter : ISimpleImageExporter
    {
        private readonly ILogger<SimpleImageExporter> _logger;

        public SimpleImageExporter(ILogger<SimpleImageExporter> logger)
        {
            _logger = logger;
        }

        public IDictionary<string, byte[]> Export(SimpleImageModel image)
        {
            var dict = new Dictionary<string, byte[]>
            {
                [$"{image.Key}.png"] = GetModernImageData(image)
            };
            return dict;
        }

        private byte[] GetModernImageData(SimpleImageModel image)
        {
            var imageData = image.ModernImageData;
            var width = image.Width;
            var height = image.Height;
            //TODO: optimise
            using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            IntPtr pixels = bitmap.GetPixels();
            int buffSize = bitmap.Height * bitmap.RowBytes;
            byte[] pixelBuffer = new byte[buffSize];
            int q = 0;
            int x = 0;
            int padding = bitmap.RowBytes - (4 * width);
            for (var row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    pixelBuffer[x++] = imageData[(row*width + col)*4];
                    pixelBuffer[x++] = imageData[(row*width + col)*4+1];
                    pixelBuffer[x++] = imageData[(row*width + col)*4+2];
                    pixelBuffer[x++] = imageData[(row*width + col)*4+3];
                }

                x += padding;
            }

            Marshal.Copy(pixelBuffer, 0, pixels, buffSize);
            
            using var imageFile = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);
            using var memStream = new MemoryStream();
            imageFile.SaveTo(memStream);
            return memStream.ToArray();
        }
    }
}