using System;
using System.IO;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.IntegrationTests.Core.Parsers.Data;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class LegacyWorldParserSnapshotTests : IDisposable
{
    private readonly LegacyWorldParser _parser;
    private readonly string _tempDir;

    public LegacyWorldParserSnapshotTests()
    {
        _parser = new LegacyWorldParser(NullLogger<LegacyWorldParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyWorldSnapshotTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Binary format stability (snapshot matches known data)

    [Fact]
    public void Snapshot_StandardWorldFile_MatchesExpectedBinary()
    {
        var generated = WorldTestDataGenerator.BuildStandardWorldFile();
        var actual = Convert.ToBase64String(generated);
        Assert.Equal(WorldSnapshotData.StandardWorldFile, actual);
    }

    #endregion

    #region Parse snapshot tests (parsed model properties from known binary)

    [Fact]
    public void ParseSnapshot_StandardWorld_HasThreeCities()
    {
        var world = ParseFromSnapshot(WorldSnapshotData.StandardWorldFile);

        Assert.Equal(3, world.Cities.Count);
    }

    [Fact]
    public void ParseSnapshot_StandardWorld_LondonCity()
    {
        var world = ParseFromSnapshot(WorldSnapshotData.StandardWorldFile);

        var london = world.Cities[0];
        Assert.Equal("London", london.Name);
        Assert.Equal("England", london.Country);
        Assert.Equal(128, london.MapX);
        Assert.Equal(45, london.MapY);
        Assert.Equal(0x0100, london.Unknown1);
        Assert.Equal(0x0200, london.Unknown2);
    }

    [Fact]
    public void ParseSnapshot_StandardWorld_ParisCity()
    {
        var world = ParseFromSnapshot(WorldSnapshotData.StandardWorldFile);

        var paris = world.Cities[1];
        Assert.Equal("Paris", paris.Name);
        Assert.Equal("France", paris.Country);
        Assert.Equal(132, paris.MapX);
        Assert.Equal(50, paris.MapY);
    }

    [Fact]
    public void ParseSnapshot_StandardWorld_BerlinCity()
    {
        var world = ParseFromSnapshot(WorldSnapshotData.StandardWorldFile);

        var berlin = world.Cities[2];
        Assert.Equal("Berlin", berlin.Name);
        Assert.Equal("Germany", berlin.Country);
        Assert.Equal(140, berlin.MapX);
        Assert.Equal(48, berlin.MapY);
        Assert.Equal(0x0300, berlin.Unknown1);
        Assert.Equal(0x0400, berlin.Unknown2);
    }

    [Fact]
    public void ParseSnapshot_StandardWorld_HasTwoOrganisations()
    {
        var world = ParseFromSnapshot(WorldSnapshotData.StandardWorldFile);

        Assert.Equal(2, world.Organisations.Count);
    }

    [Fact]
    public void ParseSnapshot_StandardWorld_MI6Organisation()
    {
        var world = ParseFromSnapshot(WorldSnapshotData.StandardWorldFile);

        var mi6 = world.Organisations[0];
        Assert.Equal("MI6", mi6.ShortName);
        Assert.Equal("Secret Intel Svc", mi6.LongName);
        Assert.Equal(10, mi6.UniqueId);
        Assert.Equal(0x0100, mi6.Unknown1);
        Assert.True(mi6.AllowMastermind);
    }

    [Fact]
    public void ParseSnapshot_StandardWorld_KGBOrganisation()
    {
        var world = ParseFromSnapshot(WorldSnapshotData.StandardWorldFile);

        var kgb = world.Organisations[1];
        Assert.Equal("KGB", kgb.ShortName);
        Assert.Equal("Committee State", kgb.LongName);
        Assert.Equal(20, kgb.UniqueId);
        Assert.Equal(0x0500, kgb.Unknown2);
        Assert.True(kgb.AllowMastermind);
    }

    [Fact]
    public void ParseSnapshot_StandardWorld_HasCorrectMetadata()
    {
        var world = ParseFromSnapshot(WorldSnapshotData.StandardWorldFile);

        Assert.Equal(0, world.Id);
        Assert.Equal("WORLD0", world.Metadata.Name);
        Assert.Equal("Legacy import", world.Metadata.Comment);
    }

    #endregion

    #region Helpers

    private WorldModel ParseFromSnapshot(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var filePath = Path.Combine(_tempDir, "WORLD0.DTA");
        File.WriteAllBytes(filePath, bytes);

        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);
        return model.Worlds[0];
    }

    #endregion
}
