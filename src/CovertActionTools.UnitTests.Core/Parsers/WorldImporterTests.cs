using System.Text.Json;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class WorldImporterTests : IDisposable
{
    private readonly WorldImporter _importer;
    private readonly string _tempDir;

    public WorldImporterTests()
    {
        _importer = new WorldImporter(NullLogger<WorldImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"WorldImporterTests_{Guid.NewGuid():N}");
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
        Assert.Equal("Processing worlds..", _importer.GetMessage());
    }

    #endregion

    #region CheckIfValid

    [Fact]
    public void CheckIfValid_WithWorldFiles_ReturnsTrue()
    {
        var worldDir = Path.Combine(_tempDir, "world");
        Directory.CreateDirectory(worldDir);
        WriteWorldFile(worldDir, CreateSampleWorld(0));

        Assert.True(_importer.CheckIfValid(_tempDir));
    }

    [Fact]
    public void CheckIfValid_EmptySubdirectory_ReturnsFalse()
    {
        var worldDir = Path.Combine(_tempDir, "world");
        Directory.CreateDirectory(worldDir);

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
    public void Import_SingleWorld_DeserializesCorrectly()
    {
        var worldDir = Path.Combine(_tempDir, "world");
        Directory.CreateDirectory(worldDir);
        WriteWorldFile(worldDir, CreateSampleWorld(2));

        _importer.Start(_tempDir);
        var done = _importer.RunStep();

        Assert.True(done);

        var result = new PackageModel();
        _importer.SetResult(result);

        Assert.Single(result.Worlds);
        Assert.True(result.Worlds.ContainsKey(2));
        var world = result.Worlds[2];
        Assert.Equal(2, world.Id);
        Assert.Equal(2, world.Cities.Count);
        Assert.Equal("Washington", world.Cities[0].Name);
        Assert.Equal("USA", world.Cities[0].Country);
    }

    #endregion

    #region Import multiple items

    [Fact]
    public void Import_MultipleWorlds_AllDeserializedCorrectly()
    {
        var worldDir = Path.Combine(_tempDir, "world");
        Directory.CreateDirectory(worldDir);
        WriteWorldFile(worldDir, CreateSampleWorld(0));
        WriteWorldFile(worldDir, CreateSampleWorld(3));

        _importer.Start(_tempDir);
        var done1 = _importer.RunStep();
        Assert.False(done1);

        var done2 = _importer.RunStep();
        Assert.True(done2);

        var result = new PackageModel();
        _importer.SetResult(result);

        Assert.Equal(2, result.Worlds.Count);
        Assert.True(result.Worlds.ContainsKey(0));
        Assert.True(result.Worlds.ContainsKey(3));
    }

    #endregion

    #region SetResult populates correct field

    [Fact]
    public void SetResult_PopulatesWorldsFieldOnPackageModel()
    {
        var worldDir = Path.Combine(_tempDir, "world");
        Directory.CreateDirectory(worldDir);
        WriteWorldFile(worldDir, CreateSampleWorld(7));

        _importer.Start(_tempDir);
        _importer.RunStep();

        var packageModel = new PackageModel();
        _importer.SetResult(packageModel);

        Assert.NotEmpty(packageModel.Worlds);
        Assert.True(packageModel.Worlds.ContainsKey(7));
    }

    #endregion

    #region Organisation and City data preserved

    [Fact]
    public void Import_PreservesOrganisationAndCityData()
    {
        var worldDir = Path.Combine(_tempDir, "world");
        Directory.CreateDirectory(worldDir);
        WriteWorldFile(worldDir, CreateSampleWorld(1));

        _importer.Start(_tempDir);
        _importer.RunStep();

        var result = new PackageModel();
        _importer.SetResult(result);

        var world = result.Worlds[1];
        Assert.Equal(2, world.Cities.Count);
        Assert.Equal(100, world.Cities[0].MapX);
        Assert.Equal(50, world.Cities[0].MapY);
        Assert.Equal("London", world.Cities[1].Name);
        Assert.Equal(200, world.Cities[1].MapX);

        Assert.Single(world.Organisations);
        Assert.Equal("CIA", world.Organisations[0].ShortName);
        Assert.Equal("Central Intelligence Agency", world.Organisations[0].LongName);
        Assert.Equal(10, world.Organisations[0].UniqueId);
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

    private static void WriteWorldFile(string worldDir, WorldModel world)
    {
        var json = JsonSerializer.Serialize(world, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(worldDir, $"WORLD{world.Id}_world.json"), json);
    }

    #endregion
}
