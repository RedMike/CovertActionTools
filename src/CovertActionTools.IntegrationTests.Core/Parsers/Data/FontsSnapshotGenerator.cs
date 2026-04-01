using System;
using System.IO;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers.Data;

/// <summary>
/// Generates base64-encoded snapshot data for fonts tests.
/// Run this test to capture new baselines when the format changes intentionally.
/// Output appears in the test runner's output window (ITestOutputHelper).
/// </summary>
public class FontsSnapshotGenerator : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _tempDir;

    public FontsSnapshotGenerator(ITestOutputHelper output)
    {
        _output = output;
        _tempDir = Path.Combine(Path.GetTempPath(), $"FontsSnapshotGen_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void GenerateSnapshots()
    {
        // Snapshot 1: Single font with default parameters
        var defaultBinary = FontsTestDataGenerator.BuildSingleFontBinary();
        var defaultModel = ParseFonts(defaultBinary);
        _output.WriteLine($"SingleFont_Default_Binary = \"{Convert.ToBase64String(defaultBinary)}\";");
        _output.WriteLine($"SingleFont_Default_CharA_Pixels = \"{Convert.ToBase64String(defaultModel.Fonts[0].CharacterImages['A'].RawVgaImageData)}\";");
        _output.WriteLine($"SingleFont_Default_CharB_Pixels = \"{Convert.ToBase64String(defaultModel.Fonts[0].CharacterImages['B'].RawVgaImageData)}\";");
        _output.WriteLine($"SingleFont_Default_CharC_Pixels = \"{Convert.ToBase64String(defaultModel.Fonts[0].CharacterImages['C'].RawVgaImageData)}\";");

        // Snapshot 2: Custom widths
        var customWidths = new byte[] { 3, 5, 7 };
        var allFf = new byte[][]
        {
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
        };
        var customBinary = FontsTestDataGenerator.BuildSingleFontBinary(charWidths: customWidths, faceData: allFf);
        var customModel = ParseFonts(customBinary);
        _output.WriteLine($"SingleFont_CustomWidths_Binary = \"{Convert.ToBase64String(customBinary)}\";");
        _output.WriteLine($"SingleFont_CustomWidths_CharA_Pixels = \"{Convert.ToBase64String(customModel.Fonts[0].CharacterImages['A'].RawVgaImageData)}\";");
    }

    private FontsModel ParseFonts(byte[] binary)
    {
        FontsTestDataGenerator.WriteFontsCv(_tempDir, binary);
        var parser = new LegacyFontsParser(NullLogger<LegacyFontsParser>.Instance);
        parser.Start(_tempDir);
        parser.RunStep();
        var model = new PackageModel();
        parser.SetResult(model);
        // Clean up for next call
        File.Delete(Path.Combine(_tempDir, "FONTS.CV"));
        return model.Fonts;
    }
}
