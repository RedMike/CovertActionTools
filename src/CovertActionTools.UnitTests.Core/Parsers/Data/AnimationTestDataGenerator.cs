using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CovertActionTools.UnitTests.Core.Parsers.Data;

public static class AnimationTestDataGenerator
{
    /// <summary>
    /// Identity colour block: entry 0 as stored by legacy files (3), colours 1-15, border colour.
    /// </summary>
    public static byte[] IdentityColorBlock()
    {
        var block = new byte[17];
        block[0] = 0x03;
        for (byte i = 1; i <= 15; i++)
        {
            block[i] = i;
        }

        return block;
    }

    /// <summary>
    /// A raw format image: ignored word, width, height, one byte per pixel.
    /// </summary>
    public static byte[] RawImage(ushort width, ushort height, byte[] pixels)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((ushort)0);
        writer.Write(width);
        writer.Write(height);
        writer.Write(pixels);
        return ms.ToArray();
    }

    /// <summary>
    /// A compressed format image header with no data, for use with StubLzwDecompression.
    /// </summary>
    public static byte[] StubCompressedImage(ushort width, ushort height)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((ushort)0x07);
        writer.Write(width);
        writer.Write(height);
        writer.Write((byte)11);
        return ms.ToArray();
    }

    /// <summary>
    /// Instruction sub-section for a wait of one frame followed by End.
    /// </summary>
    public static byte[] MinimalDataSection()
    {
        return new byte[] { 0x05, 0x00, 0x01, 0x00, 0x02, 0x14 };
    }

    /// <summary>
    /// Builds a PAN file from its parts. Images are assigned to the given IDs in order,
    /// and the data section is padded to a 16-byte paragraph.
    /// </summary>
    public static byte[] BuildPanFile(
        byte version = 0x03,
        byte imageFormat = 0x01,
        bool hasColorBlock = true,
        byte colorBlockKind = 0x00,
        byte[]? colorBlock = null,
        ushort positionX = 0,
        ushort positionY = 0,
        ushort boundingWidth = 99,
        ushort boundingHeight = 79,
        ushort frameDelay = 1,
        byte backgroundType = 0x02,
        byte clearColor = 0,
        byte unknown2 = 0,
        byte[]? backgroundImage = null,
        IList<byte[]>? images = null,
        IList<int>? imageIds = null,
        byte[]? dataSection = null,
        int? declaredParagraphs = null)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // "PANI", version, image format
        writer.Write(new byte[] { 0x50, 0x41, 0x4E, 0x49 });
        writer.Write(version);
        writer.Write(imageFormat);

        // colour block flag, kind and block
        writer.Write((byte)(hasColorBlock ? 0x01 : 0x00));
        if (hasColorBlock)
        {
            writer.Write(colorBlockKind);
            if (colorBlockKind == 0x00)
            {
                writer.Write(colorBlock ?? IdentityColorBlock());
            }
            else if (colorBlockKind == 0x02)
            {
                writer.Write(colorBlock ?? new byte[774]);
            }
        }

        writer.Write(positionX);
        writer.Write(positionY);
        writer.Write(boundingWidth);
        writer.Write(boundingHeight);
        writer.Write(frameDelay);
        writer.Write(backgroundType);

        if (backgroundType == 0x01)
        {
            WriteAligned(writer, backgroundImage ?? throw new ArgumentException("ClearToImage needs a background image"));
        }

        if (backgroundType == 0x02)
        {
            writer.Write(clearColor);
            writer.Write(unknown2);
        }

        // 250 entry index table, non-zero entries are images in file order
        images ??= new List<byte[]>();
        imageIds ??= Enumerable.Range(0, images.Count).ToList();
        for (var i = 0; i < 250; i++)
        {
            var index = imageIds.IndexOf(i);
            writer.Write((ushort)(index < 0 ? 0 : 1000 + index));
        }

        foreach (var image in images)
        {
            WriteAligned(writer, image);
        }

        dataSection ??= MinimalDataSection();
        var paragraphs = (dataSection.Length + 15) / 16;
        writer.Write((ushort)(declaredParagraphs ?? paragraphs));
        writer.Write(dataSection);
        writer.Write(new byte[paragraphs * 16 - dataSection.Length]);

        return ms.ToArray();
    }

    /// <summary>
    /// Builds a minimal valid PAN file with BackgroundType.ClearToColor, zero images,
    /// and a minimal data section containing a one frame wait and an End instruction (0x14).
    /// </summary>
    public static byte[] BuildMinimalPanFile(
        ushort boundingWidth = 99,
        ushort boundingHeight = 79,
        ushort frameDelay = 1,
        byte clearColor = 0,
        byte unknown2 = 0)
    {
        return BuildPanFile(
            boundingWidth: boundingWidth,
            boundingHeight: boundingHeight,
            frameDelay: frameDelay,
            clearColor: clearColor,
            unknown2: unknown2);
    }

    /// <summary>
    /// Writes a PAN file to the given directory.
    /// </summary>
    public static void WritePanFile(string directory, string key, byte[] data)
    {
        File.WriteAllBytes(Path.Combine(directory, $"{key}.PAN"), data);
    }

    private static void WriteAligned(BinaryWriter writer, byte[] data)
    {
        writer.Write(data);
        if (data.Length % 2 == 1)
        {
            writer.Write((byte)0);
        }
    }
}
