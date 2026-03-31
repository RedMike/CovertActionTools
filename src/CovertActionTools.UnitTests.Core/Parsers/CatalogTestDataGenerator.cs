using System.IO;
using System.Text;

namespace CovertActionTools.UnitTests.Core.Parsers;

public static class CatalogTestDataGenerator
{
    /// <summary>
    /// Builds a CAT file with the specified entries. Each entry contains a SharedImage binary
    /// built with format 0x07 (no CGA mappings). The compressed data is fake -- tests using
    /// this should mock ILzwDecompression.
    /// </summary>
    public static byte[] BuildCatalogFile(CatalogEntry[] entries)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((ushort)entries.Length);

        // Build image data for each entry first to know offsets and lengths
        var imageDataList = new byte[entries.Length][];
        for (var i = 0; i < entries.Length; i++)
        {
            imageDataList[i] = SharedImageTestDataGenerator.BuildFormat0x07Header(
                entries[i].Width, entries[i].Height, entries[i].DictionaryWidth,
                new byte[] { 0xDE, 0xAD });
        }

        // Header size: 2 (entryCount) + entries.Length * (12 + 4 + 4 + 4)
        var headerSize = 2 + entries.Length * 24;

        // Calculate offsets
        var currentOffset = (uint)headerSize;
        for (var i = 0; i < entries.Length; i++)
        {
            // Write entry name (12 bytes, null-padded, with ".PIC" suffix)
            var nameWithPic = entries[i].Name + ".PIC";
            var nameBytes = Encoding.ASCII.GetBytes(nameWithPic);
            var paddedName = new byte[12];
            for (var j = 0; j < nameBytes.Length && j < 12; j++)
            {
                paddedName[j] = nameBytes[j];
            }
            writer.Write(paddedName);

            // Checksum (ignored)
            writer.Write((uint)0);

            // Length
            writer.Write((uint)imageDataList[i].Length);

            // Offset
            writer.Write(currentOffset);

            currentOffset += (uint)imageDataList[i].Length;
        }

        // Write image data
        for (var i = 0; i < entries.Length; i++)
        {
            writer.Write(imageDataList[i]);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Writes a CAT file to the given directory.
    /// </summary>
    public static void WriteCatalogFile(string directory, string key, byte[] data)
    {
        File.WriteAllBytes(Path.Combine(directory, $"{key}.CAT"), data);
    }

    public class CatalogEntry
    {
        public string Name { get; set; } = "ENTRY";
        public ushort Width { get; set; } = 4;
        public ushort Height { get; set; } = 4;
        public byte DictionaryWidth { get; set; } = 11;
    }
}
