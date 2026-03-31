using System;
using System.Collections.Generic;
using System.IO;

namespace CovertActionTools.UnitTests.Core.Parsers;

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
    /// Builds a binary stream in format 0x07 (no CGA mappings):
    /// [formatFlag:2][width:2][height:2][dictionaryWidth:1][compressedData:N]
    /// The compressedData is fake — tests using this should mock ILzwDecompression.
    /// </summary>
    public static byte[] BuildFormat0x07Header(ushort width, ushort height, byte dictionaryWidth, byte[]? trailingData = null)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((ushort)0x07);
        writer.Write(width);
        writer.Write(height);
        writer.Write(dictionaryWidth);
        if (trailingData != null)
        {
            writer.Write(trailingData);
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Builds a binary stream in format 0x0F (with CGA color mappings):
    /// [formatFlag:2][width:2][height:2][colorMappings:16][dictionaryWidth:1][compressedData:N]
    /// The compressedData is fake — tests using this should mock ILzwDecompression.
    /// </summary>
    public static byte[] BuildFormat0x0FHeader(ushort width, ushort height, byte dictionaryWidth,
        Dictionary<byte, byte> colorMappings, byte[]? trailingData = null)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((ushort)0x0F);
        writer.Write(width);
        writer.Write(height);
        var mappingBytes = new byte[16];
        for (byte i = 0; i < 16; i++)
        {
            mappingBytes[i] = colorMappings.ContainsKey(i) ? colorMappings[i] : i;
        }
        writer.Write(mappingBytes);
        writer.Write(dictionaryWidth);
        if (trailingData != null)
        {
            writer.Write(trailingData);
        }
        return ms.ToArray();
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
