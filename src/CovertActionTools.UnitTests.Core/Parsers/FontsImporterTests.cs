using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class FontsImporterTests : IDisposable
{
    private readonly FontsImporter _importer;
    private readonly string _tempDir;

    public FontsImporterTests()
    {
        var sharedImporter = new SharedImageImporter(NullLogger<SharedImageImporter>.Instance);
        _importer = new FontsImporter(NullLogger<SimpleImageImporter>.Instance, sharedImporter);
        _tempDir = Path.Combine(Path.GetTempPath(), $"FontsImporterTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region GetMessage

    [Fact]
    public void GetMessage_ReturnsExpectedString()
    {
        Assert.Equal("Processing fonts..", _importer.GetMessage());
    }

    #endregion

    #region CheckIfValid

    [Fact]
    public void CheckIfValid_FontsJsonExists_ReturnsTrue()
    {
        var fontDir = Path.Combine(_tempDir, "font");
        Directory.CreateDirectory(fontDir);
        WriteFontFiles(fontDir, firstAscii: 65, lastAscii: 67, charWidth: 8, charHeight: 4);

        Assert.True(_importer.CheckIfValid(_tempDir));
    }

    [Fact]
    public void CheckIfValid_NoFontsJson_ReturnsFalse()
    {
        var fontDir = Path.Combine(_tempDir, "font");
        Directory.CreateDirectory(fontDir);

        Assert.False(_importer.CheckIfValid(_tempDir));
    }

    #endregion

    #region Import deserializes correctly

    [Fact]
    public void Import_SingleFont_ReadsFontMetadata()
    {
        var fontDir = Path.Combine(_tempDir, "font");
        Directory.CreateDirectory(fontDir);
        WriteFontFiles(fontDir, firstAscii: 65, lastAscii: 67, charWidth: 8, charHeight: 4);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.Single(model.Fonts.Data.Fonts);
        Assert.Equal(65, model.Fonts.Data.Fonts[0].FirstAsciiValue);
        Assert.Equal(67, model.Fonts.Data.Fonts[0].LastAsciiValue);
    }

    [Fact]
    public void Import_SingleFont_ReadsCharacterImages()
    {
        var fontDir = Path.Combine(_tempDir, "font");
        Directory.CreateDirectory(fontDir);
        WriteFontFiles(fontDir, firstAscii: 65, lastAscii: 67, charWidth: 8, charHeight: 4);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.Single(model.Fonts.Fonts);
        Assert.Equal(3, model.Fonts.Fonts[0].CharacterImages.Count);
        Assert.True(model.Fonts.Fonts[0].CharacterImages.ContainsKey('A'));
        Assert.True(model.Fonts.Fonts[0].CharacterImages.ContainsKey('B'));
        Assert.True(model.Fonts.Fonts[0].CharacterImages.ContainsKey('C'));
    }

    [Fact]
    public void Import_SingleFont_CharacterImageHasCorrectDimensions()
    {
        var fontDir = Path.Combine(_tempDir, "font");
        Directory.CreateDirectory(fontDir);
        WriteFontFiles(fontDir, firstAscii: 65, lastAscii: 65, charWidth: 6, charHeight: 8);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        var charImage = model.Fonts.Fonts[0].CharacterImages['A'];
        Assert.Equal(6, charImage.Data.Width);
        Assert.Equal(8, charImage.Data.Height);
    }

    [Fact]
    public void Import_SingleFont_CharacterImageHasRawVgaData()
    {
        var fontDir = Path.Combine(_tempDir, "font");
        Directory.CreateDirectory(fontDir);
        WriteFontFiles(fontDir, firstAscii: 65, lastAscii: 65, charWidth: 4, charHeight: 4);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        var charImage = model.Fonts.Fonts[0].CharacterImages['A'];
        Assert.Equal(4 * 4, charImage.RawVgaImageData.Length);
        Assert.NotEmpty(charImage.VgaImageData);
    }

    [Fact]
    public void Import_PreservesFontMetadataFields()
    {
        var fontDir = Path.Combine(_tempDir, "font");
        Directory.CreateDirectory(fontDir);
        WriteFontFiles(fontDir, firstAscii: 65, lastAscii: 66, charWidth: 8, charHeight: 4,
            hPadding: 3, vPadding: 5, charHeightMeta: 4, comment: "Test comment");

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        var meta = model.Fonts.Data.Fonts[0];
        Assert.Equal(3, meta.HorizontalPadding);
        Assert.Equal(5, meta.VerticalPadding);
        Assert.Equal(4, meta.CharHeight);
        Assert.Equal("Test comment", meta.Comment);
    }

    #endregion

    #region SetResult populates correct field

    [Fact]
    public void SetResult_PopulatesFontsField()
    {
        var fontDir = Path.Combine(_tempDir, "font");
        Directory.CreateDirectory(fontDir);
        WriteFontFiles(fontDir, firstAscii: 65, lastAscii: 65, charWidth: 4, charHeight: 4);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.NotNull(model.Fonts);
        Assert.NotEmpty(model.Fonts.Fonts);
        Assert.Empty(model.SimpleImages);
    }

    #endregion

    #region RunStep completion

    [Fact]
    public void RunStep_ReturnsTrueOnCompletion()
    {
        var fontDir = Path.Combine(_tempDir, "font");
        Directory.CreateDirectory(fontDir);
        WriteFontFiles(fontDir, firstAscii: 65, lastAscii: 67, charWidth: 8, charHeight: 4);

        _importer.Start(_tempDir);
        var done = _importer.RunStep();

        Assert.True(done);
    }

    #endregion

    #region Helpers

    private void WriteFontFiles(string fontDir, byte firstAscii, byte lastAscii,
        int charWidth, int charHeight, byte hPadding = 1, byte vPadding = 2,
        byte charHeightMeta = 0, string comment = "")
    {
        if (charHeightMeta == 0) charHeightMeta = (byte)charHeight;

        var characterWidths = new Dictionary<char, int>();
        for (var ascii = firstAscii; ascii <= lastAscii; ascii++)
        {
            characterWidths[(char)ascii] = charWidth;
        }

        var fontData = new FontsModel.FontData
        {
            Fonts = new Dictionary<int, FontsModel.FontMetadata>
            {
                [0] = new FontsModel.FontMetadata
                {
                    Comment = comment,
                    FirstAsciiValue = firstAscii,
                    LastAsciiValue = lastAscii,
                    HorizontalPadding = hPadding,
                    VerticalPadding = vPadding,
                    CharHeight = charHeightMeta,
                    CharacterWidths = characterWidths
                }
            }
        };

        var fontsJson = JsonSerializer.Serialize(fontData, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(fontDir, "FONTS.json"), fontsJson);

        for (var ascii = firstAscii; ascii <= lastAscii; ascii++)
        {
            var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(charWidth, charHeight, (byte)(ascii % 16));
            var imageData = new SharedImageModel.ImageData
            {
                Width = charWidth,
                Height = charHeight,
                CompressionDictionaryWidth = 11
            };

            var imageJson = JsonSerializer.Serialize(imageData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(fontDir, $"FONTS_0_{ascii}_VGA_metadata.json"), imageJson);

            var pngBytes = ImageConversion.VgaToTexture(charWidth, charHeight, pixels);
            File.WriteAllBytes(Path.Combine(fontDir, $"FONTS_0_{ascii}_VGA.png"), pngBytes);
        }
    }

    #endregion
}
