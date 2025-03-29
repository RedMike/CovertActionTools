using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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
            var imageData = image.RawImageData;
            var width = image.Width;
            var height = image.Height;
            
            //TODO: optimise
            using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            IntPtr pixels = bitmap.GetPixels();
            int buffSize = bitmap.Height * bitmap.RowBytes;
            byte[] pixelBuffer = new byte[buffSize];
            int x = 0;
            int padding = bitmap.RowBytes - (4 * width);
            for (var i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    var pixel = imageData[i * width + j];
                    if (!Constants.VgaColorMapping.TryGetValue(pixel, out var col))
                    {
                        throw new Exception($"Invalid pixel value: {pixel}");
                    }
                    
                    var (r, g, b, a) = col;
                    pixelBuffer[x++] = r;
                    pixelBuffer[x++] = g;
                    pixelBuffer[x++] = b;
                    pixelBuffer[x++] = a;
                }

                x += padding;
            }

            Marshal.Copy(pixelBuffer, 0, pixels, buffSize);
            
            using var imageFile = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);
            using var memStream = new MemoryStream();
            imageFile.SaveTo(memStream);
            return memStream.ToArray();
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
            int x = 0;
            int padding = bitmap.RowBytes - (4 * width);
            for (var i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    pixelBuffer[x++] = imageData[(i*width + j)*4];
                    pixelBuffer[x++] = imageData[(i*width + j)*4+1];
                    pixelBuffer[x++] = imageData[(i*width + j)*4+2];
                    pixelBuffer[x++] = imageData[(i*width + j)*4+3];
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