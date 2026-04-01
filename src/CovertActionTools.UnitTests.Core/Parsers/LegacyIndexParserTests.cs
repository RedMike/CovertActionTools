using System;
using System.IO;
using CovertActionTools.Core;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Parsers.Data;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class LegacyIndexParserTests : IDisposable
{
    private readonly LegacyIndexParser _parser;
    private readonly string _tempDir;

    public LegacyIndexParserTests()
    {
        _parser = new LegacyIndexParser(NullLogger<LegacyIndexParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyIndexParserTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Validation

    [Fact]
    public void CheckIfValid_WithCovertExe_ReturnsTrue()
    {
        IndexTestDataGenerator.CreateCovertExe(_tempDir);

        var result = _parser.CheckIfValid(_tempDir);

        Assert.True(result);
    }

    [Fact]
    public void CheckIfValid_WithoutCovertExe_ReturnsFalse()
    {
        var result = _parser.CheckIfValid(_tempDir);

        Assert.False(result);
    }

    #endregion

    #region Parsing

    [Fact]
    public void Parse_SetsFormatVersionToCurrentVersion()
    {
        IndexTestDataGenerator.CreateCovertExe(_tempDir);

        var model = RunParser();

        Assert.Equal(Constants.CurrentFormatVersion, model.Index.FormatVersion);
    }

    [Fact]
    public void Parse_SetsPackageVersionTo1_0_0()
    {
        IndexTestDataGenerator.CreateCovertExe(_tempDir);

        var model = RunParser();

        Assert.Equal(new Version(1, 0, 0), model.Index.PackageVersion);
    }

    [Fact]
    public void Parse_SetsMetadataCommentToLegacyImport()
    {
        IndexTestDataGenerator.CreateCovertExe(_tempDir);

        var model = RunParser();

        Assert.Equal("Legacy import", model.Index.Metadata.Comment);
    }

    [Fact]
    public void Parse_SetsMetadataNameContainingImport()
    {
        IndexTestDataGenerator.CreateCovertExe(_tempDir);

        var model = RunParser();

        Assert.Contains("Import", model.Index.Metadata.Name);
    }

    [Fact]
    public void Parse_MissingCovertExe_Throws()
    {
        // Create a dummy file so CheckIfValid passes but the inner Parse throws
        // Actually the parser calls Directory.GetFiles in GetTotalItemCountInPath
        // which returns 0, so RunStep would set _currentItem = 1, _done = true immediately
        // But RunImportStepInternal calls Parse which checks File.Exists
        // We need the file to exist for Start/GetTotalItemCountInPath, then remove it
        IndexTestDataGenerator.CreateCovertExe(_tempDir);
        _parser.Start(_tempDir);
        File.Delete(Path.Combine(_tempDir, "COVERT.EXE"));

        Assert.Throws<Exception>(() => _parser.RunStep());
    }

    #endregion

    #region Helpers

    private PackageModel RunParser()
    {
        _parser.Start(_tempDir);
        _parser.RunStep();
        var model = new PackageModel();
        _parser.SetResult(model);
        return model;
    }

    #endregion
}
