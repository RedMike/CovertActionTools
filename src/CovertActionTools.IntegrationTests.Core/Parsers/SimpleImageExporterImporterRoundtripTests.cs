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

public class SimpleImageExporterImporterRoundtripTests : IDisposable
{
    private readonly SimpleImageExporter _exporter;
    private readonly SimpleImageImporter _importer;
    private readonly string _tempDir;

    public SimpleImageExporterImporterRoundtripTests()
    {
        var compression = new StubLzwCompression();
        var sharedExporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, compression);
        var sharedImporter = new SharedImageImporter(NullLogger<SharedImageImporter>.Instance);
        _exporter = new SimpleImageExporter(NullLogger<SimpleImageExporter>.Instance, sharedExporter);
        _importer = new SimpleImageImporter(NullLogger<SimpleImageImporter>.Instance, sharedImporter);
        _tempDir = Path.Combine(Path.GetTempPath(), $"SimpleImageRoundtrip_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Single image roundtrips

    [Fact]
    public void Roundtrip_SingleImage_PreservesKey()
    {
        var images = new Dictionary<string, SimpleImageModel>
        {
            ["myimage"] = CreateSimpleImage("myimage", 4, 4)
        };

        var result = ExportThenImport(images);

        Assert.Single(result);
        Assert.True(result.ContainsKey("myimage"));
        Assert.Equal("myimage", result["myimage"].Key);
    }

    [Fact]
    public void Roundtrip_SingleImage_PreservesMetadata()
    {
        var images = new Dictionary<string, SimpleImageModel>
        {
            ["testimg"] = CreateSimpleImage("testimg", 4, 4, name: "Test Image", comment: "A test comment")
        };

        var result = ExportThenImport(images);

        Assert.Equal("Test Image", result["testimg"].Metadata.Name);
        Assert.Equal("A test comment", result["testimg"].Metadata.Comment);
    }

    [Fact]
    public void Roundtrip_SingleImage_PreservesImageDimensions()
    {
        var images = new Dictionary<string, SimpleImageModel>
        {
            ["testimg"] = CreateSimpleImage("testimg", 16, 8)
        };

        var result = ExportThenImport(images);

        Assert.Equal(16, result["testimg"].Image.Data.Width);
        Assert.Equal(8, result["testimg"].Image.Data.Height);
    }

    [Fact]
    public void Roundtrip_SingleImage_PreservesRawVgaImageData()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(8, 4);
        var images = new Dictionary<string, SimpleImageModel>
        {
            ["testimg"] = CreateSimpleImage("testimg", 8, 4, pixels: pixels)
        };

        var result = ExportThenImport(images);

        Assert.Equal(pixels, result["testimg"].Image.RawVgaImageData);
    }

    [Fact]
    public void Roundtrip_SingleImage_PreservesDictionaryWidth()
    {
        var images = new Dictionary<string, SimpleImageModel>
        {
            ["testimg"] = CreateSimpleImage("testimg", 4, 4, dictionaryWidth: 10)
        };

        var result = ExportThenImport(images);

        Assert.Equal(10, result["testimg"].Image.Data.CompressionDictionaryWidth);
    }

    [Fact]
    public void Roundtrip_SingleImage_VgaTextureDataIsNonEmpty()
    {
        var images = new Dictionary<string, SimpleImageModel>
        {
            ["testimg"] = CreateSimpleImage("testimg", 4, 4)
        };

        var result = ExportThenImport(images);

        Assert.NotEmpty(result["testimg"].Image.VgaImageData);
        Assert.Equal(4 * 4 * 4, result["testimg"].Image.VgaImageData.Length);
    }

    #endregion

    #region SpriteSheet roundtrips

