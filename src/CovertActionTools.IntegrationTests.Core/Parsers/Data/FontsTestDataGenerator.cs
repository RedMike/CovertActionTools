using System.Collections.Generic;
using System.IO;

namespace CovertActionTools.IntegrationTests.Core.Parsers.Data;

internal static class FontsTestDataGenerator
{
    #region Default test parameters

    public const byte DefaultFirstAscii = 65; // 'A'
    public const byte DefaultLastAscii = 67;  // 'C'
    public const byte DefaultBytesPerRow = 1;
    public const byte DefaultFirstRow = 0;
    public const byte DefaultLastRow = 3;     // charHeight = 4
    public const byte DefaultHorizontalPadding = 1;
    public const byte DefaultVerticalPadding = 2;

    #endregion

    #region Binary building

    /// <summary>
    /// Builds a valid FONTS.CV binary with a single font using default parameters.
    /// Characters A, B, C each with specified widths, 4 rows tall.
    /// </summary>
    public static byte[] BuildSingleFontBinary(
        byte[] charWidths = null,
        byte[][] faceData = null,
        byte firstAscii = DefaultFirstAscii,
        byte lastAscii = DefaultLastAscii,
        byte bytesPerRow = DefaultBytesPerRow,
        byte firstRow = DefaultFirstRow,
        byte lastRow = DefaultLastRow,
        byte horizontalPadding = DefaultHorizontalPadding,
        byte verticalPadding = DefaultVerticalPadding)
    {
        var charCount = lastAscii - firstAscii + 1;
        var charHeight = lastRow - firstRow + 1;

        if (charWidths == null)
        {
            charWidths = new byte[charCount];
            for (var i = 0; i < charCount; i++)
            {
                charWidths[i] = (byte)(bytesPerRow * 8);
            }
        }

        if (faceData == null)
        {
            faceData = GenerateDefaultFaceData(charCount, charHeight, bytesPerRow);
        }

        var headerSize = 2 + 2; // fontCount + one offset
        var fontFaceOffset = (ushort)(headerSize + charCount + 8);

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((ushort)1);
        writer.Write(fontFaceOffset);

        writer.Write(charWidths);

        writer.Write(firstAscii);
        writer.Write(lastAscii);
        writer.Write(bytesPerRow);
        writer.Write(firstRow);
        writer.Write(lastRow);
        writer.Write(horizontalPadding);
        writer.Write(verticalPadding);
        writer.Write((byte)0x00);

        for (var row = 0; row < charHeight; row++)
        {
            for (var c = 0; c < charCount; c++)
            {
                for (var b = 0; b < bytesPerRow; b++)
                {
                    writer.Write(faceData[c][row * bytesPerRow + b]);
                }
            }
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Generates default face data with distinct patterns per character.
    /// </summary>
    public static byte[][] GenerateDefaultFaceData(int charCount, int charHeight, int bytesPerRow)
    {
        var patterns = new byte[] { 0xFF, 0xAA, 0x55, 0xCC, 0x33 };
        var result = new byte[charCount][];

        for (var c = 0; c < charCount; c++)
        {
            result[c] = new byte[charHeight * bytesPerRow];
            var pattern = patterns[c % patterns.Length];
            for (var i = 0; i < result[c].Length; i++)
            {
                result[c][i] = pattern;
            }
        }

        return result;
    }

    /// <summary>
    /// Converts a byte's bit pattern to expected pixel values.
    /// </summary>
    public static byte[] ByteToPixels(byte value, int width)
    {
        var pixels = new List<byte>();
        uint mask = 0x80;
        for (var i = 0; i < 8; i++)
        {
            var bit = (value & mask) > 0;
            pixels.Add((byte)(bit ? 15 : 0));
            mask >>= 1;
        }

        return pixels.GetRange(0, width).ToArray();
    }

    /// <summary>
    /// Creates the FONTS.CV file in the given directory.
    /// </summary>
    public static void WriteFontsCv(string directory, byte[] data)
    {
        File.WriteAllBytes(Path.Combine(directory, "FONTS.CV"), data);
    }

    #endregion
}
