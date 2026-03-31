using System;
using System.Collections.Generic;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Generates base64-encoded exported image data for snapshot tests.
/// Run this test to capture new baselines when the format changes intentionally.
/// </summary>
public class SharedImageSnapshotGenerator
{
    private readonly SharedImageExporter _exporter;

    public SharedImageSnapshotGenerator()
    {
        var compression = new LzwCompression(NullLogger<LzwCompression>.Instance);
        _exporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, compression);
    }

    [Fact]
    public void GenerateSnapshots()
    {
        // 4x4 uniform, format 0x07
        var result1 = ExportToBase64(
            SharedImageTestDataGenerator.GenerateUniformPixels(4, 4), 4, 4);

        // 4x4 varied, format 0x07
        var result2 = ExportToBase64(
            SharedImageTestDataGenerator.GenerateVariedPixels(4, 4), 4, 4);

        // 320x200 varied, format 0x07
        var result3 = ExportToBase64(
            SharedImageTestDataGenerator.GenerateVariedPixels(320, 200), 320, 200);

        // 4x4 uniform, format 0x0F with identity mappings
        var result4 = ExportToBase64(
            SharedImageTestDataGenerator.GenerateUniformPixels(4, 4), 4, 4,
            colorMappings: SharedImageTestDataGenerator.CreateIdentityCgaColorMappings());

        // 512x2 varied, format 0x07
        var result6 = ExportToBase64(
            SharedImageTestDataGenerator.GenerateVariedPixels(512, 2), 512, 2);

        // 4x4 varied, format 0x0F with offset-3 mappings
        var result5 = ExportToBase64(
            SharedImageTestDataGenerator.GenerateVariedPixels(4, 4), 4, 4,
            colorMappings: SharedImageTestDataGenerator.CreateOffsetCgaColorMappings(3));

        // Write snapshot values to a temp file for capture.
        // Run this test manually, then copy values into SharedImageSnapshotData.cs.
        var lines = new[]
        {
            $"Exported_4x4_Uniform_Format0x07 = \"{result1}\";",
            $"Exported_4x4_Varied_Format0x07 = \"{result2}\";",
            $"Exported_320x200_Varied_Format0x07 = \"{result3}\";",
            $"Exported_512x2_Varied_Format0x07 = \"{result6}\";",
            $"Exported_4x4_Uniform_Format0x0F_Identity = \"{result4}\";",
            $"Exported_4x4_Varied_Format0x0F_Offset3 = \"{result5}\";"
        };

        var path = Path.Combine(Path.GetTempPath(), "SharedImageSnapshots.txt");
        File.WriteAllLines(path, lines);
    }

    private string ExportToBase64(byte[] pixels, int width, int height,
        int dictionaryWidth = 11, Dictionary<byte, byte>? colorMappings = null)
    {
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

        var exportedBytes = _exporter.GetLegacyFileData(model);
        return Convert.ToBase64String(exportedBytes);
    }
}
