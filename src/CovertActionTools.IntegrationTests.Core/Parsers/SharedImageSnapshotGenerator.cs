using System;
using System.Collections.Generic;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Generates base64-encoded exported image data for snapshot tests.
/// Run this test to capture new baselines when the format changes intentionally.
/// Output appears in the test runner's output window (ITestOutputHelper).
/// </summary>
public class SharedImageSnapshotGenerator
{
    private readonly SharedImageExporter _exporter;
    private readonly ITestOutputHelper _output;

    public SharedImageSnapshotGenerator(ITestOutputHelper output)
    {
        _output = output;
        var compression = new LzwCompression(NullLogger<LzwCompression>.Instance);
        _exporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, compression);
    }

    [Fact]
    public void GenerateSnapshots()
    {
        var result1 = ExportToBase64(
            SharedImageTestDataGenerator.GenerateUniformPixels(4, 4), 4, 4);

        var result2 = ExportToBase64(
            SharedImageTestDataGenerator.GenerateVariedPixels(4, 4), 4, 4);

        var result3 = ExportToBase64(
            SharedImageTestDataGenerator.GenerateVariedPixels(512, 2), 512, 2);

        var result4 = ExportToBase64(
            SharedImageTestDataGenerator.GenerateUniformPixels(4, 4), 4, 4,
            colorMappings: SharedImageTestDataGenerator.CreateIdentityCgaColorMappings());

        var result5 = ExportToBase64(
            SharedImageTestDataGenerator.GenerateVariedPixels(4, 4), 4, 4,
            colorMappings: SharedImageTestDataGenerator.CreateOffsetCgaColorMappings(3));

        // Copy these values into SharedImageSnapshotData.cs
        _output.WriteLine($"Exported_4x4_Uniform_Format0x07 = \"{result1}\";");
        _output.WriteLine($"Exported_4x4_Varied_Format0x07 = \"{result2}\";");
        _output.WriteLine($"Exported_512x2_Varied_Format0x07 = \"{result3}\";");
        _output.WriteLine($"Exported_4x4_Uniform_Format0x0F_Identity = \"{result4}\";");
        _output.WriteLine($"Exported_4x4_Varied_Format0x0F_Offset3 = \"{result5}\";");
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
