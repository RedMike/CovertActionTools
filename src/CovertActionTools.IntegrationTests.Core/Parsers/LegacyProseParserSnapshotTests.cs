using System;
using System.IO;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class LegacyProseParserSnapshotTests : IDisposable
{
    private readonly LegacyProseParser _parser;
    private readonly string _tempDir;

    public LegacyProseParserSnapshotTests()
    {
        _parser = new LegacyProseParser(NullLogger<LegacyProseParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyProseParserSnapshotTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Snapshot data stability

    [Fact]
    public void Snapshot_MixedProse_MatchesExpectedBase64()
    {
        var advice = ProseTestDataGenerator.BuildProseEntry("advice1", "\r\nBe careful out there\r\n");
        var lounge = ProseTestDataGenerator.BuildProseEntry("lounge", "\r\nYou enter the lounge\r\n");
        var surpriseL = ProseTestDataGenerator.BuildProseEntry("surpriseL", "\r\nYou lost the ambush\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { advice, lounge, surpriseL, end });
        var base64 = Convert.ToBase64String(fileData);
        Assert.Equal(ProseSnapshotData.MixedProse, base64);
    }

    #endregion

    #region Parse snapshot tests

    [Fact]
    public void ParseSnapshot_MixedProse_HasCorrectEntryCount()
    {
        var package = ParseFromSnapshot(ProseSnapshotData.MixedProse);
        Assert.Equal(3, package.Prose.Count);
    }

    [Fact]
    public void ParseSnapshot_MixedProse_Advice_HasCorrectProperties()
    {
        var package = ParseFromSnapshot(ProseSnapshotData.MixedProse);

        var prose = package.Prose["advice1"];
        Assert.Equal(ProseModel.ProseType.Advice, prose.Type);
        Assert.Equal("1", prose.SecondaryId);
        Assert.Equal("Be careful out there", prose.Message);
    }

    [Fact]
    public void ParseSnapshot_MixedProse_Lounge_HasCorrectProperties()
    {
        var package = ParseFromSnapshot(ProseSnapshotData.MixedProse);

        var prose = package.Prose["lounge"];
        Assert.Equal(ProseModel.ProseType.Lounge, prose.Type);
        Assert.Equal(string.Empty, prose.SecondaryId);
        Assert.Equal("You enter the lounge", prose.Message);
    }

    [Fact]
    public void ParseSnapshot_MixedProse_SurpriseL_HasCorrectProperties()
    {
        var package = ParseFromSnapshot(ProseSnapshotData.MixedProse);

        var prose = package.Prose["surpriseL"];
        Assert.Equal(ProseModel.ProseType.AmbushedLost, prose.Type);
        Assert.Equal("L", prose.SecondaryId);
        Assert.Equal("You lost the ambush", prose.Message);
    }

    #endregion

    #region Helpers

    private PackageModel ParseFromSnapshot(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var filePath = Path.Combine(_tempDir, "PROSE.DTA");
        File.WriteAllBytes(filePath, bytes);

        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        var package = new PackageModel();
        _parser.SetResult(package);
        return package;
    }

    #endregion
}
