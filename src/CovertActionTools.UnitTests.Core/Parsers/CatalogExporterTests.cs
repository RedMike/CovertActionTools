using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Parsers.Stubs;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class CatalogExporterTests : IDisposable
{
    private readonly CatalogExporter _exporter;
    private readonly string _tempDir;

    public CatalogExporterTests()
    {
        var stubCompression = new StubLzwCompression();
        var imageExporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, stubCompression);
        _exporter = new CatalogExporter(NullLogger<CatalogExporter>.Instance, imageExporter);
        _tempDir = Path.Combine(Path.GetTempPath(), $"CatalogExporterTests_{Guid.NewGuid():N}");
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
        Assert.Equal("Processing catalogs..", _exporter.GetMessage());
    }

    #endregion

    #region Export creates subdirectory and writes files

    [Fact]
    public void Export_SingleCatalog_CreatesCatalogSubdirectory()
    {
        var model = CreatePackageModel(CreateSampleCatalog("cat1", new[] { "entry1" }));

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        Assert.True(Directory.Exists(Path.Combine(_tempDir, "catalog")));
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "catalog", "cat1")));
    }

    [Fact]
    public void Export_SingleCatalog_WritesMetadataFile()
    {
        var model = CreatePackageModel(CreateSampleCatalog("cat1", new[] { "entry1" }));

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "catalog", "cat1", "cat1_metadata.json");
        Assert.True(File.Exists(filePath));

        var json = File.ReadAllText(filePath);
        var metadata = JsonSerializer.Deserialize<SharedMetadata>(json);
        Assert.NotNull(metadata);
        Assert.Equal("Test Catalog cat1", metadata.Name);
    }

    [Fact]
    public void Export_SingleCatalog_WritesCatalogDataFile()
    {
        var catalog = CreateSampleCatalog("cat1", new[] { "entry1", "entry2" });
        var model = CreatePackageModel(catalog);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "catalog", "cat1", "cat1_catalog.json");
        Assert.True(File.Exists(filePath));

        var json = File.ReadAllText(filePath);
        var catalogData = JsonSerializer.Deserialize<CatalogModel.CatalogData>(json);
        Assert.NotNull(catalogData);
        Assert.Equal(2, catalogData.Keys.Count);
        Assert.Contains("entry1", catalogData.Keys);
        Assert.Contains("entry2", catalogData.Keys);
    }

    [Fact]
    public void Export_SingleCatalog_WritesImageFiles()
    {
        var catalog = CreateSampleCatalog("cat1", new[] { "entry1", "entry2" });
        var model = CreatePackageModel(catalog);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var imagesPath = Path.Combine(_tempDir, "catalog", "cat1", "images");
        Assert.True(Directory.Exists(imagesPath));
        Assert.True(File.Exists(Path.Combine(imagesPath, "entry1_VGA_metadata.json")));
        Assert.True(File.Exists(Path.Combine(imagesPath, "entry1_VGA.png")));
        Assert.True(File.Exists(Path.Combine(imagesPath, "entry2_VGA_metadata.json")));
        Assert.True(File.Exists(Path.Combine(imagesPath, "entry2_VGA.png")));
    }

    [Fact]
    public void Export_SingleCatalog_ImageMetadataDeserializesCorrectly()
    {
        var catalog = CreateSampleCatalog("cat1", new[] { "img1" });
        var model = CreatePackageModel(catalog);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var metaPath = Path.Combine(_tempDir, "catalog", "cat1", "images", "img1_VGA_metadata.json");
        var json = File.ReadAllText(metaPath);
        var imageData = JsonSerializer.Deserialize<SharedImageModel.ImageData>(json);
        Assert.NotNull(imageData);
        Assert.Equal(4, imageData.Width);
        Assert.Equal(4, imageData.Height);
    }

    #endregion

    #region Multiple catalogs

    [Fact]
    public void Export_MultipleCatalogs_WritesOneSubdirectoryPerCatalog()
    {
        var model = CreatePackageModel(
            CreateSampleCatalog("catA", new[] { "e1" }),
            CreateSampleCatalog("catB", new[] { "e2" }));

        _exporter.Start(_tempDir, model);
        var done1 = _exporter.RunStep();
        Assert.False(done1);

        var done2 = _exporter.RunStep();
        Assert.True(done2);

        Assert.True(Directory.Exists(Path.Combine(_tempDir, "catalog", "catA")));
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "catalog", "catB")));
    }

    #endregion

    #region GetItemCount tracks progress

    [Fact]
    public void GetItemCount_TracksProgressCorrectly()
    {
        var model = CreatePackageModel(
            CreateSampleCatalog("c1", new[] { "e1" }),
            CreateSampleCatalog("c2", new[] { "e2" }),
            CreateSampleCatalog("c3", new[] { "e3" }));

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
    public void RunStep_SingleCatalog_ReturnsTrueImmediately()
    {
        var model = CreatePackageModel(CreateSampleCatalog("c1", new[] { "e1" }));

        _exporter.Start(_tempDir, model);
        var done = _exporter.RunStep();

        Assert.True(done);
    }

    #endregion

    #region Helpers

    private static CatalogModel CreateSampleCatalog(string key, string[] entryKeys)
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
            catalog.Entries[entryKey] = CreateTestImage(4, 4);
        }

        return catalog;
    }

    private static SharedImageModel CreateTestImage(int width, int height)
    {
        var pixels = new byte[width * height];
        Array.Fill(pixels, (byte)5);
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

    private static PackageModel CreatePackageModel(params CatalogModel[] catalogs)
    {
        var dict = new Dictionary<string, CatalogModel>();
        foreach (var catalog in catalogs)
        {
            dict[catalog.Key] = catalog;
        }

        return new PackageModel { Catalogs = dict };
    }

    #endregion
}
