using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public static class SimpleImageParserTestDataGenerator
{
    /// <summary>
    /// Builds a valid PIC file containing real LZW-compressed image data using SharedImageExporter.
    /// </summary>
    public static byte[] BuildPicFile(byte[] pixels, int width, int height,
        int dictionaryWidth = 11, Dictionary<byte, byte>? colorMappings = null)
    {
        var compression = new LzwCompression(NullLogger<LzwCompression>.Instance);
        var exporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, compression);

        var model = new SharedImageModel
        {
            RawVgaImageData = pixels,
            Data = new SharedImageModel.ImageData
            {
                Width = width,
                Height = height,
                CompressionDictionaryWidth = (byte)dictionaryWidth,
                LegacyColorMappings = colorMappings
            }
        };

        return exporter.GetLegacyFileData(model);
    }

    /// <summary>
    /// Writes a PIC file to the given directory.
    /// </summary>
    public static void WritePicFile(string directory, string key, byte[] data)
    {
        File.WriteAllBytes(Path.Combine(directory, $"{key}.PIC"), data);
    }
}
