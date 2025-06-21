using System;
using System.IO;

namespace CovertActionTools.Core.Compression
{
    public static class PixelPackingUtility
    {
        public static byte[] PackPixels(int width, int height, byte[] data)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);
            var i = 0;
            for (var y = 0; y < height; y++)
            {
                var stride = width;
                //each byte has 2 pixels, if the width is -1 we have to append a fake pixel to keep it on the same line
                //but not last line because that can just end suddenly
                if (y < height - 1 && width % 2 == 1)
                {
                    stride = width + 1;
                }
                for (var x = 0; x < stride; x++)
                {
                    var p1 = data[i];
                    i++;
                    x++;
                    byte p2 = 0;
                    if (x < width) //if we're reading the fake pixel, don't increment actual byte count
                    {
                        p2 = data[i];
                        i++;
                    }

                    if (p1 > 16 || p2 > 16)
                    {
                        throw new Exception($"Pixel value too high: {p1:X} {p2:X}");
                    }

                    var mixedPixel = (byte)(((p2 & 0x0F) << 4) | (p1 & 0x0F));
                    writer.Write(mixedPixel);
                }
            }

            return memStream.ToArray();
        }

        public static byte[] UnpackPixels(int width, int height, byte[] data)
        {
            using var memStream = new MemoryStream();
            using var writer = new BinaryWriter(memStream);

            var i = 0;
            for (var y = 0; y < height; y++)
            {
                var stride = width;
                //each byte has 2 pixels, if the width is -1 we have to append a fake pixel to keep it on the same line
                //but not last line because that can just end suddenly
                if (y < height - 1 && width % 2 == 1)
                {
                    stride = width + 1;
                }

                for (var x = 0; x < stride; x++)
                {
                    var pixel = data[i];
                    i++;
                    //each byte is actually two pixels one after the other
                    writer.Write((byte)(pixel & 0x0f));
                    x++;
                    //but if it's the padding byte to keep the stride, we don't want to actually add it to the data
                    if (x < width)
                    {
                        writer.Write((byte)((pixel >> 4) & 0x0f));
                    }
                }
            }

            return memStream.ToArray();
        }
    }
}