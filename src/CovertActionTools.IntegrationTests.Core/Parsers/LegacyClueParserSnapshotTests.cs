using System;
using System.IO;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.IntegrationTests.Core.Parsers.Data;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class LegacyClueParserSnapshotTests : IDisposable
{
    private readonly LegacyClueParser _parser;
    private readonly string _tempDir;

    public LegacyClueParserSnapshotTests()
    {
        _parser = new LegacyClueParser(NullLogger<LegacyClueParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyClueParserSnapshotTests_{Guid.NewGuid():N}");
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
    public void Snapshot_MixedClues_MatchesExpectedBase64()
    {
        var nonCrime = ClueTestDataGenerator.BuildNonCrimeClueEntry(type: 3, id: 2, source: 1, message: "\r\nAirline ticket to Paris\r\n");
        var crime = ClueTestDataGenerator.BuildCrimeClueEntry(crimeId: 1, participantId: 5, source: 2, type: 7, message: "\r\nIdentity document found\r\n");
        var fileData = ClueTestDataGenerator.BuildCluesFile(nonCrime, crime);
        var base64 = Convert.ToBase64String(fileData);
        Assert.Equal(ClueSnapshotData.MixedClues, base64);
    }

    #endregion

    #region Parse snapshot tests

    [Fact]
    public void ParseSnapshot_MixedClues_HasCorrectEntryCount()
    {
        var package = ParseFromSnapshot(ClueSnapshotData.MixedClues);
        Assert.Equal(2, package.Clues.Count);
    }

    [Fact]
    public void ParseSnapshot_MixedClues_NonCrimeClue_HasCorrectProperties()
    {
        var package = ParseFromSnapshot(ClueSnapshotData.MixedClues);

        var clue = package.Clues["C32"];
        Assert.Equal(ClueType.AirlineTicket, clue.Type);
        Assert.Equal(2, clue.Id);
        Assert.Null(clue.CrimeId);
        Assert.Equal(ClueModel.ClueSource.WireTap, clue.Source);
        Assert.Equal("Airline ticket to Paris", clue.Message);
    }

    [Fact]
    public void ParseSnapshot_MixedClues_CrimeClue_HasCorrectProperties()
    {
        var package = ParseFromSnapshot(ClueSnapshotData.MixedClues);

        var clue = package.Clues["C0105"];
        Assert.Equal(ClueType.IdentityDocument, clue.Type);
        Assert.Equal(5, clue.Id);
        Assert.Equal(1, clue.CrimeId);
        Assert.Equal(ClueModel.ClueSource.CovertSurveillance, clue.Source);
        Assert.Equal("Identity document found", clue.Message);
    }

    #endregion

    #region Helpers

    private PackageModel ParseFromSnapshot(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var filePath = Path.Combine(_tempDir, "CLUES.TXT");
        File.WriteAllBytes(filePath, bytes);

        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        var package = new PackageModel();
        _parser.SetResult(package);
        return package;
    }

    #endregion
}
