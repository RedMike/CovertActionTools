using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class PlotExporterImporterRoundtripTests : IDisposable
{
    private readonly PlotExporter _exporter;
    private readonly PlotImporter _importer;
    private readonly string _tempDir;

    public PlotExporterImporterRoundtripTests()
    {
        _exporter = new PlotExporter(NullLogger<PlotExporter>.Instance);
        _importer = new PlotImporter(NullLogger<PlotImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"PlotRoundtrip_{Guid.NewGuid():N}");
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
    public void Roundtrip_BriefingPlot_AllPropertiesMatch()
    {
        var original = new Dictionary<string, PlotModel>
        {
            ["PL0090"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.Briefing,
                MissionSetId = 0,
                CrimeIndex = null,
                MessageNumber = 0,
                Message = "Agent, your mission is to infiltrate the compound."
            }
        };

        var result = ExportAndImport(original);

        Assert.Single(result);
        var plot = result["PL0090"];
        Assert.Equal(PlotModel.PlotStringType.Briefing, plot.StringType);
        Assert.Equal(0, plot.MissionSetId);
        Assert.Null(plot.CrimeIndex);
        Assert.Equal(0, plot.MessageNumber);
        Assert.Equal("Agent, your mission is to infiltrate the compound.", plot.Message);
    }

    [Fact]
    public void Roundtrip_SuccessPlot_PreservesCrimeIndex()
    {
        var original = new Dictionary<string, PlotModel>
        {
            ["PL0310"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.Success,
                MissionSetId = 3,
                CrimeIndex = 1,
                MessageNumber = 0,
                Message = "The suspect has been apprehended."
            }
        };

        var result = ExportAndImport(original);

        Assert.Equal(1, result["PL0310"].CrimeIndex);
        Assert.Equal(3, result["PL0310"].MissionSetId);
    }

    [Fact]
    public void Roundtrip_FailurePlot_PreservesAllFields()
    {
        var original = new Dictionary<string, PlotModel>
        {
            ["PL0117"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.Failure,
                MissionSetId = 1,
                CrimeIndex = 1,
                MessageNumber = 2,
                Message = "The criminal has escaped the country."
            }
        };

        var result = ExportAndImport(original);

        var plot = result["PL0117"];
        Assert.Equal(PlotModel.PlotStringType.Failure, plot.StringType);
        Assert.Equal(1, plot.MissionSetId);
        Assert.Equal(1, plot.CrimeIndex);
        Assert.Equal(2, plot.MessageNumber);
        Assert.Equal("The criminal has escaped the country.", plot.Message);
    }

    [Fact]
    public void Roundtrip_BriefingPreviousFailure_PreservesAllFields()
    {
        var original = new Dictionary<string, PlotModel>
        {
            ["PL021A"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.BriefingPreviousFailure,
                MissionSetId = 2,
                CrimeIndex = 1,
                MessageNumber = 0,
                Message = "Due to previous failure, the situation has escalated."
            }
        };

        var result = ExportAndImport(original);

        var plot = result["PL021A"];
        Assert.Equal(PlotModel.PlotStringType.BriefingPreviousFailure, plot.StringType);
        Assert.Equal(2, plot.MissionSetId);
        Assert.Equal(1, plot.CrimeIndex);
        Assert.Equal(0, plot.MessageNumber);
    }

    [Fact]
    public void Roundtrip_MultiplePlots_AllPreserved()
    {
        var original = new Dictionary<string, PlotModel>
        {
            ["PL0090"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.Briefing, MissionSetId = 0,
                MessageNumber = 0, Message = "Briefing text one."
            },
            ["PL0091"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.Briefing, MissionSetId = 0,
                MessageNumber = 1, Message = "Briefing text two."
            },
            ["PL0110"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.Success, MissionSetId = 1, CrimeIndex = 1,
                MessageNumber = 0, Message = "Success message."
            },
            ["PL0115"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.Failure, MissionSetId = 1, CrimeIndex = 1,
                MessageNumber = 0, Message = "Failure message."
            }
        };

        var result = ExportAndImport(original);

        Assert.Equal(4, result.Count);
        foreach (var key in original.Keys)
        {
            Assert.True(result.ContainsKey(key));
            Assert.Equal(original[key].StringType, result[key].StringType);
            Assert.Equal(original[key].MissionSetId, result[key].MissionSetId);
            Assert.Equal(original[key].CrimeIndex, result[key].CrimeIndex);
            Assert.Equal(original[key].MessageNumber, result[key].MessageNumber);
            Assert.Equal(original[key].Message, result[key].Message);
        }
    }

    [Fact]
    public void Roundtrip_AllStringTypes_PreservedCorrectly()
    {
        var original = new Dictionary<string, PlotModel>
        {
            ["B"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.Briefing, MissionSetId = 0,
                MessageNumber = 0, Message = "briefing"
            },
            ["F"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.BriefingPreviousFailure, MissionSetId = 0,
                CrimeIndex = 0, MessageNumber = 0, Message = "prev failure"
            },
            ["S"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.Success, MissionSetId = 0,
                CrimeIndex = 0, MessageNumber = 0, Message = "success"
            },
            ["X"] = new PlotModel
            {
                StringType = PlotModel.PlotStringType.Failure, MissionSetId = 0,
                CrimeIndex = 0, MessageNumber = 0, Message = "failure"
            }
        };

        var result = ExportAndImport(original);

        Assert.Equal(4, result.Count);
        Assert.Equal(PlotModel.PlotStringType.Briefing, result["B"].StringType);
        Assert.Equal(PlotModel.PlotStringType.BriefingPreviousFailure, result["F"].StringType);
        Assert.Equal(PlotModel.PlotStringType.Success, result["S"].StringType);
        Assert.Equal(PlotModel.PlotStringType.Failure, result["X"].StringType);
    }

    #endregion

    #region Helpers

    private Dictionary<string, PlotModel> ExportAndImport(Dictionary<string, PlotModel> plots)
    {
        var exportModel = new PackageModel { Plots = plots };
        _exporter.Start(_tempDir, exportModel);
        _exporter.RunStep();

        _importer.Start(_tempDir);
        _importer.RunStep();

        var importModel = new PackageModel();
        _importer.SetResult(importModel);
        return importModel.Plots;
    }

    #endregion
}
