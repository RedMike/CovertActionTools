using System;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class LegacyAnimationParserTests : IDisposable
{
    private readonly LegacyAnimationParser _parser;
    private readonly StubLzwDecompression _stubDecompression;
    private readonly string _tempDir;

    public LegacyAnimationParserTests()
    {
        _stubDecompression = new StubLzwDecompression();
        var imageParser = new SharedImageParser(NullLogger<SharedImageParser>.Instance, _stubDecompression);
        _parser = new LegacyAnimationParser(NullLogger<LegacyAnimationParser>.Instance, imageParser);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyAnimationParserTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Header parsing

    [Fact]
    public void Parse_MinimalPanFile_ReadsBoundingDimensions()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile(boundingWidth: 159, boundingHeight: 99);
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(159, animations["TEST"].Data.BoundingWidth);
        Assert.Equal(99, animations["TEST"].Data.BoundingHeight);
    }

    [Fact]
    public void Parse_MinimalPanFile_ReadsFrameSkip()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile(frameSkip: 3);
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(3, animations["TEST"].Data.GlobalFrameSkip);
    }

    [Fact]
    public void Parse_MinimalPanFile_ReadsBackgroundType()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(AnimationModel.BackgroundType.ClearToColor, animations["TEST"].Data.BackgroundType);
    }

    [Fact]
    public void Parse_MinimalPanFile_ReadsClearColor()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile(clearColor: 7);
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(7, animations["TEST"].Data.ClearColor);
    }

    [Fact]
    public void Parse_MinimalPanFile_ReadsColorMapping()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(15, animations["TEST"].Data.ColorMapping.Count);
        for (byte i = 1; i <= 15; i++)
        {
            Assert.Equal(i, animations["TEST"].Data.ColorMapping[i]);
        }
    }

    #endregion

    #region No images

    [Fact]
    public void Parse_MinimalPanFile_HasNoImages()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Empty(animations["TEST"].Images);
    }

    [Fact]
    public void Parse_MinimalPanFile_HasEmptyImageIdMapping()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Empty(animations["TEST"].Data.ImageIdToIndex);
    }

    #endregion

    #region Instructions

    [Fact]
    public void Parse_MinimalPanFile_HasInstructions()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.NotEmpty(animations["TEST"].Control.Instructions);
    }

    [Fact]
    public void Parse_MinimalPanFile_EndsWithEndInstruction()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        var instructions = animations["TEST"].Control.Instructions;
        var lastInstruction = instructions[instructions.Count - 1];
        Assert.Equal(AnimationModel.AnimationInstruction.AnimationOpcode.End, lastInstruction.Opcode);
    }

    #endregion

    #region Metadata

    [Fact]
    public void Parse_SetsKey()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "MYANIM", data);

        var animations = RunParser();

        Assert.Equal("MYANIM", animations["MYANIM"].Key);
    }

    [Fact]
    public void Parse_SetsMetadata()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "MYANIM", data);

        var animations = RunParser();

        Assert.Equal("MYANIM", animations["MYANIM"].Metadata.Name);
        Assert.Equal("Legacy import", animations["MYANIM"].Metadata.Comment);
    }

    #endregion

    #region Multiple files

    [Fact]
    public void Parse_MultiplePanFiles_ParsesAll()
    {
        AnimationTestDataGenerator.WritePanFile(_tempDir, "ANIM1",
            AnimationTestDataGenerator.BuildMinimalPanFile());
        AnimationTestDataGenerator.WritePanFile(_tempDir, "ANIM2",
            AnimationTestDataGenerator.BuildMinimalPanFile());

        var animations = RunParser();

        Assert.Equal(2, animations.Count);
        Assert.True(animations.ContainsKey("ANIM1"));
        Assert.True(animations.ContainsKey("ANIM2"));
    }

    #endregion

    #region SetResult

    [Fact]
    public void SetResult_PopulatesAnimationsOnPackageModel()
    {
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST",
            AnimationTestDataGenerator.BuildMinimalPanFile());

        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);

        Assert.NotEmpty(model.Animations);
        Assert.True(model.Animations.ContainsKey("TEST"));
    }

    #endregion

    #region Helpers

    private System.Collections.Generic.Dictionary<string, AnimationModel> RunParser()
    {
        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);
        return model.Animations;
    }

    #endregion

    private class StubLzwDecompression : ILzwDecompression
    {
        private byte[] _result = Array.Empty<byte>();

        public void SetResult(byte[] data)
        {
            _result = data;
        }

        public DecompressionResult Decompress(int width, int height, int maxWordWidth, BinaryReader reader,
            bool collectMetrics = false)
        {
            return new DecompressionResult(_result, 0);
        }
    }
}
