using System.Text.Json;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class WorldExporterTests : IDisposable
{
    private readonly WorldExporter _exporter;
    private readonly string _tempDir;

    public WorldExporterTests()
    {
        _exporter = new WorldExporter(NullLogger<WorldExporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"WorldExporterTests_{Guid.NewGuid():N}");
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
        Assert.Equal("Processing worlds..", _exporter.GetMessage());
    }

    #endregion

    #region Export creates subdirectory and writes files

    [Fact]
    public void Export_SingleWorld_CreatesWorldSubdirectory()
    {
        var model = CreatePackageModel(CreateSampleWorld(0));

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        Assert.True(Directory.Exists(Path.Combine(_tempDir, "world")));
    }

    [Fact]
    public void Export_SingleWorld_WritesCorrectFile()
    {
        var model = CreatePackageModel(CreateSampleWorld(0));

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "world", "WORLD0_world.json");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Export_SingleWorld_FileDeserializesCorrectly()
    {
        var world = CreateSampleWorld(2);
        var model = CreatePackageModel(world);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "world", "WORLD2_world.json"));
        var result = JsonSerializer.Deserialize<WorldModel>(json);

        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal(2, result.Cities.Count);
        Assert.Equal("Washington", result.Cities[0].Name);
        Assert.Equal("USA", result.Cities[0].Country);
        Assert.Equal(100, result.Cities[0].MapX);
        Assert.Equal(50, result.Cities[0].MapY);
        Assert.Single(result.Organisations);
        Assert.Equal("CIA", result.Organisations[0].ShortName);
        Assert.Equal("Central Intelligence Agency", result.Organisations[0].LongName);
    }

    #endregion

    #region Multiple worlds

    [Fact]
    public void Export_MultipleWorlds_WritesOneFilePerWorld()
    {
        var model = CreatePackageModel(CreateSampleWorld(0), CreateSampleWorld(4));

        _exporter.Start(_tempDir, model);
        var done1 = _exporter.RunStep();
        Assert.False(done1);

        var done2 = _exporter.RunStep();
        Assert.True(done2);

        Assert.True(File.Exists(Path.Combine(_tempDir, "world", "WORLD0_world.json")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "world", "WORLD4_world.json")));
    }

    [Fact]
    public void Export_MultipleWorlds_EachFileDeserializesWithCorrectId()
    {
        var model = CreatePackageModel(CreateSampleWorld(1), CreateSampleWorld(5));

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();
        _exporter.RunStep();

        var world1 = JsonSerializer.Deserialize<WorldModel>(
            File.ReadAllText(Path.Combine(_tempDir, "world", "WORLD1_world.json")));
        var world5 = JsonSerializer.Deserialize<WorldModel>(
            File.ReadAllText(Path.Combine(_tempDir, "world", "WORLD5_world.json")));

        Assert.Equal(1, world1!.Id);
        Assert.Equal(5, world5!.Id);
    }

    #endregion

    #region GetItemCount tracks progress

    [Fact]
    public void GetItemCount_TracksProgressCorrectly()
    {
        var model = CreatePackageModel(CreateSampleWorld(0), CreateSampleWorld(1), CreateSampleWorld(2));

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
    public void RunStep_SingleWorld_ReturnsTrueImmediately()
    {
        var model = CreatePackageModel(CreateSampleWorld(0));

        _exporter.Start(_tempDir, model);
        var done = _exporter.RunStep();

        Assert.True(done);
    }

    #endregion

    #region Helpers

    private static WorldModel CreateSampleWorld(int id)
    {
        return new WorldModel
        {
            Id = id,
            Cities = new List<WorldModel.City>
            {
                new WorldModel.City
                {
                    Name = "Washington",
                    Country = "USA",
                    Unknown1 = 0,
                    Unknown2 = 0,
                    MapX = 100,
                    MapY = 50
                },
                new WorldModel.City
                {
                    Name = "London",
                    Country = "UK",
                    Unknown1 = 1,
                    Unknown2 = 2,
                    MapX = 200,
                    MapY = 30
                }
            },
            Organisations = new List<WorldModel.Organisation>
            {
                new WorldModel.Organisation
                {
                    ShortName = "CIA",
                    LongName = "Central Intelligence Agency",
                    Unknown1 = 0,
                    Unknown2 = 1,
                    Unknown3 = 2,
                    UniqueId = 10,
                    Unknown4 = 0
                }
            },
            Metadata = new SharedMetadata
            {
                Name = $"World {id}",
                Comment = "Test world data"
            }
        };
    }

    private static PackageModel CreatePackageModel(params WorldModel[] worlds)
    {
        var dict = new Dictionary<int, WorldModel>();
        foreach (var world in worlds)
        {
            dict[world.Id] = world;
        }

        return new PackageModel { Worlds = dict };
    }

    #endregion
}
