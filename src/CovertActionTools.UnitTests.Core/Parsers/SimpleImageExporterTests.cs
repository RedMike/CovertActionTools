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

public class SimpleImageExporterTests : IDisposable
{
    private readonly SimpleImageExporter _exporter;
    private readonly string _tempDir;

    public SimpleImageExporterTests()
    {
        var stubCompression = new StubLzwCompression();
        var sharedExporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, stubCompression);
        _exporter = new SimpleImageExporter(NullLogger<SimpleImageExporter>.Instance, sharedExporter);
        _tempDir = Path.Combine(Path.GetTempPath(), $"SimpleImageExporterTests_{Guid.NewGuid():N}");
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
        Assert.Equal("Processing simple images..", _exporter.GetMessage());
    }

    #endregion

    #region Export writes correct files

    [Fact]
    public void Export_SingleImage_WritesImageJsonFile()
    {
        var model = CreatePackageWithSingleImage("testimg", 4, 4);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "image", "testimg_image.json");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Export_SingleImage_WritesMetadataJsonFile()
    {
        var model = CreatePackageWithSingleImage("testimg", 4, 4);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "image", "testimg_metadata.json");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Export_SingleImage_WritesVgaPngFile()
    {
        var model = CreatePackageWithSingleImage("testimg", 4, 4);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "image", "testimg_VGA.png");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Export_SingleImage_ImageDataDeserializesCorrectly()
    {
        var model = CreatePackageWithSingleImage("testimg", 8, 4);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "image", "testimg_image.json"));
        var imageData = JsonSerializer.Deserialize<SharedImageModel.ImageData>(json);

        Assert.NotNull(imageData);
        Assert.Equal(8, imageData.Width);
        Assert.Equal(4, imageData.Height);
        Assert.Equal(11, imageData.CompressionDictionaryWidth);
    }

    [Fact]
    public void Export_SingleImage_MetadataDeserializesCorrectly()
    {
        var model = CreatePackageWithSingleImage("testimg", 4, 4, name: "TestName", comment: "TestComment");

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "image", "testimg_metadata.json"));
        var metadata = JsonSerializer.Deserialize<SharedMetadata>(json);

        Assert.NotNull(metadata);
        Assert.Equal("TestName", metadata.Name);
        Assert.Equal("TestComment", metadata.Comment);
    }

    #endregion

    #region SpriteSheet export

    [Fact]
    public void Export_ImageWithSpriteSheet_WritesSpritesJsonFile()
    {
        var model = CreatePackageWithSingleImage("testimg", 4, 4);
        model.SimpleImages["testimg"].SpriteSheet = new SimpleImageModel.SpriteSheetData
        {
            Sprites = new Dictionary<string, SimpleImageModel.Sprite>
            {
                ["head"] = new SimpleImageModel.Sprite { X = 0, Y = 0, Width = 2, Height = 2 }
            }
        };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "image", "testimg_sprites.json");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Export_ImageWithSpriteSheet_SpritesDeserializeCorrectly()
    {
        var model = CreatePackageWithSingleImage("testimg", 8, 8);
        model.SimpleImages["testimg"].SpriteSheet = new SimpleImageModel.SpriteSheetData
        {
            Sprites = new Dictionary<string, SimpleImageModel.Sprite>
            {
                ["head"] = new SimpleImageModel.Sprite { X = 0, Y = 0, Width = 4, Height = 4 },
                ["body"] = new SimpleImageModel.Sprite { X = 4, Y = 0, Width = 4, Height = 8 }
            }
        };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "image", "testimg_sprites.json"));
        var spriteSheet = JsonSerializer.Deserialize<SimpleImageModel.SpriteSheetData>(json);

        Assert.NotNull(spriteSheet);
        Assert.Equal(2, spriteSheet.Sprites.Count);
        Assert.Equal(4, spriteSheet.Sprites["head"].Width);
        Assert.Equal(8, spriteSheet.Sprites["body"].Height);
    }

    [Fact]
    public void Export_ImageWithoutSpriteSheet_DoesNotWriteSpritesFile()
    {
        var model = CreatePackageWithSingleImage("testimg", 4, 4);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "image", "testimg_sprites.json");
        Assert.False(File.Exists(filePath));
    }

    #endregion

    #region Multiple images

    [Fact]
    public void Export_MultipleImages_WritesFilesForEachKey()
    {
        var model = new PackageModel
        {
            SimpleImages = new Dictionary<string, SimpleImageModel>
            {
                ["img1"] = CreateSimpleImage("img1", 4, 4),
                ["img2"] = CreateSimpleImage("img2", 8, 4)
            }
        };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();
        _exporter.RunStep();

        Assert.True(File.Exists(Path.Combine(_tempDir, "image", "img1_VGA.png")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "image", "img2_VGA.png")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "image", "img1_image.json")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "image", "img2_image.json")));
    }

    #endregion

    #region RunStep completion

    [Fact]
    public void RunStep_SingleImage_ReturnsTrueOnCompletion()
    {
        var model = CreatePackageWithSingleImage("testimg", 4, 4);

        _exporter.Start(_tempDir, model);
        var done = _exporter.RunStep();

        Assert.True(done);
    }

    [Fact]
    public void RunStep_TwoImages_ReturnsFalseThenTrue()
    {
        var model = new PackageModel
        {
            SimpleImages = new Dictionary<string, SimpleImageModel>
            {
                ["img1"] = CreateSimpleImage("img1", 4, 4),
                ["img2"] = CreateSimpleImage("img2", 4, 4)
            }
        };

        _exporter.Start(_tempDir, model);
        var first = _exporter.RunStep();
        var second = _exporter.RunStep();

        Assert.False(first);
        Assert.True(second);
    }

    #endregion

    #region Helpers

    private PackageModel CreatePackageWithSingleImage(string key, int width, int height,
        string name = "", string comment = "")
    {
        var image = CreateSimpleImage(key, width, height, name, comment);
        return new PackageModel
        {
            SimpleImages = new Dictionary<string, SimpleImageModel>
            {
                [key] = image
            }
        };
    }

    private SimpleImageModel CreateSimpleImage(string key, int width, int height,
        string name = "", string comment = "")
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(width, height);
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
                    CompressionDictionaryWidth = 11
                },
                RawVgaImageData = pixels,
                VgaImageData = new byte[width * height * 4],
                CgaImageData = Array.Empty<byte>()
            }
        };
    }

    #endregion
}
