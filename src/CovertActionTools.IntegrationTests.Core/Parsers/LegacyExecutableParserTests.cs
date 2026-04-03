using System;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.IntegrationTests.Core.Parsers.Data;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class LegacyExecutableParserTests
{
    private readonly LegacyExecutableParser _parser;

    public LegacyExecutableParserTests()
    {
        var decompression = new ExepackDecompression(NullLogger<ExepackDecompression>.Instance);
        _parser = new LegacyExecutableParser(
            NullLogger<LegacyExecutableParser>.Instance, decompression);
    }

    private PackageModel TryParseFromScratch()
    {
        var scratchDir = ExecutableTestDataGenerator.FindScratchDirectory();
        if (scratchDir == null) return null;

        _parser.Start(scratchDir);
        while (!_parser.RunStep()) { }
        var model = new PackageModel();
        _parser.SetResult(model);
        return model;
    }

    #region Parse all executables

    [Fact]
    public void Parse_AllSixExecutables_ProducesModels()
    {
        var model = TryParseFromScratch();
        if (model == null) return;

        Assert.Equal(6, model.Executables.Count);
        foreach (var name in ExecutableTestDataGenerator.KnownExecutables)
        {
            Assert.True(model.Executables.ContainsKey(name), $"Missing executable: {name}");
        }
    }

    [Fact]
    public void Parse_AllExecutables_HaveNonEmptyPayload()
    {
        var model = TryParseFromScratch();
        if (model == null) return;

        foreach (var kvp in model.Executables)
        {
            Assert.True(kvp.Value.CodeSegment.Length > 0,
                $"{kvp.Key} has empty CodeSegment");
        }
    }

    [Fact]
    public void Parse_AllExecutables_HaveNonEmptyDeadZone()
    {
        var model = TryParseFromScratch();
        if (model == null) return;

        foreach (var kvp in model.Executables)
        {
            Assert.True(kvp.Value.DeadZone.Length > 0,
                $"{kvp.Key} has empty DeadZone");
        }
    }

    [Fact]
    public void Parse_AllExecutables_HaveRelocations()
    {
        var model = TryParseFromScratch();
        if (model == null) return;

        foreach (var kvp in model.Executables)
        {
            Assert.True(kvp.Value.Relocations.Length > 0,
                $"{kvp.Key} has no relocations");
            Assert.True(kvp.Value.Relocations.Length % 2 == 0,
                $"{kvp.Key} has odd number of relocation entries");
        }
    }

    [Fact]
    public void Parse_AllExecutables_HaveStub()
    {
        var model = TryParseFromScratch();
        if (model == null) return;

        foreach (var kvp in model.Executables)
        {
            Assert.True(kvp.Value.ExepackStub.Length > 0,
                $"{kvp.Key} has empty ExepackStub");
        }
    }

    [Fact]
    public void Parse_AllExecutables_HaveMzHeader()
    {
        var model = TryParseFromScratch();
        if (model == null) return;

        foreach (var kvp in model.Executables)
        {
            Assert.True(kvp.Value.OriginalMzHeader.Length >= 28,
                $"{kvp.Key} has too-small OriginalMzHeader");
            Assert.Equal((byte)'M', kvp.Value.OriginalMzHeader[0]);
            Assert.Equal((byte)'Z', kvp.Value.OriginalMzHeader[1]);
        }
    }

    #endregion

    #region Known dead zone sizes

    [Theory]
    [InlineData("CHASE", 619)]
    [InlineData("CODE", 619)]
    [InlineData("GAME", 619)]
    [InlineData("BUG", 759)]
    [InlineData("FINAL", 759)]
    [InlineData("TAC", 759)]
    public void Parse_DeadZoneSize_MatchesExpected(string name, int expectedSize)
    {
        var model = TryParseFromScratch();
        if (model == null) return;

        Assert.Equal(expectedSize, model.Executables[name].DeadZone.Length);
    }

    #endregion

    #region CheckIfValid

    [Fact]
    public void CheckIfValid_ScratchDirectory_ReturnsTrue()
    {
        var scratchDir = ExecutableTestDataGenerator.FindScratchDirectory();
        if (scratchDir == null) return;

        Assert.True(_parser.CheckIfValid(scratchDir));
    }

    [Fact]
    public void CheckIfValid_EmptyDirectory_ReturnsFalse()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ExeParserTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            Assert.False(_parser.CheckIfValid(tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    #endregion
}
