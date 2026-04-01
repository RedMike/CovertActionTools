using System;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.IntegrationTests.Core.Parsers.Data;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Integration tests for LegacyFontsParser using real dependencies.
/// Uses snapshot data to detect regressions in the parsing output.
/// </summary>
public class LegacyFontsParserSnapshotTests : IDisposable
{
    private readonly string _tempDir;

    public LegacyFontsParserSnapshotTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyFontsParserIT_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Binary snapshot tests (input stability)

    [Fact]
    public void BinarySnapshot_SingleFont_Default_MatchesExpected()
    {
        var binary = FontsTestDataGenerator.BuildSingleFontBinary();
        var actual = Convert.ToBase64String(binary);
        Assert.Equal(FontsSnapshotData.SingleFont_Default_Binary, actual);
    }

    [Fact]
    public void BinarySnapshot_SingleFont_CustomWidths_MatchesExpected()
    {
        var charWidths = new byte[] { 3, 5, 7 };
        var allFf = new byte[][]
        {
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
        };
        var binary = FontsTestDataGenerator.BuildSingleFontBinary(charWidths: charWidths, faceData: allFf);
        var actual = Convert.ToBase64String(binary);
        Assert.Equal(FontsSnapshotData.SingleFont_CustomWidths_Binary, actual);
    }

    #endregion

    #region Parse snapshot tests (parsed model stability)

    [Fact]
    public void ParseSnapshot_SingleFont_Default_CharA_PixelsMatch()
    {
        var binary = Convert.FromBase64String(FontsSnapshotData.SingleFont_Default_Binary);
        var model = ParseFromBinary(binary);

        var actualPixels = Convert.ToBase64String(model.Fonts[0].CharacterImages['A'].RawVgaImageData);
        Assert.Equal(FontsSnapshotData.SingleFont_Default_CharA_Pixels, actualPixels);
    }

    [Fact]
    public void ParseSnapshot_SingleFont_Default_CharB_PixelsMatch()
    {
        var binary = Convert.FromBase64String(FontsSnapshotData.SingleFont_Default_Binary);
        var model = ParseFromBinary(binary);

        var actualPixels = Convert.ToBase64String(model.Fonts[0].CharacterImages['B'].RawVgaImageData);
        Assert.Equal(FontsSnapshotData.SingleFont_Default_CharB_Pixels, actualPixels);
    }

    [Fact]
    public void ParseSnapshot_SingleFont_Default_CharC_PixelsMatch()
    {
        var binary = Convert.FromBase64String(FontsSnapshotData.SingleFont_Default_Binary);
        var model = ParseFromBinary(binary);

        var actualPixels = Convert.ToBase64String(model.Fonts[0].CharacterImages['C'].RawVgaImageData);
        Assert.Equal(FontsSnapshotData.SingleFont_Default_CharC_Pixels, actualPixels);
    }

    [Fact]
    public void ParseSnapshot_SingleFont_Default_HasCorrectMetadata()
    {
        var binary = Convert.FromBase64String(FontsSnapshotData.SingleFont_Default_Binary);
        var model = ParseFromBinary(binary);

        var metadata = model.Data.Fonts[0];
        Assert.Equal(FontsTestDataGenerator.DefaultFirstAscii, metadata.FirstAsciiValue);
        Assert.Equal(FontsTestDataGenerator.DefaultLastAscii, metadata.LastAsciiValue);
        Assert.Equal(4, metadata.CharHeight);
        Assert.Equal(FontsTestDataGenerator.DefaultHorizontalPadding, metadata.HorizontalPadding);
        Assert.Equal(FontsTestDataGenerator.DefaultVerticalPadding, metadata.VerticalPadding);
        Assert.Equal(3, metadata.CharacterWidths.Count);
    }

    [Fact]
    public void ParseSnapshot_SingleFont_CustomWidths_CharA_PixelsMatch()
    {
        var binary = Convert.FromBase64String(FontsSnapshotData.SingleFont_CustomWidths_Binary);
        var model = ParseFromBinary(binary);

        var actualPixels = Convert.ToBase64String(model.Fonts[0].CharacterImages['A'].RawVgaImageData);
        Assert.Equal(FontsSnapshotData.SingleFont_CustomWidths_CharA_Pixels, actualPixels);
    }

    [Fact]
    public void ParseSnapshot_SingleFont_CustomWidths_CharA_HasCorrectDimensions()
    {
        var binary = Convert.FromBase64String(FontsSnapshotData.SingleFont_CustomWidths_Binary);
        var model = ParseFromBinary(binary);

        var charA = model.Fonts[0].CharacterImages['A'];
        Assert.Equal(3, charA.Data.Width);
        Assert.Equal(4, charA.Data.Height);
        Assert.Equal(3 * 4, charA.RawVgaImageData.Length);
    }

    #endregion

    #region Full pipeline tests

    [Fact]
    public void Parse_FullPipeline_RunStepReturnsTrueOnFirstCall()
    {
        var binary = FontsTestDataGenerator.BuildSingleFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, binary);
        var parser = new LegacyFontsParser(NullLogger<LegacyFontsParser>.Instance);
        parser.Start(_tempDir);

        var done = parser.RunStep();

        Assert.True(done);
    }

    [Fact]
    public void Parse_FullPipeline_GetMessageReturnsExpectedText()
    {
        var binary = FontsTestDataGenerator.BuildSingleFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, binary);
        var parser = new LegacyFontsParser(NullLogger<LegacyFontsParser>.Instance);
        parser.Start(_tempDir);

        var message = parser.GetMessage();

        Assert.Equal("Processing fonts..", message);
    }

    [Fact]
    public void Parse_FullPipeline_VgaImageDataIsPopulated()
    {
        var binary = FontsTestDataGenerator.BuildSingleFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, binary);

        var model = ParseFromDirectory();

        var charA = model.Fonts[0].CharacterImages['A'];
        Assert.NotEmpty(charA.VgaImageData);
    }

    #endregion

    #region Helpers

    private FontsModel ParseFromBinary(byte[] binary)
    {
        FontsTestDataGenerator.WriteFontsCv(_tempDir, binary);
        return ParseFromDirectory();
    }

    private FontsModel ParseFromDirectory()
    {
        var parser = new LegacyFontsParser(NullLogger<LegacyFontsParser>.Instance);
        parser.Start(_tempDir);
        parser.RunStep();
        var model = new PackageModel();
        parser.SetResult(model);
        return model.Fonts;
    }

    #endregion
}
