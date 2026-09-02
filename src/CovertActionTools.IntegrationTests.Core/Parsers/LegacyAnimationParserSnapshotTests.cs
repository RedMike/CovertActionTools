using System;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.IntegrationTests.Core.Parsers.Data;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class LegacyAnimationParserSnapshotTests : IDisposable
{
    private readonly LegacyAnimationParser _parser;
    private readonly string _tempDir;

    public LegacyAnimationParserSnapshotTests()
    {
        var decompression = new LzwDecompression(NullLogger<LzwDecompression>.Instance);
        var imageParser = new SharedImageParser(NullLogger<SharedImageParser>.Instance, decompression);
        _parser = new LegacyAnimationParser(NullLogger<LegacyAnimationParser>.Instance, imageParser);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyAnimationSnapshotTests_{Guid.NewGuid():N}");
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
    public void ExportSnapshot_Minimal_Default()
    {
        var panData = AnimationIntegrationTestDataGenerator.BuildMinimalPanFile();
        var exported = Convert.ToBase64String(panData);
        Assert.Equal(AnimationSnapshotData.PanFile_Minimal_Default, exported);
    }

    [Fact]
    public void ExportSnapshot_Minimal_CustomValues()
    {
        var panData = AnimationIntegrationTestDataGenerator.BuildMinimalPanFile(
            boundingWidth: 159, boundingHeight: 99, frameDelay: 3, clearColor: 7);
        var exported = Convert.ToBase64String(panData);
        Assert.Equal(AnimationSnapshotData.PanFile_Minimal_CustomValues, exported);
    }

    #endregion

    #region Parse snapshot tests (parsed model properties from known binary)

    [Fact]
    public void ParseSnapshot_Default_HasCorrectBoundingDimensions()
    {
        WritePanFromSnapshot("TEST", AnimationSnapshotData.PanFile_Minimal_Default);

        var animations = RunParser();

        Assert.Equal(99, animations["TEST"].Data.BoundingWidth);
        Assert.Equal(79, animations["TEST"].Data.BoundingHeight);
    }

    [Fact]
    public void ParseSnapshot_Default_HasCorrectBackgroundType()
    {
        WritePanFromSnapshot("TEST", AnimationSnapshotData.PanFile_Minimal_Default);

        var animations = RunParser();

        Assert.Equal(AnimationModel.BackgroundType.ClearToColor, animations["TEST"].Data.BackgroundType);
    }

    [Fact]
    public void ParseSnapshot_Default_HasNoImages()
    {
        WritePanFromSnapshot("TEST", AnimationSnapshotData.PanFile_Minimal_Default);

        var animations = RunParser();

        Assert.Empty(animations["TEST"].Images);
    }

    [Fact]
    public void ParseSnapshot_Default_HasInstructions()
    {
        WritePanFromSnapshot("TEST", AnimationSnapshotData.PanFile_Minimal_Default);

        var animations = RunParser();

        Assert.NotEmpty(animations["TEST"].Control.Instructions);
    }

    [Fact]
    public void ParseSnapshot_CustomValues_HasCorrectBoundingDimensions()
    {
        WritePanFromSnapshot("TEST", AnimationSnapshotData.PanFile_Minimal_CustomValues);

        var animations = RunParser();

        Assert.Equal(159, animations["TEST"].Data.BoundingWidth);
        Assert.Equal(99, animations["TEST"].Data.BoundingHeight);
    }

    [Fact]
    public void ParseSnapshot_CustomValues_HasCorrectFrameDelay()
    {
        WritePanFromSnapshot("TEST", AnimationSnapshotData.PanFile_Minimal_CustomValues);

        var animations = RunParser();

        Assert.Equal(3, animations["TEST"].Data.FrameDelay);
    }

    [Fact]
    public void ParseSnapshot_CustomValues_HasCorrectClearColor()
    {
        WritePanFromSnapshot("TEST", AnimationSnapshotData.PanFile_Minimal_CustomValues);

        var animations = RunParser();

        Assert.Equal(7, animations["TEST"].Data.ClearColor);
    }

    [Fact]
    public void ParseSnapshot_Default_HasCorrectColorMapping()
    {
        WritePanFromSnapshot("TEST", AnimationSnapshotData.PanFile_Minimal_Default);

        var animations = RunParser();

        Assert.Equal(16, animations["TEST"].Data.ColorMapping.Count);
        Assert.Equal(3, animations["TEST"].Data.ColorMapping[0]);
        for (byte i = 1; i <= 15; i++)
        {
            Assert.Equal(i, animations["TEST"].Data.ColorMapping[i]);
        }
    }

    #endregion

    #region Helpers

    private void WritePanFromSnapshot(string key, string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        File.WriteAllBytes(Path.Combine(_tempDir, $"{key}.PAN"), bytes);
    }

    private System.Collections.Generic.Dictionary<string, AnimationModel> RunParser()
    {
        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);
        return model.Animations;
    }

    #endregion
}
