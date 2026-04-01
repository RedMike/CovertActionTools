using System;
using System.IO;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Parsers.Data;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class LegacyWorldParserTests : IDisposable
{
    private readonly LegacyWorldParser _parser;
    private readonly string _tempDir;

    public LegacyWorldParserTests()
    {
        _parser = new LegacyWorldParser(NullLogger<LegacyWorldParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyWorldParserTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region City parsing

    [Fact]
    public void Parse_SingleCity_ReadsName()
    {
        var data = WorldTestDataGenerator.BuildWorldFile(
            new[] { WorldTestDataGenerator.BuildCity(name: "London") },
            Array.Empty<byte[]>());
        WriteWorldFile(0, data);

        var worlds = RunParser();

        Assert.Equal("London", worlds[0].Cities[0].Name);
    }

    [Fact]
    public void Parse_SingleCity_ReadsCountry()
    {
        var data = WorldTestDataGenerator.BuildWorldFile(
            new[] { WorldTestDataGenerator.BuildCity(country: "England") },
            Array.Empty<byte[]>());
        WriteWorldFile(0, data);

        var worlds = RunParser();

        Assert.Equal("England", worlds[0].Cities[0].Country);
    }

    [Fact]
    public void Parse_SingleCity_ReadsMapCoordinates()
    {
        var data = WorldTestDataGenerator.BuildWorldFile(
            new[] { WorldTestDataGenerator.BuildCity(mapX: 150, mapY: 75) },
            Array.Empty<byte[]>());
        WriteWorldFile(0, data);

        var worlds = RunParser();

        Assert.Equal(150, worlds[0].Cities[0].MapX);
        Assert.Equal(75, worlds[0].Cities[0].MapY);
    }

    [Fact]
    public void Parse_SingleCity_ReadsUnknownFields()
    {
        var data = WorldTestDataGenerator.BuildWorldFile(
            new[] { WorldTestDataGenerator.BuildCity(unknown1: 0x1234, unknown2: 0x5678) },
            Array.Empty<byte[]>());
        WriteWorldFile(0, data);

        var worlds = RunParser();

        Assert.Equal(0x1234, worlds[0].Cities[0].Unknown1);
        Assert.Equal(0x5678, worlds[0].Cities[0].Unknown2);
    }

    [Fact]
    public void Parse_MultipleCities_ReadsAll()
    {
        var data = WorldTestDataGenerator.BuildWorldFile(
            new[]
            {
                WorldTestDataGenerator.BuildCity(name: "London"),
                WorldTestDataGenerator.BuildCity(name: "Paris"),
                WorldTestDataGenerator.BuildCity(name: "Berlin")
            },
            Array.Empty<byte[]>());
        WriteWorldFile(0, data);

        var worlds = RunParser();

        Assert.Equal(3, worlds[0].Cities.Count);
        Assert.Equal("London", worlds[0].Cities[0].Name);
        Assert.Equal("Paris", worlds[0].Cities[1].Name);
        Assert.Equal("Berlin", worlds[0].Cities[2].Name);
    }

    #endregion

    #region Organisation parsing

    [Fact]
    public void Parse_SingleOrg_ReadsShortName()
    {
        var data = WorldTestDataGenerator.BuildWorldFile(
            Array.Empty<byte[]>(),
            new[] { WorldTestDataGenerator.BuildOrganisation(shortName: "CIA") });
        WriteWorldFile(0, data);

        var worlds = RunParser();

        Assert.Equal("CIA", worlds[0].Organisations[0].ShortName);
    }

    [Fact]
    public void Parse_SingleOrg_ReadsLongName()
    {
        var data = WorldTestDataGenerator.BuildWorldFile(
            Array.Empty<byte[]>(),
            new[] { WorldTestDataGenerator.BuildOrganisation(longName: "Central Agency") });
        WriteWorldFile(0, data);

        var worlds = RunParser();

        Assert.Equal("Central Agency", worlds[0].Organisations[0].LongName);
    }

    [Fact]
    public void Parse_SingleOrg_ReadsUniqueId()
    {
        var data = WorldTestDataGenerator.BuildWorldFile(
            Array.Empty<byte[]>(),
            new[] { WorldTestDataGenerator.BuildOrganisation(uniqueId: 42) });
        WriteWorldFile(0, data);

        var worlds = RunParser();

        Assert.Equal(42, worlds[0].Organisations[0].UniqueId);
    }

    [Fact]
    public void Parse_SingleOrg_ReadsUnknownFields()
    {
        var data = WorldTestDataGenerator.BuildWorldFile(
            Array.Empty<byte[]>(),
            new[] { WorldTestDataGenerator.BuildOrganisation(
                unknown1: 0x0100, unknown2: 0x0200, unknown3: 0x0300, unknown4: 0x0400) });
        WriteWorldFile(0, data);

        var worlds = RunParser();

        var org = worlds[0].Organisations[0];
        Assert.Equal(0x0100, org.Unknown1);
        Assert.Equal(0x0200, org.Unknown2);
        Assert.Equal(0x0300, org.Unknown3);
        Assert.Equal(0x0400, org.Unknown4);
    }

    [Fact]
    public void Parse_OrgWithUniqueId0xFF_AllowMastermindFalse()
    {
        var data = WorldTestDataGenerator.BuildWorldFile(
            Array.Empty<byte[]>(),
            new[] { WorldTestDataGenerator.BuildOrganisation(uniqueId: 0x00FF) });
        WriteWorldFile(0, data);

        var worlds = RunParser();

        Assert.False(worlds[0].Organisations[0].AllowMastermind);
    }

    [Fact]
    public void Parse_OrgWithNormalUniqueId_AllowMastermindTrue()
    {
        var data = WorldTestDataGenerator.BuildWorldFile(
            Array.Empty<byte[]>(),
            new[] { WorldTestDataGenerator.BuildOrganisation(uniqueId: 5) });
        WriteWorldFile(0, data);

        var worlds = RunParser();

        Assert.True(worlds[0].Organisations[0].AllowMastermind);
    }

    [Fact]
    public void Parse_MultipleOrgs_ReadsAll()
    {
        var data = WorldTestDataGenerator.BuildWorldFile(
            Array.Empty<byte[]>(),
            new[]
            {
                WorldTestDataGenerator.BuildOrganisation(shortName: "CIA"),
                WorldTestDataGenerator.BuildOrganisation(shortName: "KGB"),
                WorldTestDataGenerator.BuildOrganisation(shortName: "MI6")
            });
        WriteWorldFile(0, data);

        var worlds = RunParser();

        Assert.Equal(3, worlds[0].Organisations.Count);
        Assert.Equal("CIA", worlds[0].Organisations[0].ShortName);
        Assert.Equal("KGB", worlds[0].Organisations[1].ShortName);
        Assert.Equal("MI6", worlds[0].Organisations[2].ShortName);
    }

    #endregion

    #region Mixed cities and organisations

    [Fact]
    public void Parse_CitiesAndOrgs_ReadsBoth()
    {
        var data = WorldTestDataGenerator.BuildWorldFile(
            new[]
            {
                WorldTestDataGenerator.BuildCity(name: "Moscow"),
                WorldTestDataGenerator.BuildCity(name: "Tokyo")
            },
            new[]
            {
                WorldTestDataGenerator.BuildOrganisation(shortName: "FSB")
            });
        WriteWorldFile(0, data);

        var worlds = RunParser();

        Assert.Equal(2, worlds[0].Cities.Count);
        Assert.Single(worlds[0].Organisations);
    }

    #endregion

    #region Metadata and file key

    [Fact]
    public void Parse_SetsCorrectId()
    {
        var data = WorldTestDataGenerator.BuildMinimalWorldFile();
        WriteWorldFile(5, data);

        var worlds = RunParser();

        Assert.True(worlds.ContainsKey(5));
        Assert.Equal(5, worlds[5].Id);
    }

    [Fact]
    public void Parse_SetsMetadata()
    {
        var data = WorldTestDataGenerator.BuildMinimalWorldFile();
        WriteWorldFile(2, data);

        var worlds = RunParser();

        Assert.Equal("WORLD2", worlds[2].Metadata.Name);
        Assert.Equal("Legacy import", worlds[2].Metadata.Comment);
    }

    [Fact]
    public void Parse_MultipleFiles_ParsesAll()
    {
        WriteWorldFile(0, WorldTestDataGenerator.BuildMinimalWorldFile());
        WriteWorldFile(1, WorldTestDataGenerator.BuildMinimalWorldFile());
        WriteWorldFile(2, WorldTestDataGenerator.BuildMinimalWorldFile());

        var worlds = RunParser();

        Assert.Equal(3, worlds.Count);
        Assert.True(worlds.ContainsKey(0));
        Assert.True(worlds.ContainsKey(1));
        Assert.True(worlds.ContainsKey(2));
    }

    #endregion

    #region Edge cases

    [Fact]
    public void Parse_EmptyWorld_NoCitiesNoOrgs()
    {
        var data = WorldTestDataGenerator.BuildWorldFile(
            Array.Empty<byte[]>(),
            Array.Empty<byte[]>());
        WriteWorldFile(0, data);

        var worlds = RunParser();

        Assert.Empty(worlds[0].Cities);
        Assert.Empty(worlds[0].Organisations);
    }

    #endregion

    #region SetResult

    [Fact]
    public void SetResult_PopulatesWorldsOnPackageModel()
    {
        var data = WorldTestDataGenerator.BuildMinimalWorldFile();
        WriteWorldFile(0, data);

        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);

        Assert.NotEmpty(model.Worlds);
        Assert.True(model.Worlds.ContainsKey(0));
    }

    #endregion

    #region Helpers

    private void WriteWorldFile(int key, byte[] data)
    {
        File.WriteAllBytes(Path.Combine(_tempDir, $"WORLD{key}.DTA"), data);
    }

    private System.Collections.Generic.Dictionary<int, WorldModel> RunParser()
    {
        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);
        return model.Worlds;
    }

    #endregion
}
