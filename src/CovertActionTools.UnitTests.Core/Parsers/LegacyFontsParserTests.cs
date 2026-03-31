using System;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class LegacyFontsParserTests : IDisposable
{
    private readonly LegacyFontsParser _parser;
    private readonly string _tempDir;

    public LegacyFontsParserTests()
    {
        _parser = new LegacyFontsParser(NullLogger<LegacyFontsParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyFontsParserTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Validation

    [Fact]
    public void CheckIfValid_WithFontsCv_ReturnsTrue()
    {
        FontsTestDataGenerator.WriteFontsCv(_tempDir, FontsTestDataGenerator.BuildSingleFontBinary());

        var result = _parser.CheckIfValid(_tempDir);

        Assert.True(result);
    }

    [Fact]
    public void CheckIfValid_WithoutFontsCv_ReturnsFalse()
    {
        var result = _parser.CheckIfValid(_tempDir);

        Assert.False(result);
    }

    #endregion

    #region Single font metadata

    [Fact]
    public void Parse_SingleFont_SetsCorrectCharacterCount()
    {
        var data = FontsTestDataGenerator.BuildSingleFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        Assert.Single(model.Fonts.Fonts);
        var metadata = model.Fonts.Data.Fonts[0];
        Assert.Equal(3, metadata.CharacterWidths.Count); // A, B, C
    }

    [Fact]
    public void Parse_SingleFont_SetsFirstAndLastAsciiValues()
    {
        var data = FontsTestDataGenerator.BuildSingleFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        var metadata = model.Fonts.Data.Fonts[0];
        Assert.Equal(FontsTestDataGenerator.DefaultFirstAscii, metadata.FirstAsciiValue);
        Assert.Equal(FontsTestDataGenerator.DefaultLastAscii, metadata.LastAsciiValue);
    }

    [Fact]
    public void Parse_SingleFont_SetsCharHeight()
    {
        var data = FontsTestDataGenerator.BuildSingleFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        var metadata = model.Fonts.Data.Fonts[0];
        Assert.Equal(4, metadata.CharHeight); // lastRow(3) - firstRow(0) + 1
    }

    [Fact]
    public void Parse_SingleFont_SetsPaddingValues()
    {
        var data = FontsTestDataGenerator.BuildSingleFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        var metadata = model.Fonts.Data.Fonts[0];
        Assert.Equal(FontsTestDataGenerator.DefaultHorizontalPadding, metadata.HorizontalPadding);
        Assert.Equal(FontsTestDataGenerator.DefaultVerticalPadding, metadata.VerticalPadding);
    }

    [Fact]
    public void Parse_SingleFont_SetsCommentToLegacyImport()
    {
        var data = FontsTestDataGenerator.BuildSingleFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        var metadata = model.Fonts.Data.Fonts[0];
        Assert.Equal("Legacy import", metadata.Comment);
    }

    [Fact]
    public void Parse_SingleFont_SetsDefaultCharWidths()
    {
        var data = FontsTestDataGenerator.BuildSingleFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        var metadata = model.Fonts.Data.Fonts[0];
        // Default widths are bytesPerRow * 8 = 8
        Assert.Equal(8, metadata.CharacterWidths['A']);
        Assert.Equal(8, metadata.CharacterWidths['B']);
        Assert.Equal(8, metadata.CharacterWidths['C']);
    }

    [Fact]
    public void Parse_SingleFont_CustomCharWidths()
    {
        var charWidths = new byte[] { 3, 5, 7 };
        var data = FontsTestDataGenerator.BuildSingleFontBinary(charWidths: charWidths);
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        var metadata = model.Fonts.Data.Fonts[0];
        Assert.Equal(3, metadata.CharacterWidths['A']);
        Assert.Equal(5, metadata.CharacterWidths['B']);
        Assert.Equal(7, metadata.CharacterWidths['C']);
    }

    #endregion

    #region Single font pixel data

    [Fact]
    public void Parse_SingleFont_AllBitsSet_ProducesAllColor15()
    {
        var charWidths = new byte[] { 8, 8, 8 };
        var faceData = new byte[][]
        {
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, // A: all rows 0xFF
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, // B: all rows 0xFF
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, // C: all rows 0xFF
        };
        var data = FontsTestDataGenerator.BuildSingleFontBinary(charWidths: charWidths, faceData: faceData);
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        var charA = model.Fonts.Fonts[0].CharacterImages['A'];
        Assert.All(charA.RawVgaImageData, pixel => Assert.Equal(15, pixel));
    }

    [Fact]
    public void Parse_SingleFont_AllBitsClear_ProducesAllColor0()
    {
        var charWidths = new byte[] { 8, 8, 8 };
        var faceData = new byte[][]
        {
            new byte[] { 0x00, 0x00, 0x00, 0x00 },
            new byte[] { 0x00, 0x00, 0x00, 0x00 },
            new byte[] { 0x00, 0x00, 0x00, 0x00 },
        };
        var data = FontsTestDataGenerator.BuildSingleFontBinary(charWidths: charWidths, faceData: faceData);
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        var charA = model.Fonts.Fonts[0].CharacterImages['A'];
        Assert.All(charA.RawVgaImageData, pixel => Assert.Equal(0, pixel));
    }

    [Fact]
    public void Parse_SingleFont_AlternatingBits_ProducesCorrectPattern()
    {
        // 0xAA = 10101010 -> color15, 0, 15, 0, 15, 0, 15, 0
        var charWidths = new byte[] { 8, 8, 8 };
        var faceData = new byte[][]
        {
            new byte[] { 0xAA, 0xAA, 0xAA, 0xAA },
            new byte[] { 0x55, 0x55, 0x55, 0x55 },
            new byte[] { 0x00, 0x00, 0x00, 0x00 },
        };
        var data = FontsTestDataGenerator.BuildSingleFontBinary(charWidths: charWidths, faceData: faceData);
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        var charA = model.Fonts.Fonts[0].CharacterImages['A'];
        var expectedRow = FontsTestDataGenerator.ByteToPixels(0xAA, 8);
        // First row of char A
        var firstRow = charA.RawVgaImageData.Take(8).ToArray();
        Assert.Equal(expectedRow, firstRow);
    }

    [Fact]
    public void Parse_SingleFont_CharWidthTruncatesPixels()
    {
        // Width is 3, but byte is 0xFF (8 bits) - should truncate to 3 pixels
        var charWidths = new byte[] { 3, 3, 3 };
        var faceData = new byte[][]
        {
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
        };
        var data = FontsTestDataGenerator.BuildSingleFontBinary(charWidths: charWidths, faceData: faceData);
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        var charA = model.Fonts.Fonts[0].CharacterImages['A'];
        Assert.Equal(3, charA.Data.Width);
        Assert.Equal(4, charA.Data.Height);
        Assert.Equal(3 * 4, charA.RawVgaImageData.Length);
    }

    [Fact]
    public void Parse_SingleFont_ImageDimensionsMatchMetadata()
    {
        var data = FontsTestDataGenerator.BuildSingleFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        var charA = model.Fonts.Fonts[0].CharacterImages['A'];
        Assert.Equal(8, charA.Data.Width);  // default bytesPerRow * 8
        Assert.Equal(4, charA.Data.Height); // default charHeight
    }

    #endregion

    #region Two fonts

    [Fact]
    public void Parse_TwoFonts_ReturnsBothFonts()
    {
        var data = FontsTestDataGenerator.BuildTwoFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        Assert.Equal(2, model.Fonts.Fonts.Count);
        Assert.Equal(2, model.Fonts.Data.Fonts.Count);
    }

    [Fact]
    public void Parse_TwoFonts_Font0HasCorrectCharacters()
    {
        var data = FontsTestDataGenerator.BuildTwoFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        var font0 = model.Fonts.Fonts[0];
        Assert.True(font0.CharacterImages.ContainsKey('A'));
        Assert.True(font0.CharacterImages.ContainsKey('B'));
        Assert.False(font0.CharacterImages.ContainsKey('C'));
    }

    [Fact]
    public void Parse_TwoFonts_Font1HasCorrectCharacters()
    {
        var data = FontsTestDataGenerator.BuildTwoFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        var font1 = model.Fonts.Fonts[1];
        Assert.True(font1.CharacterImages.ContainsKey('X'));
        Assert.True(font1.CharacterImages.ContainsKey('Y'));
        Assert.True(font1.CharacterImages.ContainsKey('Z'));
    }

    [Fact]
    public void Parse_TwoFonts_Font0MetadataIsCorrect()
    {
        var data = FontsTestDataGenerator.BuildTwoFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        var meta0 = model.Fonts.Data.Fonts[0];
        Assert.Equal(65, meta0.FirstAsciiValue); // A
        Assert.Equal(66, meta0.LastAsciiValue);   // B
        Assert.Equal(2, meta0.CharHeight);
        Assert.Equal(1, meta0.HorizontalPadding);
        Assert.Equal(1, meta0.VerticalPadding);
        Assert.Equal(6, meta0.CharacterWidths['A']);
        Assert.Equal(7, meta0.CharacterWidths['B']);
    }

    [Fact]
    public void Parse_TwoFonts_Font1MetadataIsCorrect()
    {
        var data = FontsTestDataGenerator.BuildTwoFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        var meta1 = model.Fonts.Data.Fonts[1];
        Assert.Equal(88, meta1.FirstAsciiValue); // X
        Assert.Equal(90, meta1.LastAsciiValue);   // Z
        Assert.Equal(3, meta1.CharHeight);
        Assert.Equal(2, meta1.HorizontalPadding);
        Assert.Equal(3, meta1.VerticalPadding);
        Assert.Equal(5, meta1.CharacterWidths['X']);
        Assert.Equal(4, meta1.CharacterWidths['Y']);
        Assert.Equal(3, meta1.CharacterWidths['Z']);
    }

    [Fact]
    public void Parse_TwoFonts_Font0PixelDataIsCorrect()
    {
        var data = FontsTestDataGenerator.BuildTwoFontBinary();
        FontsTestDataGenerator.WriteFontsCv(_tempDir, data);

        var model = RunParser();

        // A: width 6, 0xFC row 0 = 11111100 -> first 6 = all 15
        var charA = model.Fonts.Fonts[0].CharacterImages['A'];
        var expectedRow0 = FontsTestDataGenerator.ByteToPixels(0xFC, 6);
        Assert.Equal(expectedRow0, charA.RawVgaImageData.Take(6).ToArray());

        // A: 0x80 row 1 = 10000000 -> first 6 = {15, 0, 0, 0, 0, 0}
        var expectedRow1 = FontsTestDataGenerator.ByteToPixels(0x80, 6);
        Assert.Equal(expectedRow1, charA.RawVgaImageData.Skip(6).Take(6).ToArray());
    }

    #endregion

    #region Edge cases

    [Fact]
    public void Parse_MissingFontsCv_Throws()
    {
        FontsTestDataGenerator.WriteFontsCv(_tempDir, FontsTestDataGenerator.BuildSingleFontBinary());
        _parser.Start(_tempDir);
        File.Delete(Path.Combine(_tempDir, "FONTS.CV"));

        Assert.Throws<Exception>(() => _parser.RunStep());
    }

    #endregion

    #region Helpers

    private PackageModel RunParser()
    {
        _parser.Start(_tempDir);
        _parser.RunStep();
        var model = new PackageModel();
        _parser.SetResult(model);
        return model;
    }

    #endregion
}
