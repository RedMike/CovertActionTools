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

public class SimpleImageImporterTests : IDisposable
{
    private readonly SimpleImageImporter _importer;
    private readonly string _tempDir;

    public SimpleImageImporterTests()
    {
        var sharedImporter = new SharedImageImporter(NullLogger<SharedImageImporter>.Instance);
        _importer = new SimpleImageImporter(NullLogger<SimpleImageImporter>.Instance, sharedImporter);
        _tempDir = Path.Combine(Path.GetTempPath(), $"SimpleImageImporterTests_{Guid.NewGuid():N}");
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
        Assert.Equal("Processing simple images..", _importer.GetMessage());
    }

    #endregion

    #region CheckIfValid

    [Fact]
    public void CheckIfValid_ImageFilesExist_ReturnsTrue()
    {
        var imageDir = Path.Combine(_tempDir, "image");
        Directory.CreateDirectory(imageDir);
        WriteImageFiles(imageDir, "testimg", 4, 4);

        Assert.True(_importer.CheckIfValid(_tempDir));
    }

    [Fact]
    public void CheckIfValid_NoImageFiles_ReturnsFalse()
    {
        var imageDir = Path.Combine(_tempDir, "image");
        Directory.CreateDirectory(imageDir);

        Assert.False(_importer.CheckIfValid(_tempDir));
    }

    #endregion

    #region Import deserializes correctly

    [Fact]
    public void Import_SingleImage_SetsKeyCorrectly()
    {
        var imageDir = Path.Combine(_tempDir, "image");
        Directory.CreateDirectory(imageDir);
        WriteImageFiles(imageDir, "myimage", 4, 4);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.Single(model.SimpleImages);
        Assert.True(model.SimpleImages.ContainsKey("myimage"));
        Assert.Equal("myimage", model.SimpleImages["myimage"].Key);
    }

    [Fact]
    public void Import_SingleImage_ReadsMetadata()
    {
        var imageDir = Path.Combine(_tempDir, "image");
        Directory.CreateDirectory(imageDir);
        WriteImageFiles(imageDir, "testimg", 4, 4, name: "TestName", comment: "TestComment");

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.Equal("TestName", model.SimpleImages["testimg"].Metadata.Name);
        Assert.Equal("TestComment", model.SimpleImages["testimg"].Metadata.Comment);
    }

    [Fact]
    public void Import_SingleImage_ReadsImageDimensions()
    {
        var imageDir = Path.Combine(_tempDir, "image");
        Directory.CreateDirectory(imageDir);
        WriteImageFiles(imageDir, "testimg", 8, 4);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.Equal(8, model.SimpleImages["testimg"].Image.Data.Width);
        Assert.Equal(4, model.SimpleImages["testimg"].Image.Data.Height);
    }

    [Fact]
    public void Import_SingleImage_RawVgaImageDataMatchesOriginal()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4);
        var imageDir = Path.Combine(_tempDir, "image");
        Directory.CreateDirectory(imageDir);
        WriteImageFiles(imageDir, "testimg", 4, 4, pixels: pixels);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.Equal(pixels, model.SimpleImages["testimg"].Image.RawVgaImageData);
    }

    [Fact]
    public void Import_ImageWithSpriteSheet_ReadsSpriteData()
    {
        var imageDir = Path.Combine(_tempDir, "image");
        Directory.CreateDirectory(imageDir);
        WriteImageFiles(imageDir, "testimg", 8, 8);

        var spriteSheet = new SimpleImageModel.SpriteSheetData
        {
            Sprites = new Dictionary<string, SimpleImageModel.Sprite>
            {
                ["head"] = new SimpleImageModel.Sprite { X = 0, Y = 0, Width = 4, Height = 4 },
                ["body"] = new SimpleImageModel.Sprite { X = 4, Y = 0, Width = 4, Height = 8 }
            }
        };
        var spritesJson = JsonSerializer.Serialize(spriteSheet, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(imageDir, "testimg_sprites.json"), spritesJson);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.NotNull(model.SimpleImages["testimg"].SpriteSheet);
        Assert.Equal(2, model.SimpleImages["testimg"].SpriteSheet.Sprites.Count);
        Assert.Equal(4, model.SimpleImages["testimg"].SpriteSheet.Sprites["head"].Width);
    }

    [Fact]
    public void Import_ImageWithoutSpriteSheet_SpriteSheetIsNull()
    {
        var imageDir = Path.Combine(_tempDir, "image");
        Directory.CreateDirectory(imageDir);
        WriteImageFiles(imageDir, "testimg", 4, 4);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.Null(model.SimpleImages["testimg"].SpriteSheet);
    }

    #endregion

    #region Multiple images

    [Fact]
    public void Import_MultipleImages_AllPresent()
    {
        var imageDir = Path.Combine(_tempDir, "image");
        Directory.CreateDirectory(imageDir);
        WriteImageFiles(imageDir, "img1", 4, 4);
        WriteImageFiles(imageDir, "img2", 8, 4);

        _importer.Start(_tempDir);
        _importer.RunStep();
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.Equal(2, model.SimpleImages.Count);
        Assert.True(model.SimpleImages.ContainsKey("img1"));
        Assert.True(model.SimpleImages.ContainsKey("img2"));
    }

    #endregion

    #region SetResult populates correct field

    [Fact]
    public void SetResult_PopulatesSimpleImagesField()
    {
        var imageDir = Path.Combine(_tempDir, "image");
        Directory.CreateDirectory(imageDir);
        WriteImageFiles(imageDir, "testimg", 4, 4);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.NotNull(model.SimpleImages);
        Assert.Single(model.SimpleImages);
        Assert.Empty(model.Crimes);
    }

    #endregion

    #region Helpers

    private void WriteImageFiles(string imageDir, string key, int width, int height,
        string name = "", string comment = "", byte[] pixels = null)
    {
        if (pixels == null)
        {
            pixels = SharedImageTestDataGenerator.GenerateUniformPixels(width, height);
        }

        var imageData = new SharedImageModel.ImageData
        {
            Width = width,
            Height = height,
            CompressionDictionaryWidth = 11
        };
        var imageJson = JsonSerializer.Serialize(imageData, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(imageDir, $"{key}_image.json"), imageJson);

        var metadata = new SharedMetadata { Name = name, Comment = comment };
        var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(imageDir, $"{key}_metadata.json"), metadataJson);

        var pngBytes = ImageConversion.VgaToTexture(width, height, pixels);
        File.WriteAllBytes(Path.Combine(imageDir, $"{key}_VGA.png"), pngBytes);
    }

    #endregion
}
