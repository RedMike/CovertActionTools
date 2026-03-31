using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class PlotExporterTests : IDisposable
{
    private readonly PlotExporter _exporter;
    private readonly string _tempDir;

    public PlotExporterTests()
    {
        _exporter = new PlotExporter(NullLogger<PlotExporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"PlotExporterTests_{Guid.NewGuid():N}");
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
        Assert.Equal("Processing plots..", _exporter.GetMessage());
    }

    #endregion

    #region Export writes correct file

    [Fact]
    public void Export_SinglePlot_WritesPlotJsonFile()
    {
        var model = new PackageModel
        {
            Plots = new Dictionary<string, PlotModel>
            {
                ["PL0090"] = new PlotModel
                {
                    StringType = PlotModel.PlotStringType.Briefing,
                    MissionSetId = 0,
                    CrimeIndex = null,
                    MessageNumber = 0,
                    Message = "Your mission begins."
                }
            }
        };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "PLOT.json");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Export_SinglePlot_FileDeserializesCorrectly()
    {
        var model = new PackageModel
        {
            Plots = new Dictionary<string, PlotModel>
            {
                ["PL0110"] = new PlotModel
                {
                    StringType = PlotModel.PlotStringType.Success,
                    MissionSetId = 1,
                    CrimeIndex = 1,
                    MessageNumber = 0,
                    Message = "The suspect was apprehended."
                }
            }
        };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "PLOT.json"));
        var result = JsonSerializer.Deserialize<Dictionary<string, PlotModel>>(json);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(PlotModel.PlotStringType.Success, result["PL0110"].StringType);
        Assert.Equal(1, result["PL0110"].MissionSetId);
        Assert.Equal(1, result["PL0110"].CrimeIndex);
        Assert.Equal(0, result["PL0110"].MessageNumber);
        Assert.Equal("The suspect was apprehended.", result["PL0110"].Message);
    }

    #endregion

    #region Multiple plots

    [Fact]
    public void Export_MultiplePlots_AllEntriesPresent()
    {
        var model = new PackageModel
        {
            Plots = new Dictionary<string, PlotModel>
            {
                ["PL0090"] = new PlotModel
                {
                    StringType = PlotModel.PlotStringType.Briefing, MissionSetId = 0,
                    MessageNumber = 0, Message = "Briefing message."
                },
                ["PL0115"] = new PlotModel
                {
                    StringType = PlotModel.PlotStringType.Failure, MissionSetId = 1, CrimeIndex = 1,
                    MessageNumber = 0, Message = "The criminal escaped."
                },
                ["PL021A"] = new PlotModel
                {
                    StringType = PlotModel.PlotStringType.BriefingPreviousFailure, MissionSetId = 2,
                    CrimeIndex = 1, MessageNumber = 0, Message = "Previous mission failed."
                }
            }
        };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "PLOT.json"));
        var result = JsonSerializer.Deserialize<Dictionary<string, PlotModel>>(json);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey("PL0090"));
        Assert.True(result.ContainsKey("PL0115"));
        Assert.True(result.ContainsKey("PL021A"));
    }

    #endregion

    #region Empty data

    [Fact]
    public void Export_EmptyPlots_DoesNotWriteFile()
    {
        var model = new PackageModel
        {
            Plots = new Dictionary<string, PlotModel>()
        };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "PLOT.json");
        Assert.False(File.Exists(filePath));
    }

    #endregion

    #region RunStep completion

    [Fact]
    public void RunStep_ReturnsTrueOnCompletion()
    {
        var model = new PackageModel
        {
            Plots = new Dictionary<string, PlotModel>
            {
                ["PL0091"] = new PlotModel
                {
                    StringType = PlotModel.PlotStringType.Briefing, MissionSetId = 0,
                    MessageNumber = 1, Message = "Continue your mission."
                }
            }
        };

        _exporter.Start(_tempDir, model);
        var done = _exporter.RunStep();

        Assert.True(done);
    }

    #endregion
}
