using System.Collections.Generic;
using System.IO;
using System.Text;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public static class CatalogIntegrationTestDataGenerator
{
    /// <summary>
    /// Builds a valid CAT file with real LZW-compressed image data for each entry.
    /// </summary>
    public static byte[] BuildCatalogFile(CatalogEntry[] entries)
    {
        var compression = new LzwCompression(NullLogger<LzwCompression>.Instance);
        var exporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, compression);

        // Generate compressed image data for each entry
        var imageDataList = new byte[entries.Length][];
        for (var i = 0; i < entries.Length; i++)
        {
            var model = new SharedImageModel
            {
                RawVgaImageData = entries[i].Pixels,
                Data = new SharedImageModel.ImageData
                {
                    Width = entries[i].Width,
                    Height = entries[i].Height,
                    CompressionDictionaryWidth = (byte)entries[i].DictionaryWidth,
                    LegacyColorMappings = null
                }
            };
            imageDataList[i] = exporter.GetLegacyFileData(model);
        }

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((ushort)entries.Length);

        // Header size: 2 (entryCount) + entries.Length * (12 + 4 + 4 + 4)
        var headerSize = 2 + entries.Length * 24;

        var currentOffset = (uint)headerSize;
        for (var i = 0; i < entries.Length; i++)
        {
            var nameWithPic = entries[i].Name + ".PIC";
            var nameBytes = Encoding.ASCII.GetBytes(nameWithPic);
            var paddedName = new byte[12];
            for (var j = 0; j < nameBytes.Length && j < 12; j++)
            {
                paddedName[j] = nameBytes[j];
            }
            writer.Write(paddedName);

            writer.Write((uint)0); // checksum
            writer.Write((uint)imageDataList[i].Length);
            writer.Write(currentOffset);

            currentOffset += (uint)imageDataList[i].Length;
        }

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
        public int Width { get; set; } = 4;
        public int Height { get; set; } = 4;
        public int DictionaryWidth { get; set; } = 11;
        public byte[] Pixels { get; set; } = new byte[16];
    }
}
