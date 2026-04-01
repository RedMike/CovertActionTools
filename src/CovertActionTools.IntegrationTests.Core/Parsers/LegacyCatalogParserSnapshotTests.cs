using System;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.IntegrationTests.Core.Parsers.Data;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class LegacyCatalogParserSnapshotTests : IDisposable
{
    private readonly LegacyCatalogParser _parser;
    private readonly string _tempDir;

    public LegacyCatalogParserSnapshotTests()
    {
        var decompression = new LzwDecompression(NullLogger<LzwDecompression>.Instance);
        var imageParser = new SharedImageParser(NullLogger<SharedImageParser>.Instance, decompression);
        _parser = new LegacyCatalogParser(NullLogger<LegacyCatalogParser>.Instance, imageParser);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyCatalogSnapshotTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Export snapshot tests (binary format stability)

    [Fact]
    public void ExportSnapshot_SingleEntry_Uniform()
    {
        var catData = CatalogIntegrationTestDataGenerator.BuildCatalogFile(new[]
        {
            new CatalogIntegrationTestDataGenerator.CatalogEntry
            {
                Name = "IMG1",
                Width = 4,
                Height = 4,
                Pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4)
            }
        });
        var exported = Convert.ToBase64String(catData);
        Assert.Equal(CatalogSnapshotData.CatFile_SingleEntry_Uniform, exported);
    }

    [Fact]
    public void ExportSnapshot_TwoEntries_Mixed()
    {
        var catData = CatalogIntegrationTestDataGenerator.BuildCatalogFile(new[]
        {
            new CatalogIntegrationTestDataGenerator.CatalogEntry
            {
                Name = "A",
                Width = 4,
                Height = 4,
                Pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4)
            },
            new CatalogIntegrationTestDataGenerator.CatalogEntry
            {
                Name = "B",
                Width = 4,
                Height = 4,
                Pixels = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4)
            }
        });
        var exported = Convert.ToBase64String(catData);
        Assert.Equal(CatalogSnapshotData.CatFile_TwoEntries_Mixed, exported);
    }

    #endregion

    #region Parse snapshot tests (parsed model properties from known binary)

    [Fact]
    public void ParseSnapshot_SingleEntry_HasCorrectEntryName()
    {
        WriteCatFromSnapshot("TESTCAT", CatalogSnapshotData.CatFile_SingleEntry_Uniform);

        var catalogs = RunParser();

        Assert.True(catalogs["TESTCAT"].Entries.ContainsKey("IMG1"));
    }

    [Fact]
    public void ParseSnapshot_SingleEntry_HasCorrectPixels()
    {
        WriteCatFromSnapshot("TESTCAT", CatalogSnapshotData.CatFile_SingleEntry_Uniform);

        var catalogs = RunParser();

        var expected = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        Assert.Equal(expected, catalogs["TESTCAT"].Entries["IMG1"].RawVgaImageData);
    }

    [Fact]
    public void ParseSnapshot_SingleEntry_HasCorrectDimensions()
    {
        WriteCatFromSnapshot("TESTCAT", CatalogSnapshotData.CatFile_SingleEntry_Uniform);

        var catalogs = RunParser();

        var image = catalogs["TESTCAT"].Entries["IMG1"];
        Assert.Equal(4, image.Data.Width);
        Assert.Equal(4, image.Data.Height);
    }

    [Fact]
    public void ParseSnapshot_TwoEntries_HasBothEntries()
    {
        WriteCatFromSnapshot("TESTCAT", CatalogSnapshotData.CatFile_TwoEntries_Mixed);

        var catalogs = RunParser();

        Assert.Equal(2, catalogs["TESTCAT"].Entries.Count);
        Assert.True(catalogs["TESTCAT"].Entries.ContainsKey("A"));
        Assert.True(catalogs["TESTCAT"].Entries.ContainsKey("B"));
    }

    [Fact]
    public void ParseSnapshot_TwoEntries_HasCorrectPixelsForEachEntry()
    {
        WriteCatFromSnapshot("TESTCAT", CatalogSnapshotData.CatFile_TwoEntries_Mixed);

        var catalogs = RunParser();

        var expectedA = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        var expectedB = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4);
        Assert.Equal(expectedA, catalogs["TESTCAT"].Entries["A"].RawVgaImageData);
        Assert.Equal(expectedB, catalogs["TESTCAT"].Entries["B"].RawVgaImageData);
    }

    #endregion

    #region Helpers

    private void WriteCatFromSnapshot(string key, string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        File.WriteAllBytes(Path.Combine(_tempDir, $"{key}.CAT"), bytes);
    }

    private System.Collections.Generic.Dictionary<string, CatalogModel> RunParser()
    {
        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);
        return model.Catalogs;
    }

    #endregion
}
