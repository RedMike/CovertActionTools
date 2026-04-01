using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers.Data;

public static class AnimationIntegrationTestDataGenerator
{
    /// <summary>
    /// Builds a minimal valid PAN file with BackgroundType.ClearToColor, zero images,
    /// and a minimal data section. Uses no image compression since there are no images.
    /// </summary>
    public static byte[] BuildMinimalPanFile(
        ushort boundingWidth = 99,
        ushort boundingHeight = 79,
        ushort frameSkip = 1,
        byte clearColor = 0,
        byte unknown2 = 0)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // 4-byte prefix "PANI"
        writer.Write((byte)0x50);
        writer.Write((byte)0x41);
        writer.Write((byte)0x4E);
        writer.Write((byte)0x49);

        // 5-byte tag
        writer.Write((byte)0x03);
        writer.Write((byte)0x01);
        writer.Write((byte)0x01);
        writer.Write((byte)0x00);
        writer.Write((byte)0x03);

        // 15 color mapping bytes (indices 1-15, identity mapping)
        for (byte i = 1; i <= 15; i++)
        {
            writer.Write(i);
        }

        // 5 zero padding bytes
        writer.Write(new byte[5]);

        // aWidth, aHeight, frameSkip
        writer.Write(boundingWidth);
        writer.Write(boundingHeight);
        writer.Write(frameSkip);

        // backgroundType = ClearToColor (0x02)
        writer.Write((byte)0x02);

        // ClearToColor bytes
        writer.Write(clearColor);
        writer.Write(unknown2);

        // 500-byte header (250 ushort pairs, all zero = no images)
        for (var i = 0; i < 250; i++)
        {
            writer.Write((ushort)0);
        }

        // Data section: 1 * 16 = 16 bytes
        writer.Write((ushort)1);

        // PushToStack + WaitForFrames + End
        writer.Write((byte)0x05); // PushToStack
        writer.Write((byte)0x00);
        writer.Write((short)1);
        writer.Write((byte)0x02); // WaitForFrames
        writer.Write((byte)0x14); // End

        // Pad remaining bytes
        var bytesWritten = 6;
        writer.Write(new byte[16 - bytesWritten]);

        return ms.ToArray();
    }

    /// <summary>
    /// Writes a PAN file to the given directory.
    /// </summary>
    public static void WritePanFile(string directory, string key, byte[] data)
    {
        File.WriteAllBytes(Path.Combine(directory, $"{key}.PAN"), data);
    }
}
