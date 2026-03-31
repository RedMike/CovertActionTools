using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class CatalogExporterImporterRoundtripTests : IDisposable
{
    private readonly CatalogExporter _exporter;
    private readonly CatalogImporter _importer;
    private readonly string _tempDir;

    public CatalogExporterImporterRoundtripTests()
    {
        var compression = new LzwCompression(NullLogger<LzwCompression>.Instance);
        var imageExporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, compression);
        var imageImporter = new SharedImageImporter(NullLogger<SharedImageImporter>.Instance);
        _exporter = new CatalogExporter(NullLogger<CatalogExporter>.Instance, imageExporter);
        _importer = new CatalogImporter(NullLogger<CatalogImporter>.Instance, imageImporter);
        _tempDir = Path.Combine(Path.GetTempPath(), $"CatalogRoundtrip_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Roundtrip tests

    [Fact]
    public void Roundtrip_SingleCatalog_MetadataPreserved()
    {
        var original = new Dictionary<string, CatalogModel>
        {
            ["cat1"] = CreateSampleCatalog("cat1", new[] { "entry1" })
        };

        var result = ExportAndImport(original);

        Assert.Single(result);
        var catalog = result["cat1"];
        Assert.Equal("cat1", catalog.Key);
        Assert.Equal("Test Catalog cat1", catalog.Metadata.Name);
        Assert.Equal("Test catalog data", catalog.Metadata.Comment);
    }

    [Fact]
    public void Roundtrip_SingleCatalog_CatalogDataKeysPreserved()
    {
        var original = new Dictionary<string, CatalogModel>
        {
            ["cat1"] = CreateSampleCatalog("cat1", new[] { "entry1", "entry2", "entry3" })
        };

        var result = ExportAndImport(original);

        var catalog = result["cat1"];
        Assert.Equal(3, catalog.Data.Keys.Count);
        Assert.Contains("entry1", catalog.Data.Keys);
        Assert.Contains("entry2", catalog.Data.Keys);
        Assert.Contains("entry3", catalog.Data.Keys);
    }

    [Fact]
    public void Roundtrip_SingleCatalog_ImagePixelsPreserved()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4, seed: 42);
        var catalog = CreateSampleCatalog("cat1", new[] { "entry1" }, pixels);

        var original = new Dictionary<string, CatalogModel> { ["cat1"] = catalog };

        var result = ExportAndImport(original);

        var resultCatalog = result["cat1"];
        Assert.Single(resultCatalog.Entries);
        Assert.True(resultCatalog.Entries.ContainsKey("entry1"));
        Assert.Equal(pixels, resultCatalog.Entries["entry1"].RawVgaImageData);
    }

    [Fact]
    public void Roundtrip_SingleCatalog_ImageDimensionsPreserved()
    {
        var original = new Dictionary<string, CatalogModel>
        {
            ["cat1"] = CreateSampleCatalog("cat1", new[] { "entry1" })
        };

        var result = ExportAndImport(original);

        var image = result["cat1"].Entries["entry1"];
        Assert.Equal(4, image.Data.Width);
        Assert.Equal(4, image.Data.Height);
        Assert.Equal(11, image.Data.CompressionDictionaryWidth);
    }

    [Fact]
    public void Roundtrip_MultipleCatalogs_AllPreserved()
    {
        var original = new Dictionary<string, CatalogModel>
        {
            ["catA"] = CreateSampleCatalog("catA", new[] { "e1", "e2" }),
            ["catB"] = CreateSampleCatalog("catB", new[] { "e3" })
        };

        var result = ExportAndImport(original);

        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("catA"));
        Assert.True(result.ContainsKey("catB"));
        Assert.Equal(2, result["catA"].Entries.Count);
        Assert.Single(result["catB"].Entries);
        Assert.Equal("Test Catalog catA", result["catA"].Metadata.Name);
        Assert.Equal("Test Catalog catB", result["catB"].Metadata.Name);
    }

    [Fact]
    public void Roundtrip_CatalogWithMultipleEntries_AllImagePixelsPreserved()
    {
        var catalog = new CatalogModel
        {
            Key = "cat1",
            Metadata = new SharedMetadata { Name = "Multi Entry Catalog", Comment = "Multiple entries" },
            Data = new CatalogModel.CatalogData { Keys = new List<string> { "img1", "img2" } },
            Entries = new Dictionary<string, SharedImageModel>()
        };

        var pixels1 = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4, seed: 10);
        catalog.Entries["img1"] = CreateTestImage(4, 4, pixels1);

        var pixels2 = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4, seed: 20);
        catalog.Entries["img2"] = CreateTestImage(4, 4, pixels2);

        var original = new Dictionary<string, CatalogModel> { ["cat1"] = catalog };

        var result = ExportAndImport(original);

        Assert.Equal(pixels1, result["cat1"].Entries["img1"].RawVgaImageData);
        Assert.Equal(pixels2, result["cat1"].Entries["img2"].RawVgaImageData);
    }

    #endregion

    #region Helpers

    private Dictionary<string, CatalogModel> ExportAndImport(Dictionary<string, CatalogModel> catalogs)
    {
        var exportModel = new PackageModel { Catalogs = catalogs };
        _exporter.Start(_tempDir, exportModel);
        while (!_exporter.RunStep()) { }

        _importer.Start(_tempDir);
        while (!_importer.RunStep()) { }

        var importModel = new PackageModel();
        _importer.SetResult(importModel);
        return importModel.Catalogs;
    }

    private static CatalogModel CreateSampleCatalog(string key, string[] entryKeys, byte[]? sharedPixels = null)
    {
        var catalog = new CatalogModel
        {
            Key = key,
            Metadata = new SharedMetadata
            {
                Name = $"Test Catalog {key}",
                Comment = "Test catalog data"
            },
            Data = new CatalogModel.CatalogData
            {
                Keys = new List<string>(entryKeys)
            },
            Entries = new Dictionary<string, SharedImageModel>()
        };

        foreach (var entryKey in entryKeys)
        {
            var pixels = sharedPixels ?? SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
            catalog.Entries[entryKey] = CreateTestImage(4, 4, pixels);
        }

        return catalog;
    }

    private static SharedImageModel CreateTestImage(int width, int height, byte[]? pixels = null)
    {
        if (pixels == null)
        {
            pixels = new byte[width * height];
            Array.Fill(pixels, (byte)5);
        }

        return new SharedImageModel
        {
            Data = new SharedImageModel.ImageData
            {
                Width = width,
                Height = height,
                CompressionDictionaryWidth = 11
            },
            RawVgaImageData = pixels,
            VgaImageData = new byte[width * height * 4],
            CgaImageData = Array.Empty<byte>()
        };
    }

    #endregion
}
