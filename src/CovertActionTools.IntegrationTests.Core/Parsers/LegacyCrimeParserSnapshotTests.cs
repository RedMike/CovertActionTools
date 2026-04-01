using System;
using System.IO;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.IntegrationTests.Core.Parsers.Data;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class LegacyCrimeParserSnapshotTests : IDisposable
{
    private readonly LegacyCrimeParser _parser;
    private readonly string _tempDir;

    public LegacyCrimeParserSnapshotTests()
    {
        _parser = new LegacyCrimeParser(NullLogger<LegacyCrimeParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyCrimeSnapshotTests_{Guid.NewGuid():N}");
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
    public void Snapshot_StandardCrimeFile_MatchesExpectedBinary()
    {
        var generated = CrimeTestDataGenerator.BuildStandardCrimeFile();
        var actual = Convert.ToBase64String(generated);
        Assert.Equal(CrimeSnapshotData.StandardCrimeFile, actual);
    }

    #endregion

    #region Parse snapshot tests (parsed model properties from known binary)

    [Fact]
    public void ParseSnapshot_StandardCrime_HasTwoParticipants()
    {
        var crime = ParseFromSnapshot(CrimeSnapshotData.StandardCrimeFile);

        Assert.Equal(2, crime.Participants.Count);
    }

    [Fact]
    public void ParseSnapshot_StandardCrime_MastermindParticipant()
    {
        var crime = ParseFromSnapshot(CrimeSnapshotData.StandardCrimeFile);

        var mastermind = crime.Participants[0];
        Assert.Equal("Mastermind", mastermind.Role);
        Assert.Equal(50, mastermind.Exposure);
        Assert.True(mastermind.IsMastermind);
        Assert.Equal(ClueType.Address, mastermind.ClueType);
        Assert.Equal(3, mastermind.Rank);
    }

    [Fact]
    public void ParseSnapshot_StandardCrime_CourierParticipant()
    {
        var crime = ParseFromSnapshot(CrimeSnapshotData.StandardCrimeFile);

        var courier = crime.Participants[1];
        Assert.Equal("Courier", courier.Role);
        Assert.Equal(80, courier.Exposure);
        Assert.False(courier.IsMastermind);
        Assert.Equal(ClueType.MoneyHundreds, courier.ClueType);
        Assert.Equal(1, courier.Rank);
    }

    [Fact]
    public void ParseSnapshot_StandardCrime_HasTwoEvents()
    {
        var crime = ParseFromSnapshot(CrimeSnapshotData.StandardCrimeFile);

        // 1 individual (bulletin) + 1 merged pair = 2 events
        Assert.Equal(2, crime.Events.Count);
    }

    [Fact]
    public void ParseSnapshot_StandardCrime_IndividualBulletinEvent()
    {
        var crime = ParseFromSnapshot(CrimeSnapshotData.StandardCrimeFile);

        var bulletin = crime.Events[0];
        Assert.Equal(0, bulletin.MainParticipantId);
        Assert.Null(bulletin.SecondaryParticipantId);
        Assert.True(bulletin.IsBulletin);
        Assert.Equal(100, bulletin.Score);
        Assert.Equal("Initiated the plan", bulletin.ReceiveDescription);
    }

    [Fact]
    public void ParseSnapshot_StandardCrime_PairedMessageEvent()
    {
        var crime = ParseFromSnapshot(CrimeSnapshotData.StandardCrimeFile);

        var message = crime.Events[1];
        Assert.True(message.IsMessage);
        Assert.Equal("Sent orders", message.SendDescription);
        Assert.Equal("Received orders", message.ReceiveDescription);
        Assert.Equal(5, message.MessageId);
    }

    [Fact]
    public void ParseSnapshot_StandardCrime_HasTwoObjects()
    {
        var crime = ParseFromSnapshot(CrimeSnapshotData.StandardCrimeFile);

        Assert.Equal(2, crime.Objects.Count);
        Assert.Equal("Secret Plans", crime.Objects[0].Name);
        Assert.Equal(3, crime.Objects[0].PictureId);
        Assert.Equal("Ransom Money", crime.Objects[1].Name);
        Assert.Equal(7, crime.Objects[1].PictureId);
    }

    [Fact]
    public void ParseSnapshot_StandardCrime_HasCorrectMetadata()
    {
        var crime = ParseFromSnapshot(CrimeSnapshotData.StandardCrimeFile);

        Assert.Equal(0, crime.Id);
        Assert.Equal("CRIME0", crime.Metadata.Name);
        Assert.Equal("Legacy import", crime.Metadata.Comment);
    }

    #endregion

    #region Helpers

    private CrimeModel ParseFromSnapshot(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var filePath = Path.Combine(_tempDir, "CRIME0.DTA");
        File.WriteAllBytes(filePath, bytes);

        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);
        return model.Crimes[0];
    }

    #endregion
}