    [Fact]
    public void Roundtrip_ImageWithSpriteSheet_PreservesSprites()
    {
        var image = CreateSimpleImage("testimg", 8, 8);
        image.SpriteSheet = new SimpleImageModel.SpriteSheetData
        {
            Sprites = new Dictionary<string, SimpleImageModel.Sprite>
            {
                ["head"] = new SimpleImageModel.Sprite { X = 0, Y = 0, Width = 4, Height = 4 },
                ["body"] = new SimpleImageModel.Sprite { X = 4, Y = 0, Width = 4, Height = 8 }
            }
        };

        var images = new Dictionary<string, SimpleImageModel> { ["testimg"] = image };
        var result = ExportThenImport(images);

        Assert.NotNull(result["testimg"].SpriteSheet);
        Assert.Equal(2, result["testimg"].SpriteSheet.Sprites.Count);
        Assert.Equal(0, result["testimg"].SpriteSheet.Sprites["head"].X);
        Assert.Equal(4, result["testimg"].SpriteSheet.Sprites["head"].Width);
        Assert.Equal(4, result["testimg"].SpriteSheet.Sprites["body"].X);
        Assert.Equal(8, result["testimg"].SpriteSheet.Sprites["body"].Height);
    }

    [Fact]
    public void Roundtrip_ImageWithoutSpriteSheet_RemainsNull()
    {
        var images = new Dictionary<string, SimpleImageModel>
        {
            ["testimg"] = CreateSimpleImage("testimg", 4, 4)
        };

        var result = ExportThenImport(images);

        Assert.Null(result["testimg"].SpriteSheet);
    }

    #endregion

    #region Multiple image roundtrips

    [Fact]
    public void Roundtrip_MultipleImages_AllPreserved()
    {
        var pixels1 = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4, seed: 1);
        var pixels2 = SharedImageTestDataGenerator.GenerateVariedPixels(8, 4, seed: 2);

        var images = new Dictionary<string, SimpleImageModel>
        {
            ["img1"] = CreateSimpleImage("img1", 4, 4, pixels: pixels1),
            ["img2"] = CreateSimpleImage("img2", 8, 4, pixels: pixels2)
        };

        var result = ExportThenImport(images);

        Assert.Equal(2, result.Count);
        Assert.Equal(pixels1, result["img1"].Image.RawVgaImageData);
        Assert.Equal(pixels2, result["img2"].Image.RawVgaImageData);
    }

    #endregion

    #region CheckIfValid after export

    [Fact]
    public void Roundtrip_AfterExport_ImporterConsidersPathValid()
    {
        var images = new Dictionary<string, SimpleImageModel>
        {
            ["testimg"] = CreateSimpleImage("testimg", 4, 4)
        };
        var model = new PackageModel { SimpleImages = images };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        Assert.True(_importer.CheckIfValid(_tempDir));
    }

    #endregion

    #region Large image roundtrip

    [Fact]
    public void Roundtrip_320x200_VariedPixels_PreservesData()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(320, 200);
        var images = new Dictionary<string, SimpleImageModel>
        {
            ["largeimg"] = CreateSimpleImage("largeimg", 320, 200, pixels: pixels)
        };

        var result = ExportThenImport(images);

        Assert.Equal(pixels, result["largeimg"].Image.RawVgaImageData);
        Assert.Equal(320, result["largeimg"].Image.Data.Width);
        Assert.Equal(200, result["largeimg"].Image.Data.Height);
    }

    #endregion

    #region Helpers

    private Dictionary<string, SimpleImageModel> ExportThenImport(Dictionary<string, SimpleImageModel> images)
    {
        var model = new PackageModel { SimpleImages = images };

        _exporter.Start(_tempDir, model);
        var exportDone = false;
        while (!exportDone)
        {
            exportDone = _exporter.RunStep();
        }

        _importer.Start(_tempDir);
        var importDone = false;
        while (!importDone)
        {
            importDone = _importer.RunStep();
        }

        var resultModel = new PackageModel();
        _importer.SetResult(resultModel);

        return resultModel.SimpleImages;
    }

    private SimpleImageModel CreateSimpleImage(string key, int width, int height,
        string name = "", string comment = "", byte[] pixels = null, byte dictionaryWidth = 11)
    {
        if (pixels == null)
        {
            pixels = SharedImageTestDataGenerator.GenerateUniformPixels(width, height);
        }

        return new SimpleImageModel
        {
            Key = key,
            Metadata = new SharedMetadata { Name = name, Comment = comment },
            Image = new SharedImageModel
            {
                Data = new SharedImageModel.ImageData
                {
                    Width = width,
                    Height = height,
                    CompressionDictionaryWidth = dictionaryWidth
                },
                RawVgaImageData = pixels,
                VgaImageData = new byte[width * height * 4],
                CgaImageData = Array.Empty<byte>()
            }
        };
    }

    #endregion
}
