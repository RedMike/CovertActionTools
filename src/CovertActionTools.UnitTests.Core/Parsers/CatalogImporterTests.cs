using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class CatalogImporterTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonEnumOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    private readonly CatalogImporter _importer;
    private readonly string _tempDir;

    public CatalogImporterTests()
    {
        var imageImporter = new SharedImageImporter(NullLogger<SharedImageImporter>.Instance);
        _importer = new CatalogImporter(NullLogger<CatalogImporter>.Instance, imageImporter);
        _tempDir = Path.Combine(Path.GetTempPath(), $"CatalogImporterTests_{Guid.NewGuid():N}");
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
        Assert.Equal("Processing catalogs..", _importer.GetMessage());
    }

    #endregion

    #region CheckIfValid

    [Fact]
    public void CheckIfValid_WithCatalogJsonFiles_ReturnsTrue()
    {
        var catDir = Path.Combine(_tempDir, "catalog");
        Directory.CreateDirectory(catDir);
        File.WriteAllText(Path.Combine(catDir, "test_catalog.json"), "{}");

        Assert.True(_importer.CheckIfValid(_tempDir));
    }

    [Fact]
    public void CheckIfValid_EmptySubdirectory_ReturnsFalse()
    {
        var catDir = Path.Combine(_tempDir, "catalog");
        Directory.CreateDirectory(catDir);

        Assert.False(_importer.CheckIfValid(_tempDir));
    }

    [Fact]
    public void CheckIfValid_NoSubdirectory_Throws()
    {
        Assert.ThrowsAny<Exception>(() => _importer.CheckIfValid(_tempDir));
    }

    [Fact]
    public void CheckIfValid_WithNonCatalogJsonFiles_ReturnsFalse()
    {
        var catDir = Path.Combine(_tempDir, "catalog");
        Directory.CreateDirectory(catDir);
        File.WriteAllText(Path.Combine(catDir, "test_metadata.json"), "{}");

        Assert.False(_importer.CheckIfValid(_tempDir));
    }

    #endregion

    #region Import single item

    [Fact]
    public void Import_SingleCatalog_DeserializesCorrectly()
    {
        WriteCatalogFiles("cat1", new[] { "entry1" });

        _importer.Start(_tempDir);
        var done = _importer.RunStep();

        Assert.True(done);

        var result = new PackageModel();
        _importer.SetResult(result);

        Assert.Single(result.Catalogs);
        Assert.True(result.Catalogs.ContainsKey("cat1"));
        var catalog = result.Catalogs["cat1"];
        Assert.Equal("cat1", catalog.Key);
        Assert.Equal("Test Catalog cat1", catalog.Metadata.Name);
        Assert.Single(catalog.Data.Keys);
        Assert.Contains("entry1", catalog.Data.Keys);
    }

    [Fact]
    public void Import_SingleCatalog_ReadsEntryImages()
    {
        WriteCatalogFiles("cat1", new[] { "entry1", "entry2" });

        _importer.Start(_tempDir);
        _importer.RunStep();

        var result = new PackageModel();
        _importer.SetResult(result);

        var catalog = result.Catalogs["cat1"];
        Assert.Equal(2, catalog.Entries.Count);
        Assert.True(catalog.Entries.ContainsKey("entry1"));
        Assert.True(catalog.Entries.ContainsKey("entry2"));
        Assert.Equal(4, catalog.Entries["entry1"].Data.Width);
        Assert.Equal(4, catalog.Entries["entry1"].Data.Height);
    }

    #endregion

    #region Import multiple items

    [Fact]
    public void Import_MultipleCatalogs_AllDeserializedCorrectly()
    {
        WriteCatalogFiles("catA", new[] { "e1" });
        WriteCatalogFiles("catB", new[] { "e2" });

        _importer.Start(_tempDir);
        var done1 = _importer.RunStep();
        Assert.False(done1);

        var done2 = _importer.RunStep();
        Assert.True(done2);

        var result = new PackageModel();
        _importer.SetResult(result);

        Assert.Equal(2, result.Catalogs.Count);
        Assert.True(result.Catalogs.ContainsKey("catA"));
        Assert.True(result.Catalogs.ContainsKey("catB"));
    }

    #endregion

    #region SetResult populates correct field

    [Fact]
    public void SetResult_PopulatesCatalogsFieldOnPackageModel()
    {
        WriteCatalogFiles("cat1", new[] { "e1" });

        _importer.Start(_tempDir);
        _importer.RunStep();

        var packageModel = new PackageModel();
        _importer.SetResult(packageModel);

        Assert.NotEmpty(packageModel.Catalogs);
        Assert.True(packageModel.Catalogs.ContainsKey("cat1"));
    }

    #endregion

    #region Helpers

    private void WriteCatalogFiles(string key, string[] entryKeys)
    {
        var catDir = Path.Combine(_tempDir, "catalog");
        if (!Directory.Exists(catDir))
        {
            Directory.CreateDirectory(catDir);
        }

        var keyDir = Path.Combine(catDir, key);
        Directory.CreateDirectory(keyDir);

        // Metadata (CatalogImporter reads with JsonStringEnumConverter, but SharedMetadata has no enums)
        var metadata = new SharedMetadata
        {
            Name = $"Test Catalog {key}",
            Comment = "Test catalog data"
        };
        File.WriteAllText(Path.Combine(keyDir, $"{key}_metadata.json"),
            JsonSerializer.Serialize(metadata, JsonEnumOptions));

        // Catalog data (CatalogImporter reads with JsonStringEnumConverter, but CatalogData has no enums)
        var catalogData = new CatalogModel.CatalogData
        {
            Keys = new List<string>(entryKeys)
        };
        File.WriteAllText(Path.Combine(keyDir, $"{key}_catalog.json"),
            JsonSerializer.Serialize(catalogData, JsonEnumOptions));

        // Images
        var imagesDir = Path.Combine(keyDir, "images");
        Directory.CreateDirectory(imagesDir);

        foreach (var entryKey in entryKeys)
        {
            var imageData = new SharedImageModel.ImageData
            {
                Width = 4,
                Height = 4,
                CompressionDictionaryWidth = 11
            };
            File.WriteAllText(
                Path.Combine(imagesDir, $"{entryKey}_VGA_metadata.json"),
                JsonSerializer.Serialize(imageData, JsonOptions));

            var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
            var pngBytes = ImageConversion.VgaToTexture(4, 4, pixels);
            File.WriteAllBytes(
                Path.Combine(imagesDir, $"{entryKey}_VGA.png"),
                pngBytes);
        }
    }

    #endregion
}
