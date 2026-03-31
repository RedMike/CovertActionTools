using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class WorldExporterImporterRoundtripTests : IDisposable
{
    private readonly WorldExporter _exporter;
    private readonly WorldImporter _importer;
    private readonly string _tempDir;

    public WorldExporterImporterRoundtripTests()
    {
        _exporter = new WorldExporter(NullLogger<WorldExporter>.Instance);
        _importer = new WorldImporter(NullLogger<WorldImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"WorldRoundtrip_{Guid.NewGuid():N}");
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
    public void Roundtrip_SingleWorld_AllPropertiesMatch()
    {
        var original = CreateDetailedWorld(0);
        var result = ExportAndImportModel(original);

        Assert.Single(result.Worlds);
        var world = result.Worlds[0];

        Assert.Equal(0, world.Id);
        Assert.Equal("World 0", world.Metadata.Name);
        Assert.Equal("Detailed test world", world.Metadata.Comment);

        Assert.Equal(3, world.Cities.Count);
        AssertCityMatches(original.Worlds[0].Cities[0], world.Cities[0]);
        AssertCityMatches(original.Worlds[0].Cities[1], world.Cities[1]);
        AssertCityMatches(original.Worlds[0].Cities[2], world.Cities[2]);

        Assert.Equal(2, world.Organisations.Count);
        AssertOrganisationMatches(original.Worlds[0].Organisations[0], world.Organisations[0]);
        AssertOrganisationMatches(original.Worlds[0].Organisations[1], world.Organisations[1]);
    }

    #endregion

    #region Multiple items roundtrip

    [Fact]
    public void Roundtrip_MultipleWorlds_AllPreserved()
    {
        var world0 = CreateDetailedWorld(0);
        var world2 = CreateDetailedWorld(2);
        var world5 = CreateDetailedWorld(5);

        var packageModel = new PackageModel
        {
            Worlds = new Dictionary<int, WorldModel>
            {
                [0] = world0.Worlds[0],
                [2] = world2.Worlds[2],
                [5] = world5.Worlds[5]
            }
        };

        var result = ExportAndImportModel(packageModel);

        Assert.Equal(3, result.Worlds.Count);
        Assert.True(result.Worlds.ContainsKey(0));
        Assert.True(result.Worlds.ContainsKey(2));
        Assert.True(result.Worlds.ContainsKey(5));

        Assert.Equal(0, result.Worlds[0].Id);
        Assert.Equal(2, result.Worlds[2].Id);
        Assert.Equal(5, result.Worlds[5].Id);
    }

    #endregion

    #region Edge case roundtrips

    [Fact]
    public void Roundtrip_WorldWithEmptyCollections_Preserved()
    {
        var world = new WorldModel
        {
            Id = 9,
            Cities = new List<WorldModel.City>(),
            Organisations = new List<WorldModel.Organisation>(),
            Metadata = new SharedMetadata { Name = "Empty world", Comment = "" }
        };

        var model = new PackageModel
        {
            Worlds = new Dictionary<int, WorldModel> { [9] = world }
        };

        var result = ExportAndImportModel(model);

        Assert.Single(result.Worlds);
        var imported = result.Worlds[9];
        Assert.Empty(imported.Cities);
        Assert.Empty(imported.Organisations);
    }

    [Fact]
    public void Roundtrip_WorldWithDisallowedMastermindOrg_Preserved()
    {
        var world = new WorldModel
        {
            Id = 6,
            Cities = new List<WorldModel.City>
            {
                new WorldModel.City
                {
                    Name = "Berlin",
                    Country = "Germany",
                    MapX = 150,
                    MapY = 40
                }
            },
            Organisations = new List<WorldModel.Organisation>
            {
                new WorldModel.Organisation
                {
                    ShortName = "BND",
                    LongName = "Bundesnachrichtendienst",
                    UniqueId = 0xFF,
                    Unknown1 = 0,
                    Unknown2 = 0,
                    Unknown3 = 0,
                    Unknown4 = 0
                }
            },
            Metadata = new SharedMetadata { Name = "German world", Comment = "Org with 0xFF UniqueId" }
        };

        var model = new PackageModel
        {
            Worlds = new Dictionary<int, WorldModel> { [6] = world }
        };

        var result = ExportAndImportModel(model);

        var imported = result.Worlds[6];
        Assert.Equal(0xFF, imported.Organisations[0].UniqueId);
        Assert.False(imported.Organisations[0].AllowMastermind);
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

    private static PackageModel CreateDetailedWorld(int id)
    {
        var world = new WorldModel
        {
            Id = id,
            Cities = new List<WorldModel.City>
            {
                new WorldModel.City
                {
                    Name = "Washington",
                    Country = "USA",
                    Unknown1 = 0,
                    Unknown2 = 1,
                    MapX = 80,
                    MapY = 55
                },
                new WorldModel.City
                {
                    Name = "Moscow",
                    Country = "Russia",
                    Unknown1 = 2,
                    Unknown2 = 3,
                    MapX = 250,
                    MapY = 45
                },
                new WorldModel.City
                {
                    Name = "Tokyo",
                    Country = "Japan",
                    Unknown1 = 0,
                    Unknown2 = 0,
                    MapX = 300,
                    MapY = 60
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
                },
                new WorldModel.Organisation
                {
                    ShortName = "KGB",
                    LongName = "Committee State Security",
                    Unknown1 = 3,
                    Unknown2 = 4,
                    Unknown3 = 5,
                    UniqueId = 20,
                    Unknown4 = 1
                }
            },
            Metadata = new SharedMetadata
            {
                Name = $"World {id}",
                Comment = "Detailed test world"
            }
        };

        return new PackageModel
        {
            Worlds = new Dictionary<int, WorldModel> { [id] = world }
        };
    }

    private static void AssertCityMatches(WorldModel.City expected, WorldModel.City actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Country, actual.Country);
        Assert.Equal(expected.Unknown1, actual.Unknown1);
        Assert.Equal(expected.Unknown2, actual.Unknown2);
        Assert.Equal(expected.MapX, actual.MapX);
        Assert.Equal(expected.MapY, actual.MapY);
    }

    private static void AssertOrganisationMatches(WorldModel.Organisation expected, WorldModel.Organisation actual)
    {
        Assert.Equal(expected.ShortName, actual.ShortName);
        Assert.Equal(expected.LongName, actual.LongName);
        Assert.Equal(expected.Unknown1, actual.Unknown1);
        Assert.Equal(expected.Unknown2, actual.Unknown2);
        Assert.Equal(expected.Unknown3, actual.Unknown3);
        Assert.Equal(expected.UniqueId, actual.UniqueId);
        Assert.Equal(expected.Unknown4, actual.Unknown4);
    }

    #endregion
}
