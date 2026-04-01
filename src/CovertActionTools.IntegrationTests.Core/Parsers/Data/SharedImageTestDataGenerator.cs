using System;
using System.Collections.Generic;

namespace CovertActionTools.IntegrationTests.Core.Parsers.Data;

public static class SharedImageTestDataGenerator
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

    /// <summary>
    /// Creates a CGA color mapping where each VGA index maps to a packed byte
    /// with both nibbles set to the same CGA index: (i % 4).
    /// Each nibble must be a valid CGA palette index (0-3).
    /// </summary>
    public static Dictionary<byte, byte> CreateIdentityCgaColorMappings()
    {
        var mappings = new Dictionary<byte, byte>();
        for (byte i = 0; i < 16; i++)
        {
            var cga = (byte)(i % 4);
            mappings[i] = (byte)((cga << 4) | cga);
        }
        return mappings;
    }

    /// <summary>
    /// Creates a CGA color mapping where each VGA index maps to a packed byte
    /// with both nibbles set to ((i + offset) % 4).
    /// Each nibble must be a valid CGA palette index (0-3).
    /// </summary>
    public static Dictionary<byte, byte> CreateOffsetCgaColorMappings(byte offset = 1)
    {
        var mappings = new Dictionary<byte, byte>();
        for (byte i = 0; i < 16; i++)
        {
            var cga = (byte)((i + offset) % 4);
            mappings[i] = (byte)((cga << 4) | cga);
        }
        return mappings;
    }
}
