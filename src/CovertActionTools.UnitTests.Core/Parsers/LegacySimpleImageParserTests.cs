using System;
using System.IO;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Importing.Parsers.SpriteSheets;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Parsers.Data;
using CovertActionTools.UnitTests.Core.Parsers.Stubs;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class LegacySimpleImageParserTests : IDisposable
{
    private readonly LegacySimpleImageParser _parser;
    private readonly StubLzwDecompression _stubDecompression;
    private readonly string _tempDir;

    public LegacySimpleImageParserTests()
    {
        _stubDecompression = new StubLzwDecompression();
        var imageParser = new SharedImageParser(NullLogger<SharedImageParser>.Instance, _stubDecompression);
        var spriteSheetContainer = new LegacySpriteSheetContainer(new BaseLegacySpriteSheetData[]
        {
            new LegacyCameraSpriteSheetData(),
            new LegacyEquip1SpriteSheetData(),
            new LegacyEquip2SpriteSheetData(),
            new LegacyFacesSpriteSheetData(),
            new LegacyMapTilesSpriteSheetData(),
            new LegacySpritesSpriteSheetData(),
        });
        _parser = new LegacySimpleImageParser(NullLogger<LegacySimpleImageParser>.Instance, imageParser, spriteSheetContainer);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacySimpleImageParserTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Image dimensions and data

    [Fact]
    public void Parse_SinglePicFile_ReadsWidthAndHeight()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(8, 4);
        _stubDecompression.SetResult(pixels);
        var data = SimpleImageParserTestDataGenerator.BuildPicFile(8, 4);
        SimpleImageParserTestDataGenerator.WritePicFile(_tempDir, "TEST", data);

        var images = RunParser();

        Assert.Equal(8, images["TEST"].Image.Data.Width);
        Assert.Equal(4, images["TEST"].Image.Data.Height);
    }

    [Fact]
    public void Parse_SinglePicFile_PreservesPixelData()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = SimpleImageParserTestDataGenerator.BuildPicFile(4, 4);
        SimpleImageParserTestDataGenerator.WritePicFile(_tempDir, "TEST", data);

        var images = RunParser();

        Assert.Equal(pixels, images["TEST"].Image.RawVgaImageData);
    }

    [Fact]
    public void Parse_SinglePicFile_SetsKeyFromFilename()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = SimpleImageParserTestDataGenerator.BuildPicFile(4, 4);
        SimpleImageParserTestDataGenerator.WritePicFile(_tempDir, "MYIMAGE", data);

        var images = RunParser();

        Assert.True(images.ContainsKey("MYIMAGE"));
        Assert.Equal("MYIMAGE", images["MYIMAGE"].Key);
    }

    #endregion

    #region Metadata

    [Fact]
    public void Parse_SetsMetadataName()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = SimpleImageParserTestDataGenerator.BuildPicFile(4, 4);
        SimpleImageParserTestDataGenerator.WritePicFile(_tempDir, "HELLO", data);

        var images = RunParser();

        Assert.Equal("HELLO", images["HELLO"].Metadata.Name);
        Assert.Equal("Legacy importer", images["HELLO"].Metadata.Comment);
    }

    #endregion

    #region Sprite sheet lookup

    [Fact]
    public void Parse_KnownSpriteSheetFile_HasSpriteSheetData()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(320, 200);
        _stubDecompression.SetResult(pixels);
        var data = SimpleImageParserTestDataGenerator.BuildPicFile(320, 200);
        SimpleImageParserTestDataGenerator.WritePicFile(_tempDir, "CAMERA", data);

        var images = RunParser();

        Assert.NotNull(images["CAMERA"].SpriteSheet);
        Assert.True(images["CAMERA"].SpriteSheet!.Sprites.ContainsKey("screen"));
    }

    [Fact]
    public void Parse_UnknownFile_HasNoSpriteSheet()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = SimpleImageParserTestDataGenerator.BuildPicFile(4, 4);
        SimpleImageParserTestDataGenerator.WritePicFile(_tempDir, "UNKNOWN", data);

        var images = RunParser();

        Assert.Null(images["UNKNOWN"].SpriteSheet);
    }

    #endregion

    #region Multiple files

    [Fact]
    public void Parse_MultiplePicFiles_ParsesAll()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        SimpleImageParserTestDataGenerator.WritePicFile(_tempDir, "IMG1",
            SimpleImageParserTestDataGenerator.BuildPicFile(4, 4));
        SimpleImageParserTestDataGenerator.WritePicFile(_tempDir, "IMG2",
            SimpleImageParserTestDataGenerator.BuildPicFile(4, 4));

        var images = RunParser();

        Assert.Equal(2, images.Count);
        Assert.True(images.ContainsKey("IMG1"));
        Assert.True(images.ContainsKey("IMG2"));
    }

    #endregion

    #region SetResult

    [Fact]
    public void SetResult_PopulatesSimpleImagesOnPackageModel()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        SimpleImageParserTestDataGenerator.WritePicFile(_tempDir, "TEST",
            SimpleImageParserTestDataGenerator.BuildPicFile(4, 4));

        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);

        Assert.NotEmpty(model.SimpleImages);
        Assert.True(model.SimpleImages.ContainsKey("TEST"));
    }

    #endregion

    #region Helpers

    private System.Collections.Generic.Dictionary<string, SimpleImageModel> RunParser()
    {
        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);
        return model.SimpleImages;
    }

    #endregion
}
