using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class PlotImporterTests : IDisposable
{
    private readonly PlotImporter _importer;
    private readonly string _tempDir;

    public PlotImporterTests()
    {
        _importer = new PlotImporter(NullLogger<PlotImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"PlotImporterTests_{Guid.NewGuid():N}");
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
        Assert.Equal("Processing plots..", _importer.GetMessage());
    }

    #endregion

    #region CheckIfValid

    [Fact]
    public void CheckIfValid_FileExists_ReturnsTrue()
    {
        WritePlotJson(new Dictionary<string, PlotModel>
        {
            ["PL0090"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.Briefing, MissionSetId = 0, MessageNumber = 0,
                Message = "test"
            }
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
    public void Import_DeserializesPlotModel()
    {
        var plots = new Dictionary<string, PlotModel>
        {
            ["PL0310"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.Success,
                MissionSetId = 3,
                CrimeIndex = 1,
                MessageNumber = 0,
                Message = "Mission accomplished."
            }
        };
        WritePlotJson(plots);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.Single(model.Plots);
        var plot = model.Plots["PL0310"];
        Assert.Equal(PlotModel.PlotStringType.Success, plot.StringType);
        Assert.Equal(3, plot.MissionSetId);
        Assert.Equal(1, plot.CrimeIndex);
        Assert.Equal(0, plot.MessageNumber);
        Assert.Equal("Mission accomplished.", plot.Message);
    }

    [Fact]
    public void Import_MultiplePlots_AllPresent()
    {
        var plots = new Dictionary<string, PlotModel>
        {
            ["PL0090"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.Briefing, MissionSetId = 0,
                MessageNumber = 0, Message = "Briefing."
            },
            ["PL0115"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.Failure, MissionSetId = 1, CrimeIndex = 1,
                MessageNumber = 0, Message = "Failed."
            },
            ["PL021A"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.BriefingPreviousFailure, MissionSetId = 2,
                CrimeIndex = 1, MessageNumber = 0, Message = "Previous failure."
            }
        };
        WritePlotJson(plots);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.Equal(3, model.Plots.Count);
    }

    #endregion

    #region SetResult populates correct field

    [Fact]
    public void SetResult_PopulatesPlotsField()
    {
        var plots = new Dictionary<string, PlotModel>
        {
            ["PL0091"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.Briefing, MissionSetId = 0,
                MessageNumber = 1, Message = "More briefing."
            }
        };
        WritePlotJson(plots);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.NotNull(model.Plots);
        Assert.True(model.Plots.ContainsKey("PL0091"));
        Assert.Empty(model.Clues);
    }

    #endregion

    #region Helpers

    private void WritePlotJson(Dictionary<string, PlotModel> plots)
    {
        var json = JsonSerializer.Serialize(plots, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_tempDir, "PLOT.json"), json);
    }

    #endregion
}
