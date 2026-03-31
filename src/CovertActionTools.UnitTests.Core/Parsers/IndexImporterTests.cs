using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class IndexImporterTests : IDisposable
{
    private readonly IndexImporter _importer;
    private readonly string _tempDir;

    public IndexImporterTests()
    {
        _importer = new IndexImporter(NullLogger<IndexImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"IndexImporterTests_{Guid.NewGuid():N}");
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
        Assert.Equal("Processing index..", _importer.GetMessage());
    }

    #endregion

    #region CheckIfValid

    [Fact]
    public void CheckIfValid_FileExists_ReturnsTrue()
    {
        WriteIndexJson(new PackageIndex
        {
            FormatVersion = 1,
            PackageVersion = new Version(1, 0, 0),
            Metadata = new SharedMetadata { Name = "Test", Comment = "" }
        });

        Assert.True(_importer.CheckIfValid(_tempDir));
    }

    [Fact]
    public void CheckIfValid_FileMissing_ReturnsFalse()
    {
        Assert.False(_importer.CheckIfValid(_tempDir));
    }

    #endregion

    #region Import deserializes correctly

    [Fact]
    public void Import_DeserializesPackageIndex()
    {
        var index = new PackageIndex
        {
            FormatVersion = 3,
            PackageVersion = new Version(2, 5, 1),
            Metadata = new SharedMetadata { Name = "ModPack", Comment = "A custom mod" },
            ClueChanges = true,
            PlotIncluded = true
        };
        WriteIndexJson(index);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.Equal(3, model.Index.FormatVersion);
        Assert.Equal(new Version(2, 5, 1), model.Index.PackageVersion);
        Assert.Equal("ModPack", model.Index.Metadata.Name);
        Assert.Equal("A custom mod", model.Index.Metadata.Comment);
        Assert.True(model.Index.ClueChanges);
        Assert.True(model.Index.PlotIncluded);
    }

    [Fact]
    public void Import_PreservesChangeTrackingSets()
    {
        var index = new PackageIndex
        {
            FormatVersion = 1,
            PackageVersion = new Version(1, 0, 0),
            Metadata = new SharedMetadata(),
            SimpleImageChanges = new HashSet<string> { "A", "B" },
            CrimeIncluded = new HashSet<int> { 0, 3 }
        };
        WriteIndexJson(index);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.Equal(2, model.Index.SimpleImageChanges.Count);
        Assert.Contains("A", model.Index.SimpleImageChanges);
        Assert.Equal(2, model.Index.CrimeIncluded.Count);
        Assert.Contains(3, model.Index.CrimeIncluded);
    }

    #endregion

    #region SetResult populates correct field

    [Fact]
    public void SetResult_PopulatesIndexField()
    {
        var index = new PackageIndex
        {
            FormatVersion = 1,
            PackageVersion = new Version(1, 0, 0),
            Metadata = new SharedMetadata { Name = "IndexTest", Comment = "" }
        };
        WriteIndexJson(index);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.Equal("IndexTest", model.Index.Metadata.Name);
    }

    #endregion

    #region Helpers

    private void WriteIndexJson(PackageIndex index)
    {
        var json = JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_tempDir, "index.json"), json);
    }

    #endregion
}
