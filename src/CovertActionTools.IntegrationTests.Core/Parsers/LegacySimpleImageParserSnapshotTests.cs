using System;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Importing.Parsers.SpriteSheets;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.IntegrationTests.Core.Parsers.Data;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class LegacySimpleImageParserSnapshotTests : IDisposable
{
    private readonly LegacySimpleImageParser _parser;
    private readonly string _tempDir;

    public LegacySimpleImageParserSnapshotTests()
    {
        var decompression = new LzwDecompression(NullLogger<LzwDecompression>.Instance);
        var imageParser = new SharedImageParser(NullLogger<SharedImageParser>.Instance, decompression);
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
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacySimpleImageSnapshotTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Export snapshot tests (binary format stability)

    [Fact]
    public void ExportSnapshot_4x4_Uniform()
    {
        var picData = SimpleImageParserTestDataGenerator.BuildPicFile(
            SharedImageTestDataGenerator.GenerateUniformPixels(4, 4), 4, 4);
        var exported = Convert.ToBase64String(picData);
        Assert.Equal(SimpleImageParserSnapshotData.PicFile_4x4_Uniform, exported);
    }

    [Fact]
    public void ExportSnapshot_4x4_Varied()
    {
        var picData = SimpleImageParserTestDataGenerator.BuildPicFile(
            SharedImageTestDataGenerator.GenerateVariedPixels(4, 4), 4, 4);
        var exported = Convert.ToBase64String(picData);
        Assert.Equal(SimpleImageParserSnapshotData.PicFile_4x4_Varied, exported);
    }

    [Fact]
    public void ExportSnapshot_16x8_Varied()
    {
        var picData = SimpleImageParserTestDataGenerator.BuildPicFile(
            SharedImageTestDataGenerator.GenerateVariedPixels(16, 8), 16, 8);
        var exported = Convert.ToBase64String(picData);
        Assert.Equal(SimpleImageParserSnapshotData.PicFile_16x8_Varied, exported);
    }

    #endregion

    #region Parse snapshot tests (parsed model properties from known binary)

    [Fact]
    public void ParseSnapshot_4x4_Uniform_HasCorrectDimensions()
    {
        WritePicFromSnapshot("TEST", SimpleImageParserSnapshotData.PicFile_4x4_Uniform);

        var images = RunParser();

        Assert.Equal(4, images["TEST"].Image.Data.Width);
        Assert.Equal(4, images["TEST"].Image.Data.Height);
    }

    [Fact]
    public void ParseSnapshot_4x4_Uniform_HasCorrectPixels()
    {
        WritePicFromSnapshot("TEST", SimpleImageParserSnapshotData.PicFile_4x4_Uniform);

        var images = RunParser();

        var expected = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        Assert.Equal(expected, images["TEST"].Image.RawVgaImageData);
    }

    [Fact]
    public void ParseSnapshot_4x4_Varied_HasCorrectPixels()
    {
        WritePicFromSnapshot("TEST", SimpleImageParserSnapshotData.PicFile_4x4_Varied);

        var images = RunParser();

        var expected = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4);
        Assert.Equal(expected, images["TEST"].Image.RawVgaImageData);
    }

    [Fact]
    public void ParseSnapshot_16x8_Varied_HasCorrectDimensions()
    {
        WritePicFromSnapshot("TEST", SimpleImageParserSnapshotData.PicFile_16x8_Varied);

        var images = RunParser();

        Assert.Equal(16, images["TEST"].Image.Data.Width);
        Assert.Equal(8, images["TEST"].Image.Data.Height);
    }

    [Fact]
    public void ParseSnapshot_16x8_Varied_HasCorrectPixels()
    {
        WritePicFromSnapshot("TEST", SimpleImageParserSnapshotData.PicFile_16x8_Varied);

        var images = RunParser();

        var expected = SharedImageTestDataGenerator.GenerateVariedPixels(16, 8);
        Assert.Equal(expected, images["TEST"].Image.RawVgaImageData);
    }

    #endregion

    #region Helpers

    private void WritePicFromSnapshot(string key, string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        File.WriteAllBytes(Path.Combine(_tempDir, $"{key}.PIC"), bytes);
    }

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
