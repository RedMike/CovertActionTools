using System.Text.Json;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class CrimeExporterTests : IDisposable
{
    private readonly CrimeExporter _exporter;
    private readonly string _tempDir;

    public CrimeExporterTests()
    {
        _exporter = new CrimeExporter(NullLogger<CrimeExporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"CrimeExporterTests_{Guid.NewGuid():N}");
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
        Assert.Equal("Processing crimes..", _exporter.GetMessage());
    }

    #endregion

    #region Export creates subdirectory and writes files

    [Fact]
    public void Export_SingleCrime_CreatesCrimeSubdirectory()
    {
        var model = CreatePackageModel(CreateSampleCrime(0));

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        Assert.True(Directory.Exists(Path.Combine(_tempDir, "crime")));
    }

    [Fact]
    public void Export_SingleCrime_WritesCorrectFile()
    {
        var model = CreatePackageModel(CreateSampleCrime(0));

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "crime", "CRIME0_crime.json");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Export_SingleCrime_FileDeserializesCorrectly()
    {
        var crime = CreateSampleCrime(5);
        var model = CreatePackageModel(crime);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "crime", "CRIME5_crime.json"));
        var result = JsonSerializer.Deserialize<CrimeModel>(json);

        Assert.NotNull(result);
        Assert.Equal(5, result.Id);
        Assert.Equal(2, result.Participants.Count);
        Assert.Equal("Mastermind", result.Participants[0].Role);
        Assert.True(result.Participants[0].IsMastermind);
        Assert.Equal(ClueType.Vehicle, result.Participants[0].ClueType);
        Assert.Single(result.Events);
        Assert.Equal("Bomb Parts", result.Objects[0].Name);
    }

    #endregion

    #region Multiple crimes

    [Fact]
    public void Export_MultipleCrimes_WritesOneFilePerCrime()
    {
        var model = CreatePackageModel(CreateSampleCrime(0), CreateSampleCrime(3));

        _exporter.Start(_tempDir, model);
        var done1 = _exporter.RunStep();
        Assert.False(done1);

        var done2 = _exporter.RunStep();
        Assert.True(done2);

        Assert.True(File.Exists(Path.Combine(_tempDir, "crime", "CRIME0_crime.json")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "crime", "CRIME3_crime.json")));
    }

    [Fact]
    public void Export_MultipleCrimes_EachFileDeserializesWithCorrectId()
    {
        var model = CreatePackageModel(CreateSampleCrime(1), CreateSampleCrime(7));

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();
        _exporter.RunStep();

        var crime1 = JsonSerializer.Deserialize<CrimeModel>(
            File.ReadAllText(Path.Combine(_tempDir, "crime", "CRIME1_crime.json")));
        var crime7 = JsonSerializer.Deserialize<CrimeModel>(
            File.ReadAllText(Path.Combine(_tempDir, "crime", "CRIME7_crime.json")));

        Assert.Equal(1, crime1!.Id);
        Assert.Equal(7, crime7!.Id);
    }

    #endregion

    #region GetItemCount tracks progress

    [Fact]
    public void GetItemCount_TracksProgressCorrectly()
    {
        var model = CreatePackageModel(CreateSampleCrime(0), CreateSampleCrime(1), CreateSampleCrime(2));

        _exporter.Start(_tempDir, model);
        var (current0, total0) = _exporter.GetItemCount();
        Assert.Equal(0, current0);
        Assert.Equal(3, total0);

        _exporter.RunStep();
        var (current1, total1) = _exporter.GetItemCount();
        Assert.Equal(0, current1);
        Assert.Equal(3, total1);

        _exporter.RunStep();
        var (current2, total2) = _exporter.GetItemCount();
        Assert.Equal(1, current2);
        Assert.Equal(3, total2);
    }

    #endregion

    #region RunStep completion

    [Fact]
    public void RunStep_SingleCrime_ReturnsTrueImmediately()
    {
        var model = CreatePackageModel(CreateSampleCrime(0));

        _exporter.Start(_tempDir, model);
        var done = _exporter.RunStep();

        Assert.True(done);
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

    private static PackageModel CreatePackageModel(params CrimeModel[] crimes)
    {
        var dict = new Dictionary<int, CrimeModel>();
        foreach (var crime in crimes)
        {
            dict[crime.Id] = crime;
        }

        return new PackageModel { Crimes = dict };
    }

    #endregion
}
