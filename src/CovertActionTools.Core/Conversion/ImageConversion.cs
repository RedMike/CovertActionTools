using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace CovertActionTools.Core.Conversion
{
    public static class ImageConversion
    {
        public static byte[] VgaToTexture(int width, int height, byte[] bytes)
        {
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
                    var pixel = bytes[i * width + j];
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
        
        public static byte[] VgaToCgaTexture(int width, int height, byte[] bytes, Dictionary<byte, byte> cgaMapping)
        {
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
                    var pixel = bytes[i * width + j];
                    
                    //if the pixel is 0x00, the CGA replacement does not run! it's transparent
                    var col = (Constants.TransparentColor);
                    if (pixel != 0x00)
                    {
                        //each pixel is one of two colours based on the CGA replacement
                        //if they differ, you get hashing; if they're the same, you get a solid colour
                        if (!cgaMapping.TryGetValue(pixel, out var cgaColours))
                        {
                            throw new Exception($"Invalid pixel value (no CGA mapping): {pixel}");
                        }

                        var cgaPixel1 = (byte)(cgaColours & 0xF);
                        var cgaPixel2 = (byte)((cgaColours >> 4) & 0xF);
                    
                        //we pick based on location which one it is
                        var cgaPixel = ((i + j) % 2) == 0 ? cgaPixel1 : cgaPixel2;
                    
                        if (!Constants.CgaColorMapping.TryGetValue(cgaPixel, out col))
                        {
                            throw new Exception($"Invalid CGA pixel value: {cgaPixel} {j}x{i} {cgaColours:X2}");
                        }
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

        public static byte[] TextureToVga(int width, int height, byte[] rawBytes)
        {
            //the raw bytes are pixel packed so for odd widths there's an extra byte at the end
            //except the last row which does not have it
            var bytes = new byte[width * height];
            for (var i = 0; i < height; i++)
            {
                for (var j = 0; j < width; j++)
                {
                    var r = rawBytes[(i * width + j) * 4 + 0];
                    var g = rawBytes[(i * width + j) * 4 + 1];
                    var b = rawBytes[(i * width + j) * 4 + 2];
                    var a = rawBytes[(i * width + j) * 4 + 3];
                    if (!Constants.ReverseVgaColorMapping.TryGetValue((r, g, b, a), out var pixel))
                    {
                        throw new Exception($"Invalid VGA color: {j}x{i} = {(r, g, b, a)}");
                    }

                    bytes[i * width + j] = pixel;
                }
            }

            return bytes;
        }

        public static byte[] RgbaToTexture(int width, int height, byte[] rawBytes)
        {
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
                    pixelBuffer[x++] = rawBytes[(i*width + j)*4];
                    pixelBuffer[x++] = rawBytes[(i*width + j)*4+1];
                    pixelBuffer[x++] = rawBytes[(i*width + j)*4+2];
                    pixelBuffer[x++] = rawBytes[(i*width + j)*4+3];
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