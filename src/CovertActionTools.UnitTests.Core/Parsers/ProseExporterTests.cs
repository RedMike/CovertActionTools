using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class ProseExporterTests : IDisposable
{
    private readonly ProseExporter _exporter;
    private readonly string _tempDir;

    public ProseExporterTests()
    {
        _exporter = new ProseExporter(NullLogger<ProseExporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"ProseExporterTests_{Guid.NewGuid():N}");
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
    public void GetMessage_ReturnsExpectedMessage()
    {
        var message = _exporter.GetMessage();

        Assert.Equal("Processing prose..", message);
    }

    #endregion

    #region Export creates PROSE.json

    [Fact]
    public void Export_SingleEntry_CreatesProseJsonFile()
    {
        var model = CreatePackageModelWithProse(new Dictionary<string, ProseModel>
        {
            ["advice1"] = new ProseModel
            {
                Type = ProseModel.ProseType.Advice,
                SecondaryId = "1",
                Message = "Some advice text"
            }
        });

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        Assert.True(File.Exists(Path.Combine(_tempDir, "PROSE.json")));
    }

    [Fact]
    public void Export_SingleEntry_JsonContainsCorrectData()
    {
        var prose = new Dictionary<string, ProseModel>
        {
            ["lounge"] = new ProseModel
            {
                Type = ProseModel.ProseType.Lounge,
                SecondaryId = "",
                Message = "Welcome to the lounge"
            }
        };
        var model = CreatePackageModelWithProse(prose);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "PROSE.json"));
        var result = JsonSerializer.Deserialize<Dictionary<string, ProseModel>>(json);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result.ContainsKey("lounge"));
        Assert.Equal(ProseModel.ProseType.Lounge, result["lounge"].Type);
        Assert.Equal("Welcome to the lounge", result["lounge"].Message);
    }

    [Fact]
    public void Export_MultipleEntries_AllEntriesPresent()
    {
        var prose = new Dictionary<string, ProseModel>
        {
            ["advice1"] = new ProseModel
            {
                Type = ProseModel.ProseType.Advice,
                SecondaryId = "1",
                Message = "First advice"
            },
            ["nice0"] = new ProseModel
            {
                Type = ProseModel.ProseType.CharacterCapture,
                SecondaryId = "0",
                Message = "Captured character"
            },
            ["escape"] = new ProseModel
            {
                Type = ProseModel.ProseType.Escape,
                SecondaryId = "",
                Message = "You escaped"
            }
        };
        var model = CreatePackageModelWithProse(prose);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "PROSE.json"));
        var result = JsonSerializer.Deserialize<Dictionary<string, ProseModel>>(json);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey("advice1"));
        Assert.True(result.ContainsKey("nice0"));
        Assert.True(result.ContainsKey("escape"));
    }

    #endregion

    #region Empty data handling

    [Fact]
    public void Export_EmptyDictionary_DoesNotCreateFile()
    {
        var model = CreatePackageModelWithProse(new Dictionary<string, ProseModel>());

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        Assert.False(File.Exists(Path.Combine(_tempDir, "PROSE.json")));
    }

    #endregion

    #region RunStep returns done

    [Fact]
    public void RunStep_WithData_ReturnsTrue()
    {
        var prose = new Dictionary<string, ProseModel>
        {
            ["followed"] = new ProseModel
            {
                Type = ProseModel.ProseType.CarFollowed,
                SecondaryId = "",
                Message = "You are being followed"
            }
        };
        var model = CreatePackageModelWithProse(prose);

        _exporter.Start(_tempDir, model);
        var done = _exporter.RunStep();

        Assert.True(done);
    }

    #endregion

    #region Helpers

    private static PackageModel CreatePackageModelWithProse(Dictionary<string, ProseModel> prose)
    {
        return new PackageModel
        {
            Prose = prose
        };
    }

    #endregion
}
