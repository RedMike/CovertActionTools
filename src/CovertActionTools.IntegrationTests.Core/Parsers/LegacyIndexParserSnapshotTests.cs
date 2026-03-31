using System;
using System.IO;
using CovertActionTools.Core;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Integration tests for LegacyIndexParser using real dependencies.
/// No snapshot data is needed since the index output is deterministic and trivial.
/// </summary>
public class LegacyIndexParserSnapshotTests : IDisposable
{
    private readonly LegacyIndexParser _parser;
    private readonly string _tempDir;

    public LegacyIndexParserSnapshotTests()
    {
        _parser = new LegacyIndexParser(NullLogger<LegacyIndexParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyIndexParserIT_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Full pipeline tests

    [Fact]
    public void Parse_FullPipeline_SetsFormatVersion()
    {
        CreateCovertExe();

        var model = RunFullPipeline();

        Assert.Equal(Constants.CurrentFormatVersion, model.Index.FormatVersion);
    }

    [Fact]
    public void Parse_FullPipeline_SetsPackageVersion()
    {
        CreateCovertExe();

        var model = RunFullPipeline();

        Assert.Equal(new Version(1, 0, 0), model.Index.PackageVersion);
    }

    [Fact]
    public void Parse_FullPipeline_SetsMetadata()
    {
        CreateCovertExe();

        var model = RunFullPipeline();

        Assert.Equal("Legacy import", model.Index.Metadata.Comment);
        Assert.NotNull(model.Index.Metadata.Name);
        Assert.NotEmpty(model.Index.Metadata.Name);
    }

    [Fact]
    public void Parse_FullPipeline_RunStepReturnsTrueOnFirstCall()
    {
        CreateCovertExe();
        _parser.Start(_tempDir);

        var done = _parser.RunStep();

        Assert.True(done);
    }

    [Fact]
    public void Parse_FullPipeline_GetItemCountReflectsCompletion()
    {
        CreateCovertExe();
        _parser.Start(_tempDir);
        _parser.RunStep();

        var (current, total) = _parser.GetItemCount();

        Assert.Equal(1, current);
        Assert.Equal(1, total);
    }

    [Fact]
    public void Parse_FullPipeline_GetMessageReturnsExpectedText()
    {
        CreateCovertExe();
        _parser.Start(_tempDir);

        var message = _parser.GetMessage();

        Assert.Equal("Processing index..", message);
    }

    #endregion

    #region Helpers

    private void CreateCovertExe()
    {
        File.WriteAllBytes(Path.Combine(_tempDir, "COVERT.EXE"), Array.Empty<byte>());
    }

    private PackageModel RunFullPipeline()
    {
        _parser.Start(_tempDir);
        _parser.RunStep();
        var model = new PackageModel();
        _parser.SetResult(model);
        return model;
    }

    #endregion
}
