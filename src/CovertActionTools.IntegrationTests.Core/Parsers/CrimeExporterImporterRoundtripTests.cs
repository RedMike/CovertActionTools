using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class CrimeExporterImporterRoundtripTests : IDisposable
{
    private readonly CrimeExporter _exporter;
    private readonly CrimeImporter _importer;
    private readonly string _tempDir;

    public CrimeExporterImporterRoundtripTests()
    {
        _exporter = new CrimeExporter(NullLogger<CrimeExporter>.Instance);
        _importer = new CrimeImporter(NullLogger<CrimeImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"CrimeRoundtrip_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Single item roundtrip

    [Fact]
    public void Roundtrip_SingleCrime_AllPropertiesMatch()
    {
        var original = CreateDetailedCrime(0);
        var result = ExportAndImportModel(original);

        Assert.Single(result.Crimes);
        var crime = result.Crimes[0];

        Assert.Equal(0, crime.Id);
        Assert.Equal("Crime 0", crime.Metadata.Name);
        Assert.Equal("Detailed test crime", crime.Metadata.Comment);

        Assert.Equal(3, crime.Participants.Count);
        AssertParticipantMatches(original.Crimes[0].Participants[0], crime.Participants[0]);
        AssertParticipantMatches(original.Crimes[0].Participants[1], crime.Participants[1]);
        AssertParticipantMatches(original.Crimes[0].Participants[2], crime.Participants[2]);

        Assert.Equal(2, crime.Events.Count);
        AssertEventMatches(original.Crimes[0].Events[0], crime.Events[0]);
        AssertEventMatches(original.Crimes[0].Events[1], crime.Events[1]);

        Assert.Equal(2, crime.Objects.Count);
        Assert.Equal("Explosives", crime.Objects[0].Name);
        Assert.Equal(5, crime.Objects[0].PictureId);
        Assert.Equal("Forged Passport", crime.Objects[1].Name);
        Assert.Equal(7, crime.Objects[1].PictureId);
    }

    #endregion

    #region Multiple items roundtrip

    [Fact]
    public void Roundtrip_MultipleCrimes_AllPreserved()
    {
        var crime0 = CreateDetailedCrime(0);
        var crime3 = CreateDetailedCrime(3);
        var crime7 = CreateDetailedCrime(7);

        var packageModel = new PackageModel
        {
            Crimes = new Dictionary<int, CrimeModel>
            {
                [0] = crime0.Crimes[0],
                [3] = crime3.Crimes[3],
                [7] = crime7.Crimes[7]
            }
        };

        var result = ExportAndImportModel(packageModel);

        Assert.Equal(3, result.Crimes.Count);
        Assert.True(result.Crimes.ContainsKey(0));
        Assert.True(result.Crimes.ContainsKey(3));
        Assert.True(result.Crimes.ContainsKey(7));

        Assert.Equal(0, result.Crimes[0].Id);
        Assert.Equal(3, result.Crimes[3].Id);
        Assert.Equal(7, result.Crimes[7].Id);
    }

    #endregion

    #region Edge case roundtrips

    [Fact]
    public void Roundtrip_CrimeWithEmptyCollections_Preserved()
    {
        var crime = new CrimeModel
        {
            Id = 10,
            Participants = new List<CrimeModel.Participant>(),
            Events = new List<CrimeModel.Event>(),
            Objects = new List<CrimeModel.Object>(),
            Metadata = new SharedMetadata { Name = "Empty crime", Comment = "" }
        };

        var model = new PackageModel
        {
            Crimes = new Dictionary<int, CrimeModel> { [10] = crime }
        };

        var result = ExportAndImportModel(model);

        Assert.Single(result.Crimes);
        var imported = result.Crimes[10];
        Assert.Empty(imported.Participants);
        Assert.Empty(imported.Events);
        Assert.Empty(imported.Objects);
    }

    [Fact]
    public void Roundtrip_CrimeWithNullSecondaryParticipant_Preserved()
    {
        var crime = new CrimeModel
        {
            Id = 4,
            Participants = new List<CrimeModel.Participant>
            {
                new CrimeModel.Participant { Role = "Solo Actor", IsMastermind = true, ClueType = ClueType.Weapon }
            },
            Events = new List<CrimeModel.Event>
            {
                new CrimeModel.Event
                {
                    MainParticipantId = 0,
                    SecondaryParticipantId = null,
                    MessageId = 42,
                    ReceiveDescription = "Planted device",
                    SendDescription = "",
                    IsBulletin = true,
                    Score = 100
                }
            },
            Objects = new List<CrimeModel.Object>(),
            Metadata = new SharedMetadata { Name = "Solo crime", Comment = "Individual event" }
        };

        var model = new PackageModel
        {
            Crimes = new Dictionary<int, CrimeModel> { [4] = crime }
        };

        var result = ExportAndImportModel(model);

        var imported = result.Crimes[4];
        Assert.Null(imported.Events[0].SecondaryParticipantId);
        Assert.True(imported.Events[0].IsBulletin);
        Assert.Equal(100, imported.Events[0].Score);
    }

    #endregion

    #region Helpers

    private PackageModel ExportAndImportModel(PackageModel model)
    {
        _exporter.Start(_tempDir, model);
        while (!_exporter.RunStep()) { }

        _importer.Start(_tempDir);
        while (!_importer.RunStep()) { }

        var result = new PackageModel();
        _importer.SetResult(result);
        return result;
    }

    private static PackageModel CreateDetailedCrime(int id)
    {
        var crime = new CrimeModel
        {
            Id = id,
            Participants = new List<CrimeModel.Participant>
            {
                new CrimeModel.Participant
                {
                    Exposure = 15,
                    Role = "Mastermind",
                    IsMastermind = true,
                    ForceFemale = false,
                    CanComeOutOfHiding = false,
                    IsInsideContact = false,
                    Unknown1 = 1,
                    Unknown2 = 0,
                    Unknown3 = 0,
                    Unknown4 = 0x0600,
                    Unknown5 = 0,
                    ClueType = ClueType.Vehicle,
                    Rank = 4
                },
                new CrimeModel.Participant
                {
                    Exposure = 8,
                    Role = "Bomb Maker",
                    IsMastermind = false,
                    ForceFemale = false,
                    CanComeOutOfHiding = true,
                    IsInsideContact = false,
                    Unknown1 = 1,
                    Unknown2 = 0x4C,
                    Unknown3 = 0,
                    Unknown4 = 0x0600,
                    Unknown5 = 0,
                    ClueType = ClueType.Weapon,
                    Rank = 2
                },
                new CrimeModel.Participant
                {
                    Exposure = 3,
                    Role = "Courier",
                    IsMastermind = false,
                    ForceFemale = true,
                    CanComeOutOfHiding = false,
                    IsInsideContact = true,
                    Unknown1 = 1,
                    Unknown2 = 0x21,
                    Unknown3 = 0,
                    Unknown4 = 0x0600,
                    Unknown5 = 0,
                    ClueType = ClueType.AirlineTicket,
                    Rank = 1
                }
            },
            Events = new List<CrimeModel.Event>
            {
                new CrimeModel.Event
                {
                    MainParticipantId = 1,
                    SecondaryParticipantId = 2,
                    MessageId = 200,
                    ReceiveDescription = "Received explosives",
                    SendDescription = "Shipped explosives",
                    IsMessage = false,
                    IsPackage = true,
                    IsMeeting = false,
                    IsBulletin = false,
                    Unknown1 = false,
                    ItemsToSecondary = false,
                    ReceivedObjectIds = new HashSet<int> { 0 },
                    DestroyedObjectIds = new HashSet<int>(),
                    Score = 0
                },
                new CrimeModel.Event
                {
                    MainParticipantId = 0,
                    SecondaryParticipantId = 1,
                    MessageId = 201,
                    ReceiveDescription = "Received forged passport",
                    SendDescription = "Delivered passport",
                    IsMessage = true,
                    IsPackage = false,
                    IsMeeting = false,
                    IsBulletin = true,
                    Unknown1 = true,
                    ItemsToSecondary = true,
                    ReceivedObjectIds = new HashSet<int> { 1 },
                    DestroyedObjectIds = new HashSet<int> { 0 },
                    Score = 75
                }
            },
            Objects = new List<CrimeModel.Object>
            {
                new CrimeModel.Object { Name = "Explosives", PictureId = 5 },
                new CrimeModel.Object { Name = "Forged Passport", PictureId = 7 }
            },
            Metadata = new SharedMetadata
            {
                Name = $"Crime {id}",
                Comment = "Detailed test crime"
            }
        };

        return new PackageModel
        {
            Crimes = new Dictionary<int, CrimeModel> { [id] = crime }
        };
    }

    private static void AssertParticipantMatches(CrimeModel.Participant expected, CrimeModel.Participant actual)
    {
        Assert.Equal(expected.Exposure, actual.Exposure);
        Assert.Equal(expected.Role, actual.Role);
        Assert.Equal(expected.IsMastermind, actual.IsMastermind);
        Assert.Equal(expected.ForceFemale, actual.ForceFemale);
        Assert.Equal(expected.CanComeOutOfHiding, actual.CanComeOutOfHiding);
        Assert.Equal(expected.IsInsideContact, actual.IsInsideContact);
        Assert.Equal(expected.Unknown1, actual.Unknown1);
        Assert.Equal(expected.Unknown2, actual.Unknown2);
        Assert.Equal(expected.Unknown3, actual.Unknown3);
        Assert.Equal(expected.Unknown4, actual.Unknown4);
        Assert.Equal(expected.Unknown5, actual.Unknown5);
        Assert.Equal(expected.ClueType, actual.ClueType);
        Assert.Equal(expected.Rank, actual.Rank);
    }

    private static void AssertEventMatches(CrimeModel.Event expected, CrimeModel.Event actual)
    {
        Assert.Equal(expected.MainParticipantId, actual.MainParticipantId);
        Assert.Equal(expected.SecondaryParticipantId, actual.SecondaryParticipantId);
        Assert.Equal(expected.MessageId, actual.MessageId);
        Assert.Equal(expected.ReceiveDescription, actual.ReceiveDescription);
        Assert.Equal(expected.SendDescription, actual.SendDescription);
        Assert.Equal(expected.IsMessage, actual.IsMessage);
        Assert.Equal(expected.IsPackage, actual.IsPackage);
        Assert.Equal(expected.IsMeeting, actual.IsMeeting);
        Assert.Equal(expected.IsBulletin, actual.IsBulletin);
        Assert.Equal(expected.Unknown1, actual.Unknown1);
        Assert.Equal(expected.ItemsToSecondary, actual.ItemsToSecondary);
        Assert.Equal(expected.ReceivedObjectIds, actual.ReceivedObjectIds);
        Assert.Equal(expected.DestroyedObjectIds, actual.DestroyedObjectIds);
        Assert.Equal(expected.Score, actual.Score);
    }

    #endregion
}
