using System;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class LegacyCrimeParserTests : IDisposable
{
    private readonly LegacyCrimeParser _parser;
    private readonly string _tempDir;

    public LegacyCrimeParserTests()
    {
        _parser = new LegacyCrimeParser(NullLogger<LegacyCrimeParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyCrimeParserTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Participant parsing

    [Fact]
    public void Parse_SingleParticipant_ReadsExposure()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant(exposure: 42) },
            Array.Empty<byte[]>());
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Equal(42, crimes[0].Participants[0].Exposure);
    }

    [Fact]
    public void Parse_SingleParticipant_ReadsRole()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant(role: "Mastermind Leader") },
            Array.Empty<byte[]>());
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Equal("Mastermind Leader", crimes[0].Participants[0].Role);
    }

    [Fact]
    public void Parse_SingleParticipant_ReadsClueType()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant(clueType: 3) },
            Array.Empty<byte[]>());
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Equal(ClueType.AirlineTicket, crimes[0].Participants[0].ClueType);
    }

    [Fact]
    public void Parse_SingleParticipant_ReadsRank()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant(rank: 5) },
            Array.Empty<byte[]>());
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Equal(5, crimes[0].Participants[0].Rank);
    }

    [Fact]
    public void Parse_MastermindParticipant_SetsIsMastermind()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant(participantType: 1) },
            Array.Empty<byte[]>());
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.True(crimes[0].Participants[0].IsMastermind);
    }

    [Fact]
    public void Parse_WidowParticipant_SetsForceFemale()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant(participantType: 2) },
            Array.Empty<byte[]>());
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.True(crimes[0].Participants[0].ForceFemale);
    }

    [Fact]
    public void Parse_AssassinParticipant_SetsCanComeOutOfHiding()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant(participantType: 64) },
            Array.Empty<byte[]>());
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.True(crimes[0].Participants[0].CanComeOutOfHiding);
    }

    [Fact]
    public void Parse_InsideContact_SetsIsInsideContact()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant(unknown2: 0x01) },
            Array.Empty<byte[]>());
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.True(crimes[0].Participants[0].IsInsideContact);
    }

    [Fact]
    public void Parse_NormalParticipant_AllFlagsFalse()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant(participantType: 0, unknown2: 0x48) },
            Array.Empty<byte[]>());
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        var p = crimes[0].Participants[0];
        Assert.False(p.IsMastermind);
        Assert.False(p.ForceFemale);
        Assert.False(p.CanComeOutOfHiding);
        Assert.False(p.IsInsideContact);
    }

    [Fact]
    public void Parse_MultipleParticipants_ReadsAll()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[]
            {
                CrimeTestDataGenerator.BuildParticipant(role: "Leader", exposure: 10),
                CrimeTestDataGenerator.BuildParticipant(role: "Agent", exposure: 20),
                CrimeTestDataGenerator.BuildParticipant(role: "Courier", exposure: 30)
            },
            Array.Empty<byte[]>());
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Equal(3, crimes[0].Participants.Count);
        Assert.Equal("Leader", crimes[0].Participants[0].Role);
        Assert.Equal("Agent", crimes[0].Participants[1].Role);
        Assert.Equal("Courier", crimes[0].Participants[2].Role);
    }

    [Fact]
    public void Parse_Participant_ReadsUnknownFields()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant(
                unknown1: 13, unknown2: 0x4C, unknown3: 5, unknown4: 0x0700, unknown5: 0) },
            Array.Empty<byte[]>());
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        var p = crimes[0].Participants[0];
        Assert.Equal(13, p.Unknown1);
        Assert.Equal(0x4C, p.Unknown2);
        Assert.Equal(5, p.Unknown3);
        Assert.Equal(0x0700, p.Unknown4);
        Assert.Equal(0, p.Unknown5);
    }

    #endregion

    #region Individual event parsing

    [Fact]
    public void Parse_IndividualEvent_ReadsDescription()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant() },
            new[] { CrimeTestDataGenerator.BuildIndividualEvent(
                sourceParticipantId: 0, description: "Stole the plans") });
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Single(crimes[0].Events);
        Assert.Equal("Stole the plans", crimes[0].Events[0].ReceiveDescription);
        Assert.Equal("Stole the plans", crimes[0].Events[0].SendDescription);
    }

    [Fact]
    public void Parse_IndividualEvent_ReadsMainParticipantId()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[]
            {
                CrimeTestDataGenerator.BuildParticipant(),
                CrimeTestDataGenerator.BuildParticipant()
            },
            new[] { CrimeTestDataGenerator.BuildIndividualEvent(sourceParticipantId: 1) });
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Equal(1, crimes[0].Events[0].MainParticipantId);
    }

    [Fact]
    public void Parse_IndividualEvent_HasNoSecondaryParticipant()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant() },
            new[] { CrimeTestDataGenerator.BuildIndividualEvent() });
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Null(crimes[0].Events[0].SecondaryParticipantId);
    }

    [Fact]
    public void Parse_IndividualEvent_ReadsMessageId()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant() },
            new[] { CrimeTestDataGenerator.BuildIndividualEvent(messageId: 42) });
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Equal(42, crimes[0].Events[0].MessageId);
    }

    [Fact]
    public void Parse_IndividualEvent_ReadsScore()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant() },
            new[] { CrimeTestDataGenerator.BuildIndividualEvent(score: 150) });
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Equal(150, crimes[0].Events[0].Score);
    }

    [Fact]
    public void Parse_IndividualEvent_NotPaired()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant() },
            new[] { CrimeTestDataGenerator.BuildIndividualEvent() });
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        var evt = crimes[0].Events[0];
        Assert.False(evt.IsMessage);
        Assert.False(evt.IsPackage);
        Assert.False(evt.IsMeeting);
    }

    [Fact]
    public void Parse_BulletinEvent_SetsBulletinFlag()
    {
        // EventType 0x20 = Bulletin (bit 5 set, low nibble 0 = individual)
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant() },
            new[] { CrimeTestDataGenerator.BuildIndividualEvent(eventTypeByte: 0x20) });
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.True(crimes[0].Events[0].IsBulletin);
    }

    [Fact]
    public void Parse_IndividualEvent_ReadsReceivedObjects()
    {
        // Bitmask 0x05 = bit 0 + bit 2 = objects 0 and 2
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant() },
            new[] { CrimeTestDataGenerator.BuildIndividualEvent(receivedObjectBitmask: 0x05) });
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Contains(0, crimes[0].Events[0].ReceivedObjectIds);
        Assert.Contains(2, crimes[0].Events[0].ReceivedObjectIds);
        Assert.Equal(2, crimes[0].Events[0].ReceivedObjectIds.Count);
    }

    [Fact]
    public void Parse_IndividualEvent_ReadsDestroyedObjects()
    {
        // Bitmask 0x02 = bit 1 = object 1
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant() },
            new[] { CrimeTestDataGenerator.BuildIndividualEvent(destroyedObjectBitmask: 0x02) });
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Contains(1, crimes[0].Events[0].DestroyedObjectIds);
        Assert.Single(crimes[0].Events[0].DestroyedObjectIds);
    }

    [Fact]
    public void Parse_IgnoredEvent_IsSkipped()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant() },
            new[]
            {
                CrimeTestDataGenerator.BuildIgnoredEvent(),
                CrimeTestDataGenerator.BuildIndividualEvent(description: "Real event")
            });
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Single(crimes[0].Events);
        Assert.Equal("Real event", crimes[0].Events[0].ReceiveDescription);
    }

    #endregion

    #region Paired event parsing

    [Fact]
    public void Parse_PairedMessageEvents_MergedCorrectly()
    {
        // SentMessage = 2, ReceivedMessage = 3, same messageId
        var sendEvent = CrimeTestDataGenerator.BuildPairedSendEvent(
            sourceParticipantId: 0,
            messageId: 10,
            description: "Sent secret plans",
            targetParticipantId: 1,
            eventTypeByte: 2); // SentMessage

        var receiveEvent = CrimeTestDataGenerator.BuildPairedSendEvent(
            sourceParticipantId: 1,
            messageId: 10,
            description: "Received secret plans",
            targetParticipantId: 0,
            eventTypeByte: 3); // ReceivedMessage

        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[]
            {
                CrimeTestDataGenerator.BuildParticipant(role: "Sender"),
                CrimeTestDataGenerator.BuildParticipant(role: "Receiver")
            },
            new[] { sendEvent, receiveEvent });
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Single(crimes[0].Events);
        var evt = crimes[0].Events[0];
        Assert.True(evt.IsMessage);
        Assert.Equal("Sent secret plans", evt.SendDescription);
        Assert.Equal("Received secret plans", evt.ReceiveDescription);
    }

    #endregion

    #region Object parsing

    [Fact]
    public void Parse_RealObjects_ReadsNameAndPictureId()
    {
        var objects = new[]
        {
            CrimeTestDataGenerator.BuildObject("Secret Doc", 3),
            CrimeTestDataGenerator.BuildBlankObject(),
            CrimeTestDataGenerator.BuildBlankObject(),
            CrimeTestDataGenerator.BuildBlankObject()
        };

        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant() },
            Array.Empty<byte[]>(),
            objects);
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Single(crimes[0].Objects);
        Assert.Equal("Secret Doc", crimes[0].Objects[0].Name);
        Assert.Equal(3, crimes[0].Objects[0].PictureId);
    }

    [Fact]
    public void Parse_MultipleRealObjects_ReadsAll()
    {
        var objects = new[]
        {
            CrimeTestDataGenerator.BuildObject("Item A", 0),
            CrimeTestDataGenerator.BuildObject("Item B", 5),
            CrimeTestDataGenerator.BuildBlankObject(),
            CrimeTestDataGenerator.BuildObject("Item C", 12)
        };

        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant() },
            Array.Empty<byte[]>(),
            objects);
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Equal(3, crimes[0].Objects.Count);
        Assert.Equal("Item A", crimes[0].Objects[0].Name);
        Assert.Equal("Item B", crimes[0].Objects[1].Name);
        Assert.Equal("Item C", crimes[0].Objects[2].Name);
    }

    [Fact]
    public void Parse_AllBlankObjects_ReturnsEmptyList()
    {
        var data = CrimeTestDataGenerator.BuildCrimeFile(
            new[] { CrimeTestDataGenerator.BuildParticipant() },
            Array.Empty<byte[]>());
        WriteCrimeFile(0, data);

        var crimes = RunParser();

        Assert.Empty(crimes[0].Objects);
    }

    #endregion

    #region Metadata and file key

    [Fact]
    public void Parse_SetsCorrectId()
    {
        var data = CrimeTestDataGenerator.BuildMinimalCrimeFile();
        WriteCrimeFile(7, data);

        var crimes = RunParser();

        Assert.True(crimes.ContainsKey(7));
        Assert.Equal(7, crimes[7].Id);
    }

    [Fact]
    public void Parse_SetsMetadata()
    {
        var data = CrimeTestDataGenerator.BuildMinimalCrimeFile();
        WriteCrimeFile(3, data);

        var crimes = RunParser();

        Assert.Equal("CRIME3", crimes[3].Metadata.Name);
        Assert.Equal("Legacy import", crimes[3].Metadata.Comment);
    }

    [Fact]
    public void Parse_MultipleFiles_ParsesAll()
    {
        WriteCrimeFile(0, CrimeTestDataGenerator.BuildMinimalCrimeFile());
        WriteCrimeFile(1, CrimeTestDataGenerator.BuildMinimalCrimeFile());
        WriteCrimeFile(2, CrimeTestDataGenerator.BuildMinimalCrimeFile());

        var crimes = RunParser();

        Assert.Equal(3, crimes.Count);
        Assert.True(crimes.ContainsKey(0));
        Assert.True(crimes.ContainsKey(1));
        Assert.True(crimes.ContainsKey(2));
    }

    #endregion

    #region SetResult

    [Fact]
    public void SetResult_PopulatesCrimesOnPackageModel()
    {
        var data = CrimeTestDataGenerator.BuildMinimalCrimeFile();
        WriteCrimeFile(0, data);

        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);

        Assert.NotEmpty(model.Crimes);
        Assert.True(model.Crimes.ContainsKey(0));
    }

    #endregion

    #region Helpers

    private void WriteCrimeFile(int key, byte[] data)
    {
        File.WriteAllBytes(Path.Combine(_tempDir, $"CRIME{key}.DTA"), data);
    }

    private System.Collections.Generic.Dictionary<int, CrimeModel> RunParser()
    {
        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);
        return model.Crimes;
    }

    #endregion
}
