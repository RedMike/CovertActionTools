using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class IndexExporterImporterRoundtripTests : IDisposable
{
    private readonly IndexExporter _exporter;
    private readonly IndexImporter _importer;
    private readonly string _tempDir;

    public IndexExporterImporterRoundtripTests()
    {
        _exporter = new IndexExporter(NullLogger<IndexExporter>.Instance);
        _importer = new IndexImporter(NullLogger<IndexImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"IndexRoundtrip_{Guid.NewGuid():N}");
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
    public void Roundtrip_BasicIndex_AllPropertiesMatch()
    {
        var original = new PackageIndex
        {
            FormatVersion = 1,
            PackageVersion = new Version(1, 0, 0),
            Metadata = new SharedMetadata { Name = "TestPackage", Comment = "A test package" }
        };

        var result = ExportAndImport(original);

        Assert.Equal(1, result.FormatVersion);
        Assert.Equal(new Version(1, 0, 0), result.PackageVersion);
        Assert.Equal("TestPackage", result.Metadata.Name);
        Assert.Equal("A test package", result.Metadata.Comment);
    }

    [Fact]
    public void Roundtrip_VersionFields_PreservedCorrectly()
    {
        var original = new PackageIndex
        {
            FormatVersion = 5,
            PackageVersion = new Version(10, 3, 7),
            Metadata = new SharedMetadata { Name = "V", Comment = "" }
        };

        var result = ExportAndImport(original);

        Assert.Equal(5, result.FormatVersion);
        Assert.Equal(new Version(10, 3, 7), result.PackageVersion);
    }

    [Fact]
    public void Roundtrip_BooleanChangeFlags_PreservedCorrectly()
    {
        var original = new PackageIndex
        {
            FormatVersion = 1,
            PackageVersion = new Version(1, 0, 0),
            Metadata = new SharedMetadata(),
            TextChanges = true,
            ClueChanges = true,
            PlotChanges = true,
            FontChanges = true,
            ProseChanges = true,
            TextIncluded = true,
            ClueIncluded = true,
            PlotIncluded = true,
            FontIncluded = true,
            ProseIncluded = true
        };

        var result = ExportAndImport(original);

        Assert.True(result.TextChanges);
        Assert.True(result.ClueChanges);
        Assert.True(result.PlotChanges);
        Assert.True(result.FontChanges);
        Assert.True(result.ProseChanges);
        Assert.True(result.TextIncluded);
        Assert.True(result.ClueIncluded);
        Assert.True(result.PlotIncluded);
        Assert.True(result.FontIncluded);
        Assert.True(result.ProseIncluded);
    }

    [Fact]
    public void Roundtrip_HashSetChangeFields_PreservedCorrectly()
    {
        var original = new PackageIndex
        {
            FormatVersion = 1,
            PackageVersion = new Version(1, 0, 0),
            Metadata = new SharedMetadata(),
            SimpleImageChanges = new HashSet<string> { "IMG_001", "IMG_002", "IMG_003" },
            CrimeChanges = new HashSet<int> { 0, 1, 2 },
            WorldChanges = new HashSet<int> { 5 },
            AnimationChanges = new HashSet<string> { "WALK", "RUN" },
            CatalogChanges = new HashSet<string> { "CAT_A" },
            SimpleImageIncluded = new HashSet<string> { "IMG_001" },
            CrimeIncluded = new HashSet<int> { 0 },
            WorldIncluded = new HashSet<int> { 5 },
            AnimationIncluded = new HashSet<string> { "WALK" },
            CatalogIncluded = new HashSet<string> { "CAT_A" }
        };

        var result = ExportAndImport(original);

        Assert.Equal(3, result.SimpleImageChanges.Count);
        Assert.Contains("IMG_002", result.SimpleImageChanges);
        Assert.Equal(3, result.CrimeChanges.Count);
        Assert.Contains(2, result.CrimeChanges);
        Assert.Single(result.WorldChanges);
        Assert.Equal(2, result.AnimationChanges.Count);
        Assert.Single(result.CatalogChanges);
        Assert.Single(result.SimpleImageIncluded);
        Assert.Single(result.CrimeIncluded);
        Assert.Single(result.WorldIncluded);
        Assert.Single(result.AnimationIncluded);
        Assert.Single(result.CatalogIncluded);
    }

    [Fact]
    public void Roundtrip_DefaultValues_PreservedCorrectly()
    {
        var original = new PackageIndex
        {
            FormatVersion = 0,
            PackageVersion = new Version(0, 0, 0),
            Metadata = new SharedMetadata { Name = "", Comment = "" }
        };

        var result = ExportAndImport(original);

        Assert.Equal(0, result.FormatVersion);
        Assert.Equal(new Version(0, 0, 0), result.PackageVersion);
        Assert.Equal("", result.Metadata.Name);
        Assert.Equal("", result.Metadata.Comment);
        Assert.False(result.TextChanges);
        Assert.False(result.ClueChanges);
        Assert.False(result.PlotChanges);
        Assert.Empty(result.SimpleImageChanges);
        Assert.Empty(result.CrimeChanges);
    }

    #endregion

    #region Helpers

    private PackageIndex ExportAndImport(PackageIndex index)
    {
        var exportModel = new PackageModel { Index = index };
        _exporter.Start(_tempDir, exportModel);
        _exporter.RunStep();

        _importer.Start(_tempDir);
        _importer.RunStep();

        var importModel = new PackageModel();
        _importer.SetResult(importModel);
        return importModel.Index;
    }

    #endregion
}
