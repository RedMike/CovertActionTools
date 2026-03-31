using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class FontsExporterImporterRoundtripTests : IDisposable
{
    private readonly FontsExporter _exporter;
    private readonly FontsImporter _importer;
    private readonly string _tempDir;

    public FontsExporterImporterRoundtripTests()
    {
        var compression = new StubLzwCompression();
        var sharedExporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, compression);
        var sharedImporter = new SharedImageImporter(NullLogger<SharedImageImporter>.Instance);
        _exporter = new FontsExporter(NullLogger<FontsExporter>.Instance, sharedExporter);
        _importer = new FontsImporter(NullLogger<SimpleImageImporter>.Instance, sharedImporter);
        _tempDir = Path.Combine(Path.GetTempPath(), $"FontsRoundtrip_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Single font roundtrips

    [Fact]
    public void Roundtrip_SingleFont_PreservesFontMetadata()
    {
        var fonts = CreateFontsModel(firstAscii: 65, lastAscii: 67, charWidth: 8, charHeight: 4,
            hPadding: 1, vPadding: 2, charHeightMeta: 4, comment: "Test font");

        var result = ExportThenImport(fonts);

        Assert.Single(result.Data.Fonts);
        Assert.Equal(65, result.Data.Fonts[0].FirstAsciiValue);
        Assert.Equal(67, result.Data.Fonts[0].LastAsciiValue);
        Assert.Equal(1, result.Data.Fonts[0].HorizontalPadding);
        Assert.Equal(2, result.Data.Fonts[0].VerticalPadding);
        Assert.Equal(4, result.Data.Fonts[0].CharHeight);
        Assert.Equal("Test font", result.Data.Fonts[0].Comment);
    }

    [Fact]
    public void Roundtrip_SingleFont_PreservesCharacterCount()
    {
        var fonts = CreateFontsModel(firstAscii: 65, lastAscii: 67, charWidth: 8, charHeight: 4);

        var result = ExportThenImport(fonts);

        Assert.Single(result.Fonts);
        Assert.Equal(3, result.Fonts[0].CharacterImages.Count);
        Assert.True(result.Fonts[0].CharacterImages.ContainsKey('A'));
        Assert.True(result.Fonts[0].CharacterImages.ContainsKey('B'));
        Assert.True(result.Fonts[0].CharacterImages.ContainsKey('C'));
    }

    [Fact]
    public void Roundtrip_SingleFont_PreservesCharacterImageDimensions()
    {
        var fonts = CreateFontsModel(firstAscii: 65, lastAscii: 65, charWidth: 6, charHeight: 10);

        var result = ExportThenImport(fonts);

        var charImage = result.Fonts[0].CharacterImages['A'];
        Assert.Equal(6, charImage.Data.Width);
        Assert.Equal(10, charImage.Data.Height);
    }

    [Fact]
    public void Roundtrip_SingleFont_PreservesRawVgaImageData()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(8, 4, seed: 100);
        var fonts = CreateFontsModel(firstAscii: 65, lastAscii: 65, charWidth: 8, charHeight: 4,
            pixelOverrides: new Dictionary<char, byte[]> { ['A'] = pixels });

        var result = ExportThenImport(fonts);

        Assert.Equal(pixels, result.Fonts[0].CharacterImages['A'].RawVgaImageData);
    }

    [Fact]
    public void Roundtrip_SingleFont_VgaTextureDataIsNonEmpty()
    {
        var fonts = CreateFontsModel(firstAscii: 65, lastAscii: 65, charWidth: 4, charHeight: 4);

        var result = ExportThenImport(fonts);

        var charImage = result.Fonts[0].CharacterImages['A'];
        Assert.NotEmpty(charImage.VgaImageData);
        Assert.Equal(4 * 4 * 4, charImage.VgaImageData.Length);
    }

    [Fact]
    public void Roundtrip_SingleFont_PreservesCharacterWidths()
    {
        var fonts = CreateFontsModel(firstAscii: 65, lastAscii: 67, charWidth: 8, charHeight: 4);

        var result = ExportThenImport(fonts);

        Assert.Equal(3, result.Data.Fonts[0].CharacterWidths.Count);
        Assert.Equal(8, result.Data.Fonts[0].CharacterWidths['A']);
        Assert.Equal(8, result.Data.Fonts[0].CharacterWidths['B']);
        Assert.Equal(8, result.Data.Fonts[0].CharacterWidths['C']);
    }

    #endregion

    #region Multiple character roundtrips

    [Fact]
    public void Roundtrip_MultipleCharacters_AllPixelsPreserved()
    {
        var pixelsA = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4, seed: 10);
        var pixelsB = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4, seed: 20);
        var pixelsC = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4, seed: 30);

        var fonts = CreateFontsModel(firstAscii: 65, lastAscii: 67, charWidth: 4, charHeight: 4,
            pixelOverrides: new Dictionary<char, byte[]>
            {
                ['A'] = pixelsA,
                ['B'] = pixelsB,
                ['C'] = pixelsC
            });

        var result = ExportThenImport(fonts);

        Assert.Equal(pixelsA, result.Fonts[0].CharacterImages['A'].RawVgaImageData);
        Assert.Equal(pixelsB, result.Fonts[0].CharacterImages['B'].RawVgaImageData);
        Assert.Equal(pixelsC, result.Fonts[0].CharacterImages['C'].RawVgaImageData);
    }

    #endregion

    #region CheckIfValid after export

    [Fact]
    public void Roundtrip_AfterExport_ImporterConsidersPathValid()
    {
        var fonts = CreateFontsModel(firstAscii: 65, lastAscii: 67, charWidth: 8, charHeight: 4);
        var model = new PackageModel { Fonts = fonts };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        Assert.True(_importer.CheckIfValid(_tempDir));
    }

    #endregion

    #region Helpers

    private FontsModel ExportThenImport(FontsModel fonts)
    {
        var model = new PackageModel { Fonts = fonts };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        _importer.Start(_tempDir);
        _importer.RunStep();

        var resultModel = new PackageModel();
        _importer.SetResult(resultModel);

        return resultModel.Fonts;
    }

    private FontsModel CreateFontsModel(byte firstAscii, byte lastAscii, int charWidth, int charHeight,
        byte hPadding = 1, byte vPadding = 2, byte charHeightMeta = 0, string comment = "",
        Dictionary<char, byte[]> pixelOverrides = null)
    {
        if (charHeightMeta == 0) charHeightMeta = (byte)charHeight;

        var characterImages = new Dictionary<char, SharedImageModel>();
        var characterWidths = new Dictionary<char, int>();

        for (var ascii = firstAscii; ascii <= lastAscii; ascii++)
        {
            var c = (char)ascii;
            byte[] pixels;
            if (pixelOverrides != null && pixelOverrides.ContainsKey(c))
            {
                pixels = pixelOverrides[c];
            }
            else
            {
                pixels = SharedImageTestDataGenerator.GenerateUniformPixels(charWidth, charHeight, (byte)(ascii % 16));
            }

            characterImages[c] = new SharedImageModel
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
            characterWidths[c] = charWidth;
        }

        return new FontsModel
        {
            Data = new FontsModel.FontData
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
            },
            Fonts = new List<FontsModel.Font>
            {
                new FontsModel.Font { CharacterImages = characterImages }
            }
        };
    }

    #endregion
}
