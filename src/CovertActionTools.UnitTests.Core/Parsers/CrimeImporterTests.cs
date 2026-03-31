using System.Text.Json;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class CrimeImporterTests : IDisposable
{
    private readonly CrimeImporter _importer;
    private readonly string _tempDir;

    public CrimeImporterTests()
    {
        _importer = new CrimeImporter(NullLogger<CrimeImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"CrimeImporterTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region GetMessage

    [Fact]
    public void GetMessage_ReturnsExpectedString()
    {
        Assert.Equal("Processing crimes..", _importer.GetMessage());
    }

    #endregion

    #region CheckIfValid

    [Fact]
    public void CheckIfValid_WithCrimeFiles_ReturnsTrue()
    {
        var crimeDir = Path.Combine(_tempDir, "crime");
        Directory.CreateDirectory(crimeDir);
        WriteCrimeFile(crimeDir, CreateSampleCrime(0));

        Assert.True(_importer.CheckIfValid(_tempDir));
    }

    [Fact]
    public void CheckIfValid_EmptySubdirectory_ReturnsFalse()
    {
        var crimeDir = Path.Combine(_tempDir, "crime");
        Directory.CreateDirectory(crimeDir);

        Assert.False(_importer.CheckIfValid(_tempDir));
    }

    [Fact]
    public void CheckIfValid_NoSubdirectory_Throws()
    {
        Assert.ThrowsAny<Exception>(() => _importer.CheckIfValid(_tempDir));
    }

    #endregion

    #region Import single item

    [Fact]
    public void Import_SingleCrime_DeserializesCorrectly()
    {
        var crimeDir = Path.Combine(_tempDir, "crime");
        Directory.CreateDirectory(crimeDir);
        var original = CreateSampleCrime(3);
        WriteCrimeFile(crimeDir, original);

        _importer.Start(_tempDir);
        var done = _importer.RunStep();

        Assert.True(done);

        var result = new PackageModel();
        _importer.SetResult(result);

        Assert.Single(result.Crimes);
        Assert.True(result.Crimes.ContainsKey(3));
        var crime = result.Crimes[3];
        Assert.Equal(3, crime.Id);
        Assert.Equal(2, crime.Participants.Count);
        Assert.Equal("Mastermind", crime.Participants[0].Role);
        Assert.True(crime.Participants[0].IsMastermind);
        Assert.Equal(ClueType.Vehicle, crime.Participants[0].ClueType);
    }

    #endregion

    #region Import multiple items

    [Fact]
    public void Import_MultipleCrimes_AllDeserializedCorrectly()
    {
        var crimeDir = Path.Combine(_tempDir, "crime");
        Directory.CreateDirectory(crimeDir);
        WriteCrimeFile(crimeDir, CreateSampleCrime(0));
        WriteCrimeFile(crimeDir, CreateSampleCrime(2));

        _importer.Start(_tempDir);
        var done1 = _importer.RunStep();
        Assert.False(done1);

        var done2 = _importer.RunStep();
        Assert.True(done2);

        var result = new PackageModel();
        _importer.SetResult(result);

        Assert.Equal(2, result.Crimes.Count);
        Assert.True(result.Crimes.ContainsKey(0));
        Assert.True(result.Crimes.ContainsKey(2));
    }

    #endregion

    #region SetResult populates correct field

    [Fact]
    public void SetResult_PopulatesCrimesFieldOnPackageModel()
    {
        var crimeDir = Path.Combine(_tempDir, "crime");
        Directory.CreateDirectory(crimeDir);
        WriteCrimeFile(crimeDir, CreateSampleCrime(5));

        _importer.Start(_tempDir);
        _importer.RunStep();

        var packageModel = new PackageModel();
        _importer.SetResult(packageModel);

        Assert.NotEmpty(packageModel.Crimes);
        Assert.True(packageModel.Crimes.ContainsKey(5));
    }

    #endregion

    #region Event and Object data preserved

    [Fact]
    public void Import_PreservesEventAndObjectData()
    {
        var crimeDir = Path.Combine(_tempDir, "crime");
        Directory.CreateDirectory(crimeDir);
        WriteCrimeFile(crimeDir, CreateSampleCrime(1));

        _importer.Start(_tempDir);
        _importer.RunStep();

        var result = new PackageModel();
        _importer.SetResult(result);

        var crime = result.Crimes[1];
        Assert.Single(crime.Events);
        var evt = crime.Events[0];
        Assert.Equal(0, evt.MainParticipantId);
        Assert.Equal(1, evt.SecondaryParticipantId);
        Assert.True(evt.IsPackage);
        Assert.Contains(0, evt.ReceivedObjectIds);
        Assert.Equal(50, evt.Score);

        Assert.Single(crime.Objects);
        Assert.Equal("Bomb Parts", crime.Objects[0].Name);
        Assert.Equal(3, crime.Objects[0].PictureId);
    }

    #endregion

    #region Helpers

    private static CrimeModel CreateSampleCrime(int id)
    {
        return new CrimeModel
        {
            Id = id,
            Participants = new List<CrimeModel.Participant>
            {
                new CrimeModel.Participant
                {
                    Exposure = 10,
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
                    Rank = 3
                },
                new CrimeModel.Participant
                {
                    Exposure = 5,
                    Role = "Courier",
                    IsMastermind = false,
                    ForceFemale = true,
                    CanComeOutOfHiding = false,
                    IsInsideContact = true,
                    Unknown1 = 1,
                    Unknown2 = 0x48,
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
                    MainParticipantId = 0,
                    SecondaryParticipantId = 1,
                    MessageId = 100,
                    ReceiveDescription = "Received bomb parts",
                    SendDescription = "Sent bomb parts",
                    IsMessage = false,
                    IsPackage = true,
                    IsMeeting = false,
                    IsBulletin = false,
                    Unknown1 = false,
                    ItemsToSecondary = false,
                    ReceivedObjectIds = new HashSet<int> { 0 },
                    DestroyedObjectIds = new HashSet<int>(),
                    Score = 50
                }
            },
            Objects = new List<CrimeModel.Object>
            {
                new CrimeModel.Object
                {
                    Name = "Bomb Parts",
                    PictureId = 3
                }
            },
            Metadata = new SharedMetadata
            {
                Name = $"Crime {id}",
                Comment = "Test crime data"
            }
        };
    }

    private static void WriteCrimeFile(string crimeDir, CrimeModel crime)
    {
        var json = JsonSerializer.Serialize(crime, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(crimeDir, $"CRIME{crime.Id}_crime.json"), json);
    }

    #endregion
}
