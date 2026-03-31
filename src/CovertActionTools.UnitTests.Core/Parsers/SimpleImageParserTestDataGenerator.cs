using System.IO;

namespace CovertActionTools.UnitTests.Core.Parsers;

public static class SimpleImageParserTestDataGenerator
{
    /// <summary>
    /// Builds a minimal PIC file containing a SharedImage binary with format 0x07.
    /// The compressed data is fake -- tests using this should mock ILzwDecompression.
    /// </summary>
    public static byte[] BuildPicFile(ushort width, ushort height, byte dictionaryWidth = 11)
    {
        return SharedImageTestDataGenerator.BuildFormat0x07Header(width, height, dictionaryWidth,
            new byte[] { 0xDE, 0xAD });
    }

    /// <summary>
    /// Writes a PIC file to the given directory with the specified key (filename without extension).
    /// </summary>
    public static void WritePicFile(string directory, string key, byte[] data)
    {
        File.WriteAllBytes(Path.Combine(directory, $"{key}.PIC"), data);
    }
}
