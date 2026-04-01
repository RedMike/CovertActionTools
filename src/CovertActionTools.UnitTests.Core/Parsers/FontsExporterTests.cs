using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Parsers.Data;
using CovertActionTools.UnitTests.Core.Parsers.Stubs;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class FontsExporterTests : IDisposable
{
    private readonly FontsExporter _exporter;
    private readonly string _tempDir;

    public FontsExporterTests()
    {
        var stubCompression = new StubLzwCompression();
        var sharedExporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, stubCompression);
        _exporter = new FontsExporter(NullLogger<FontsExporter>.Instance, sharedExporter);
        _tempDir = Path.Combine(Path.GetTempPath(), $"FontsExporterTests_{Guid.NewGuid():N}");
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
        Assert.Equal("Processing fonts..", _exporter.GetMessage());
    }

    #endregion

    #region Export writes correct files

    [Fact]
    public void Export_SingleFont_WritesFontsJsonFile()
    {
        var model = CreatePackageWithSingleFont();

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "font", "FONTS.json");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Export_SingleFont_WritesCharacterPngFiles()
    {
        var model = CreatePackageWithSingleFont();

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        // Characters A(65), B(66), C(67)
        Assert.True(File.Exists(Path.Combine(_tempDir, "font", "FONTS_0_65_VGA.png")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "font", "FONTS_0_66_VGA.png")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "font", "FONTS_0_67_VGA.png")));
    }

    [Fact]
    public void Export_SingleFont_WritesCharacterMetadataFiles()
    {
        var model = CreatePackageWithSingleFont();

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        Assert.True(File.Exists(Path.Combine(_tempDir, "font", "FONTS_0_65_VGA_metadata.json")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "font", "FONTS_0_66_VGA_metadata.json")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "font", "FONTS_0_67_VGA_metadata.json")));
    }

    [Fact]
    public void Export_SingleFont_FontsJsonDeserializesCorrectly()
    {
        var model = CreatePackageWithSingleFont();

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "font", "FONTS.json"));
        var fontData = JsonSerializer.Deserialize<FontsModel.FontData>(json);

        Assert.NotNull(fontData);
        Assert.Single(fontData.Fonts);
        Assert.True(fontData.Fonts.ContainsKey(0));
        Assert.Equal(65, fontData.Fonts[0].FirstAsciiValue);
        Assert.Equal(67, fontData.Fonts[0].LastAsciiValue);
    }

    [Fact]
    public void Export_SingleFont_CharacterMetadataHasCorrectDimensions()
    {
        var model = CreatePackageWithSingleFont();

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "font", "FONTS_0_65_VGA_metadata.json"));
        var imageData = JsonSerializer.Deserialize<SharedImageModel.ImageData>(json);

        Assert.NotNull(imageData);
        Assert.Equal(8, imageData.Width);
        Assert.Equal(4, imageData.Height);
    }

    #endregion

    #region Empty fonts

    [Fact]
    public void Export_EmptyFonts_DoesNotWriteFiles()
    {
        var model = new PackageModel
        {
            Fonts = new FontsModel()
        };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var fontDir = Path.Combine(_tempDir, "font");
        Assert.False(Directory.Exists(fontDir));
    }

    #endregion

    #region RunStep completion

    [Fact]
    public void RunStep_ReturnsTrueOnCompletion()
    {
        var model = CreatePackageWithSingleFont();

        _exporter.Start(_tempDir, model);
        var done = _exporter.RunStep();

        Assert.True(done);
    }

    #endregion

    #region Helpers

    private PackageModel CreatePackageWithSingleFont()
    {
        var fontsModel = CreateFontsModel(
            firstAscii: 65, lastAscii: 67,
            charWidth: 8, charHeight: 4);

        return new PackageModel { Fonts = fontsModel };
    }

    private FontsModel CreateFontsModel(byte firstAscii, byte lastAscii, int charWidth, int charHeight)
    {
        var characterImages = new Dictionary<char, SharedImageModel>();
        var characterWidths = new Dictionary<char, int>();

        for (var ascii = firstAscii; ascii <= lastAscii; ascii++)
        {
            var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(charWidth, charHeight, (byte)(ascii % 16));
            characterImages[(char)ascii] = new SharedImageModel
            {
                Data = new SharedImageModel.ImageData
                {
                    Width = charWidth,
                    Height = charHeight,
                    CompressionDictionaryWidth = 11
                },
                RawVgaImageData = pixels,
                VgaImageData = new byte[charWidth * charHeight * 4],
                CgaImageData = Array.Empty<byte>()
            };
            characterWidths[(char)ascii] = charWidth;
        }

        return new FontsModel
        {
            Data = new FontsModel.FontData
            {
                Fonts = new Dictionary<int, FontsModel.FontMetadata>
                {
                    [0] = new FontsModel.FontMetadata
                    {
                        Comment = "Test font",
                        FirstAsciiValue = firstAscii,
                        LastAsciiValue = lastAscii,
                        HorizontalPadding = 1,
                        VerticalPadding = 2,
                        CharHeight = (byte)charHeight,
                        CharacterWidths = characterWidths
                    }
                }
            },
            Fonts = new System.Collections.Generic.List<FontsModel.Font>
            {
                new FontsModel.Font { CharacterImages = characterImages }
            }
        };
    }

    #endregion
}
