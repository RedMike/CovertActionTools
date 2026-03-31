using System;
using System.Collections.Generic;
using System.IO;

namespace CovertActionTools.UnitTests.Core.Parsers;

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
    /// Characters A, B, C each 8 pixels wide (1 byte per row), 4 rows tall.
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
                charWidths[i] = (byte)(bytesPerRow * 8); // full byte width
            }
        }

        if (faceData == null)
        {
            faceData = GenerateDefaultFaceData(charCount, charHeight, bytesPerRow);
        }

        // Layout:
        // [0..1] fontCount (ushort) = 1
        // [2..3] fontFaceOffset (ushort)
        // [4..4+charCount-1] charWidths
        // [4+charCount..4+charCount+7] config (8 bytes)
        // [4+charCount+8..] face data

        var headerSize = 2 + 2; // fontCount + one offset
        var fontFaceOffset = (ushort)(headerSize + charCount + 8);

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Header
        writer.Write((ushort)1); // fontCount
        writer.Write(fontFaceOffset);

        // Char widths
        writer.Write(charWidths);

        // Config (8 bytes)
        writer.Write(firstAscii);
        writer.Write(lastAscii);
        writer.Write(bytesPerRow);
        writer.Write(firstRow);
        writer.Write(lastRow);
        writer.Write(horizontalPadding);
        writer.Write(verticalPadding);
        writer.Write((byte)0x00); // padding

        // Face data: for each row, for each char, bytesPerRow bytes
        for (var row = 0; row < charHeight; row++)
        {
            for (var c = 0; c < charCount; c++)
            {
                var rowOffset = row * charCount * bytesPerRow + c * bytesPerRow;
                for (var b = 0; b < bytesPerRow; b++)
                {
                    writer.Write(faceData[c][row * bytesPerRow + b]);
                }
            }
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Builds a FONTS.CV binary with two fonts.
    /// </summary>
    public static byte[] BuildTwoFontBinary()
    {
        // Font 0: A-B, 1 byte per row, 2 rows tall
        // Font 1: X-Z, 1 byte per row, 3 rows tall
        byte firstAscii0 = 65; // A
        byte lastAscii0 = 66;  // B
        byte bytesPerRow0 = 1;
        byte firstRow0 = 0;
        byte lastRow0 = 1; // height 2
        var charCount0 = 2;
        var charHeight0 = 2;

        byte firstAscii1 = 88; // X
        byte lastAscii1 = 90;  // Z
        byte bytesPerRow1 = 1;
        byte firstRow1 = 0;
        byte lastRow1 = 2; // height 3
        var charCount1 = 3;
        var charHeight1 = 3;

        // Header: fontCount(2) + offset0(2) + offset1(2) = 6
        var headerSize = 2 + 2 * 2;

        // Font 0 region: charWidths(2) + config(8) + faceData(2*2*1 = 4)
        var font0WidthsStart = headerSize;
        var font0ConfigStart = font0WidthsStart + charCount0;
        var font0FaceStart = font0ConfigStart + 8;
        var font0End = font0FaceStart + charHeight0 * charCount0 * bytesPerRow0;

        // Font 1 region: charWidths(3) + config(8) + faceData(3*3*1 = 9)
        var font1WidthsStart = font0End;
        var font1ConfigStart = font1WidthsStart + charCount1;
        var font1FaceStart = font1ConfigStart + 8;

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Header
        writer.Write((ushort)2);
        writer.Write((ushort)font0FaceStart);
        writer.Write((ushort)font1FaceStart);

        // Font 0: char widths
        writer.Write((byte)6); // A width
        writer.Write((byte)7); // B width

        // Font 0: config
        writer.Write(firstAscii0);
        writer.Write(lastAscii0);
        writer.Write(bytesPerRow0);
        writer.Write(firstRow0);
        writer.Write(lastRow0);
        writer.Write((byte)1); // hPadding
        writer.Write((byte)1); // vPadding
        writer.Write((byte)0x00);

        // Font 0: face data (row-major: row0-charA, row0-charB, row1-charA, row1-charB)
        writer.Write((byte)0xFC); // A row 0: 11111100
        writer.Write((byte)0xFE); // B row 0: 11111110
        writer.Write((byte)0x80); // A row 1: 10000000
        writer.Write((byte)0xC0); // B row 1: 11000000

        // Font 1: char widths
        writer.Write((byte)5); // X width
        writer.Write((byte)4); // Y width
        writer.Write((byte)3); // Z width

        // Font 1: config
        writer.Write(firstAscii1);
        writer.Write(lastAscii1);
        writer.Write(bytesPerRow1);
        writer.Write(firstRow1);
        writer.Write(lastRow1);
        writer.Write((byte)2); // hPadding
        writer.Write((byte)3); // vPadding
        writer.Write((byte)0x00);

        // Font 1: face data
        writer.Write((byte)0xF8); // X row 0: 11111000
        writer.Write((byte)0xF0); // Y row 0: 11110000
        writer.Write((byte)0xE0); // Z row 0: 11100000
        writer.Write((byte)0x80); // X row 1: 10000000
        writer.Write((byte)0x40); // Y row 1: 01000000
        writer.Write((byte)0x20); // Z row 1: 00100000
        writer.Write((byte)0x00); // X row 2: 00000000
        writer.Write((byte)0x00); // Y row 2: 00000000
        writer.Write((byte)0x00); // Z row 2: 00000000

        return ms.ToArray();
    }

    /// <summary>
    /// Generates default face data where each character has a distinct pattern.
    /// Char 0 gets 0xFF (all bits set), Char 1 gets 0xAA, Char 2 gets 0x55.
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
    /// Creates the FONTS.CV file in the given directory.
    /// </summary>
    public static void WriteFontsCv(string directory, byte[] data)
    {
        File.WriteAllBytes(Path.Combine(directory, "FONTS.CV"), data);
    }

    #endregion

    #region Expected pixel helpers

    /// <summary>
    /// Converts a byte's bit pattern to expected pixel values (color 15 for set bits, 0 for unset),
    /// trimmed to the given width.
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

    #endregion
}
