using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class IndexExporterTests : IDisposable
{
    private readonly IndexExporter _exporter;
    private readonly string _tempDir;

    public IndexExporterTests()
    {
        _exporter = new IndexExporter(NullLogger<IndexExporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"IndexExporterTests_{Guid.NewGuid():N}");
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
        Assert.Equal("Processing index..", _exporter.GetMessage());
    }

    #endregion

    #region Export writes correct file

    [Fact]
    public void Export_WritesIndexJsonFile()
    {
        var model = new PackageModel
        {
            Index = new PackageIndex
            {
                FormatVersion = 1,
                PackageVersion = new Version(1, 0, 0),
                Metadata = new SharedMetadata { Name = "TestPackage", Comment = "A test" }
            }
        };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "index.json");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Export_FileDeserializesCorrectly()
    {
        var model = new PackageModel
        {
            Index = new PackageIndex
            {
                FormatVersion = 2,
                PackageVersion = new Version(3, 1, 4),
                Metadata = new SharedMetadata { Name = "MyMod", Comment = "Custom mod" },
                ClueChanges = true,
                TextIncluded = true
            }
        };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "index.json"));
        var result = JsonSerializer.Deserialize<PackageIndex>(json);

        Assert.NotNull(result);
        Assert.Equal(2, result.FormatVersion);
        Assert.Equal(new Version(3, 1, 4), result.PackageVersion);
        Assert.Equal("MyMod", result.Metadata.Name);
        Assert.Equal("Custom mod", result.Metadata.Comment);
        Assert.True(result.ClueChanges);
        Assert.True(result.TextIncluded);
    }

    #endregion

    #region Change tracking fields

    [Fact]
    public void Export_PreservesChangeTrackingFields()
    {
        var model = new PackageModel
        {
            Index = new PackageIndex
            {
                FormatVersion = 1,
                PackageVersion = new Version(1, 0, 0),
                Metadata = new SharedMetadata { Name = "Test", Comment = "" },
                SimpleImageChanges = new HashSet<string> { "IMG001", "IMG002" },
                CrimeChanges = new HashSet<int> { 0, 1 },
                PlotChanges = true,
                FontChanges = true,
                AnimationIncluded = new HashSet<string> { "WALK" }
            }
        };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "index.json"));
        var result = JsonSerializer.Deserialize<PackageIndex>(json);

        Assert.NotNull(result);
        Assert.Equal(2, result.SimpleImageChanges.Count);
        Assert.Contains("IMG001", result.SimpleImageChanges);
        Assert.Equal(2, result.CrimeChanges.Count);
        Assert.True(result.PlotChanges);
        Assert.True(result.FontChanges);
        Assert.Single(result.AnimationIncluded);
    }

    #endregion

    #region GetTotalItemCountInPath always returns 1

    [Fact]
    public void Export_GetItemCount_TotalIsOne()
    {
        var model = new PackageModel
        {
            Index = new PackageIndex
            {
                FormatVersion = 1,
                PackageVersion = new Version(1, 0, 0),
                Metadata = new SharedMetadata()
            }
        };

        _exporter.Start(_tempDir, model);
        var (_, total) = _exporter.GetItemCount();

        Assert.Equal(1, total);
    }

    #endregion

    #region RunStep completion

    [Fact]
    public void RunStep_ReturnsTrueOnCompletion()
    {
        var model = new PackageModel
        {
            Index = new PackageIndex
            {
                FormatVersion = 1,
                PackageVersion = new Version(1, 0, 0),
                Metadata = new SharedMetadata()
            }
        };

        _exporter.Start(_tempDir, model);
        var done = _exporter.RunStep();

        Assert.True(done);
    }

    #endregion
}
