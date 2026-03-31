using System.IO;

namespace CovertActionTools.UnitTests.Core.Parsers;

public static class AnimationTestDataGenerator
{
    /// <summary>
    /// Builds a minimal valid PAN file with BackgroundType.ClearToColor, zero images,
    /// and a minimal data section containing just an End instruction (0x14).
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

        // ClearToColor: clearColor + unknown2
        writer.Write(clearColor);
        writer.Write(unknown2);

        // 500-byte header (250 ushort pairs, all zero = no images)
        for (var i = 0; i < 250; i++)
        {
            writer.Write((ushort)0);
        }

        // Data section: dataSectionLength in 16-byte units
        // We need 16 bytes total: 1 byte for End opcode (0x14) + 15 padding bytes
        writer.Write((ushort)1); // 1 * 16 = 16 bytes

        // Data section: PushToStack (0x05 0x00 value) then End (0x14)
        // Minimal valid: just start with 0x05 then End
        writer.Write((byte)0x05); // PushToStack
        writer.Write((byte)0x00); // sub-opcode for PushToStack
        writer.Write((short)1);   // value
        writer.Write((byte)0x02); // WaitForFrames (consumes the push)
        writer.Write((byte)0x14); // End

        // Pad remaining bytes to fill the 16-byte data section
        var bytesWrittenInDataSection = 6;
        var paddingNeeded = 16 - bytesWrittenInDataSection;
        writer.Write(new byte[paddingNeeded]);

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
