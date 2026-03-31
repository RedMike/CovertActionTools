using System;

namespace CovertActionTools.UnitTests.Core.Helpers;

public static class TestDataGenerator
{
    public static byte[] GenerateUniformPixels(int width, int height, byte value = 5)
    {
        var data = new byte[width * height];
        Array.Fill(data, value);
        return data;
    }

    public static byte[] GenerateVariedPixels(int width, int height, int seed = 42)
    {
        var rng = new Random(seed);
        var data = new byte[width * height];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (byte)rng.Next(0, 16);
        }
        return data;
    }

    public static byte[] Generate0x90Pixels(int width, int height)
    {
        // Alternating 0 and 9 so PackPixels produces 0x90 bytes:
        // p1=0, p2=9 → ((9 & 0x0F) << 4) | (0 & 0x0F) = 0x90
        var data = new byte[width * height];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (i % 2 == 0) ? (byte)0 : (byte)9;
        }
        return data;
    }
}
