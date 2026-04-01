using System;
using System.IO;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Parsers.Data;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class LegacyClueParserTests : IDisposable
{
    private readonly LegacyClueParser _parser;
    private readonly string _tempDir;

    public LegacyClueParserTests()
    {
        _parser = new LegacyClueParser(NullLogger<LegacyClueParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyClueParserTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Non-crime clues

    [Fact]
    public void Parse_SingleNonCrimeClue_SetsTypeAndId()
    {
        var entry = ClueTestDataGenerator.BuildNonCrimeClueEntry(type: 3, id: 2, source: 1, message: "\r\nHello world\r\n");
        var fileData = ClueTestDataGenerator.BuildCluesFile(entry);
        ClueTestDataGenerator.WriteCluesFile(_tempDir, fileData);

        var package = RunParser();

        Assert.True(package.Clues.ContainsKey("C32"));
        var clue = package.Clues["C32"];
        Assert.Equal(ClueType.AirlineTicket, clue.Type);
        Assert.Equal(2, clue.Id);
    }

    [Fact]
    public void Parse_SingleNonCrimeClue_SetsSource()
    {
        var entry = ClueTestDataGenerator.BuildNonCrimeClueEntry(type: 1, id: 0, source: 4, message: "\r\nTest message\r\n");
        var fileData = ClueTestDataGenerator.BuildCluesFile(entry);
        ClueTestDataGenerator.WriteCluesFile(_tempDir, fileData);

        var package = RunParser();

        var clue = package.Clues["C10"];
        Assert.Equal(ClueModel.ClueSource.LocalInformant, clue.Source);
    }

    [Fact]
    public void Parse_SingleNonCrimeClue_SetsMessage()
    {
        var entry = ClueTestDataGenerator.BuildNonCrimeClueEntry(type: 0, id: 1, source: 2, message: "\r\nSome clue text\r\n");
        var fileData = ClueTestDataGenerator.BuildCluesFile(entry);
        ClueTestDataGenerator.WriteCluesFile(_tempDir, fileData);

        var package = RunParser();

        var clue = package.Clues["C01"];
        Assert.Equal("Some clue text", clue.Message);
    }

    [Fact]
    public void Parse_NonCrimeClue_NullCrimeId()
    {
        var entry = ClueTestDataGenerator.BuildNonCrimeClueEntry(type: 2, id: 0, source: 0, message: "\r\nMsg\r\n");
        var fileData = ClueTestDataGenerator.BuildCluesFile(entry);
        ClueTestDataGenerator.WriteCluesFile(_tempDir, fileData);

        var package = RunParser();

        var clue = package.Clues["C20"];
        Assert.Null(clue.CrimeId);
    }

    #endregion

    #region Crime-specific clues

    [Fact]
    public void Parse_SingleCrimeClue_SetsCrimeIdAndParticipantId()
    {
        var entry = ClueTestDataGenerator.BuildCrimeClueEntry(crimeId: 3, participantId: 5, source: 1, type: 2, message: "\r\nCrime clue\r\n");
        var fileData = ClueTestDataGenerator.BuildCluesFile(entry);
        ClueTestDataGenerator.WriteCluesFile(_tempDir, fileData);

        var package = RunParser();

        Assert.True(package.Clues.ContainsKey("C0305"));
        var clue = package.Clues["C0305"];
        Assert.Equal(3, clue.CrimeId);
        Assert.Equal(5, clue.Id);
    }

    [Fact]
    public void Parse_SingleCrimeClue_SetsSourceAndType()
    {
        var entry = ClueTestDataGenerator.BuildCrimeClueEntry(crimeId: 1, participantId: 2, source: 3, type: 7, message: "\r\nTest\r\n");
        var fileData = ClueTestDataGenerator.BuildCluesFile(entry);
        ClueTestDataGenerator.WriteCluesFile(_tempDir, fileData);

        var package = RunParser();

        var clue = package.Clues["C0102"];
        Assert.Equal(ClueModel.ClueSource.FileRecordSearch, clue.Source);
        Assert.Equal(ClueType.IdentityDocument, clue.Type);
    }

    [Fact]
    public void Parse_SingleCrimeClue_SetsMessage()
    {
        var entry = ClueTestDataGenerator.BuildCrimeClueEntry(crimeId: 0, participantId: 1, source: 5, type: 0, message: "\r\nCrime message here\r\n");
        var fileData = ClueTestDataGenerator.BuildCluesFile(entry);
        ClueTestDataGenerator.WriteCluesFile(_tempDir, fileData);

        var package = RunParser();

        var clue = package.Clues["C0001"];
        Assert.Equal("Crime message here", clue.Message);
    }

    #endregion

    #region Multiple entries

    [Fact]
    public void Parse_MultipleEntries_ReturnsAll()
    {
        var entry1 = ClueTestDataGenerator.BuildNonCrimeClueEntry(type: 1, id: 0, source: 2, message: "\r\nFirst\r\n");
        var entry2 = ClueTestDataGenerator.BuildNonCrimeClueEntry(type: 2, id: 1, source: 3, message: "\r\nSecond\r\n");
        var fileData = ClueTestDataGenerator.BuildCluesFile(entry1, entry2);
        ClueTestDataGenerator.WriteCluesFile(_tempDir, fileData);

        var package = RunParser();

        Assert.Equal(2, package.Clues.Count);
        Assert.Equal("First", package.Clues["C10"].Message);
        Assert.Equal("Second", package.Clues["C21"].Message);
    }

    #endregion

    #region Duplicate entries

    [Fact]
    public void Parse_DuplicateCrimeClue_SharesMessageWithNext()
    {
        var dup = ClueTestDataGenerator.BuildDuplicateCrimeClueEntry(crimeId: 2, participantId: 3);
        var real = ClueTestDataGenerator.BuildCrimeClueEntry(crimeId: 2, participantId: 4, source: 1, type: 0, message: "\r\nShared message\r\n");
        var fileData = ClueTestDataGenerator.BuildCluesFile(dup, real);
        ClueTestDataGenerator.WriteCluesFile(_tempDir, fileData);

        var package = RunParser();

        Assert.Equal(2, package.Clues.Count);
        Assert.Equal("Shared message", package.Clues["C0203"].Message);
        Assert.Equal("Shared message", package.Clues["C0204"].Message);
    }

    #endregion

    #region Message trimming

    [Fact]
    public void Parse_MessageWithLeadingAndTrailingNewlines_TrimsOnePair()
    {
        var entry = ClueTestDataGenerator.BuildNonCrimeClueEntry(type: 0, id: 0, source: 0, message: "\r\nInner\r\nContent\r\n");
        var fileData = ClueTestDataGenerator.BuildCluesFile(entry);
        ClueTestDataGenerator.WriteCluesFile(_tempDir, fileData);

        var package = RunParser();

        var clue = package.Clues["C00"];
        Assert.Equal("Inner\r\nContent", clue.Message);
    }

    #endregion

    #region SetResult

    [Fact]
    public void SetResult_PopulatesCluesOnPackageModel()
    {
        var entry = ClueTestDataGenerator.BuildNonCrimeClueEntry(type: 1, id: 0, source: 0, message: "\r\nTest\r\n");
        var fileData = ClueTestDataGenerator.BuildCluesFile(entry);
        ClueTestDataGenerator.WriteCluesFile(_tempDir, fileData);

        var package = RunParser();

        Assert.NotEmpty(package.Clues);
    }

    #endregion

    #region Helpers

    private PackageModel RunParser()
    {
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        var package = new PackageModel();
        _parser.SetResult(package);
        return package;
    }

    #endregion
}
