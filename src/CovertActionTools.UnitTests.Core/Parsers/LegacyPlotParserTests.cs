using System;
using System.IO;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Parsers.Data;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class LegacyPlotParserTests : IDisposable
{
    private readonly LegacyPlotParser _parser;
    private readonly string _tempDir;

    public LegacyPlotParserTests()
    {
        _parser = new LegacyPlotParser(NullLogger<LegacyPlotParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyPlotParserTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Briefing entries (crimeIndex=9)

    [Fact]
    public void Parse_BriefingEntry_SetsBriefingType()
    {
        var entry = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 1, crimeIndex: 9, messageNumber: 0, message: "\r\nBriefing text\r\n", isLast: true);
        var fileData = PlotTestDataGenerator.BuildPlotFile(entry);
        PlotTestDataGenerator.WritePlotFile(_tempDir, fileData);

        var package = RunParser();

        var plot = package.Plots["PL0190"];
        Assert.Equal(PlotModel.PlotStringType.Briefing, plot.StringType);
    }

    [Fact]
    public void Parse_BriefingEntry_HasNullCrimeIndex()
    {
        var entry = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 2, crimeIndex: 9, messageNumber: 1, message: "\r\nBriefing\r\n", isLast: true);
        var fileData = PlotTestDataGenerator.BuildPlotFile(entry);
        PlotTestDataGenerator.WritePlotFile(_tempDir, fileData);

        var package = RunParser();

        var plot = package.Plots["PL0291"];
        Assert.Null(plot.CrimeIndex);
    }

    [Fact]
    public void Parse_BriefingEntry_SetsMissionSetId()
    {
        var entry = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 5, crimeIndex: 9, messageNumber: 2, message: "\r\nTest\r\n", isLast: true);
        var fileData = PlotTestDataGenerator.BuildPlotFile(entry);
        PlotTestDataGenerator.WritePlotFile(_tempDir, fileData);

        var package = RunParser();

        var plot = package.Plots["PL0592"];
        Assert.Equal(5, plot.MissionSetId);
    }

    #endregion

    #region Success entries (messageNumber < 5)

    [Fact]
    public void Parse_SuccessEntry_SetsSuccessType()
    {
        var entry = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 1, crimeIndex: 0, messageNumber: 3, message: "\r\nSuccess msg\r\n", isLast: true);
        var fileData = PlotTestDataGenerator.BuildPlotFile(entry);
        PlotTestDataGenerator.WritePlotFile(_tempDir, fileData);

        var package = RunParser();

        var plot = package.Plots["PL0103"];
        Assert.Equal(PlotModel.PlotStringType.Success, plot.StringType);
        Assert.Equal(3, plot.MessageNumber);
    }

    [Fact]
    public void Parse_SuccessEntry_SetsCrimeIndex()
    {
        var entry = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 3, crimeIndex: 2, messageNumber: 0, message: "\r\nWin\r\n", isLast: true);
        var fileData = PlotTestDataGenerator.BuildPlotFile(entry);
        PlotTestDataGenerator.WritePlotFile(_tempDir, fileData);

        var package = RunParser();

        var plot = package.Plots["PL0320"];
        Assert.Equal(2, plot.CrimeIndex);
    }

    #endregion

    #region Failure entries (messageNumber 5-9)

    [Fact]
    public void Parse_FailureEntry_SetsFailureType()
    {
        // messageNumber=7 in file -> type=Failure, adjusted messageNumber=2
        var entry = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 1, crimeIndex: 1, messageNumber: 7, message: "\r\nFailed\r\n", isLast: true);
        var fileData = PlotTestDataGenerator.BuildPlotFile(entry);
        PlotTestDataGenerator.WritePlotFile(_tempDir, fileData);

        var package = RunParser();

        var plot = package.Plots["PL0117"];
        Assert.Equal(PlotModel.PlotStringType.Failure, plot.StringType);
        Assert.Equal(2, plot.MessageNumber);
    }

    #endregion

    #region BriefingPreviousFailure entries (messageNumber >= 10)

    [Fact]
    public void Parse_BriefingPreviousFailureEntry_SetsType()
    {
        // messageNumber=0xA (10) in file -> type=BriefingPreviousFailure, adjusted messageNumber=0
        var entry = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 2, crimeIndex: 0, messageNumber: 0xA, message: "\r\nPrev fail brief\r\n", isLast: true);
        var fileData = PlotTestDataGenerator.BuildPlotFile(entry);
        PlotTestDataGenerator.WritePlotFile(_tempDir, fileData);

        var package = RunParser();

        var plot = package.Plots["PL020A"];
        Assert.Equal(PlotModel.PlotStringType.BriefingPreviousFailure, plot.StringType);
        Assert.Equal(0, plot.MessageNumber);
    }

    #endregion

    #region Multiple entries

    [Fact]
    public void Parse_MultipleEntries_ReturnsAll()
    {
        var entry1 = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 1, crimeIndex: 9, messageNumber: 0, message: "\r\nBrief\r\n", isLast: false);
        var entry2 = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 1, crimeIndex: 0, messageNumber: 0, message: "\r\nSuccess\r\n", isLast: true);
        var fileData = PlotTestDataGenerator.BuildPlotFile(entry1, entry2);
        PlotTestDataGenerator.WritePlotFile(_tempDir, fileData);

        var package = RunParser();

        Assert.Equal(2, package.Plots.Count);
        Assert.Equal("Brief", package.Plots["PL0190"].Message);
        Assert.Equal("Success", package.Plots["PL0100"].Message);
    }

    #endregion

    #region Message trimming

    [Fact]
    public void Parse_MessageWithLeadingAndTrailingNewlines_TrimsOnePair()
    {
        var entry = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 0, crimeIndex: 9, messageNumber: 0, message: "\r\nLine1\r\nLine2\r\n", isLast: true);
        var fileData = PlotTestDataGenerator.BuildPlotFile(entry);
        PlotTestDataGenerator.WritePlotFile(_tempDir, fileData);

        var package = RunParser();

        var plot = package.Plots["PL0090"];
        Assert.Equal("Line1\r\nLine2", plot.Message);
    }

    #endregion

    #region Last entry terminator

    [Fact]
    public void Parse_LastEntryWith0x1A_ParsesCorrectly()
    {
        var entry = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 4, crimeIndex: 1, messageNumber: 2, message: "\r\nLast entry\r\n", isLast: true);
        var fileData = PlotTestDataGenerator.BuildPlotFile(entry);
        PlotTestDataGenerator.WritePlotFile(_tempDir, fileData);

        var package = RunParser();

        Assert.Single(package.Plots);
        Assert.Equal("Last entry", package.Plots["PL0412"].Message);
    }

    #endregion

    #region SetResult

    [Fact]
    public void SetResult_PopulatesPlotsOnPackageModel()
    {
        var entry = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 0, crimeIndex: 9, messageNumber: 0, message: "\r\nTest\r\n", isLast: true);
        var fileData = PlotTestDataGenerator.BuildPlotFile(entry);
        PlotTestDataGenerator.WritePlotFile(_tempDir, fileData);

        var package = RunParser();

        Assert.NotEmpty(package.Plots);
    }

    #endregion

    #region Helpers

    private PackageModel RunParser()
    {
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        var package = new PackageModel();
        _parser.SetResult(package);
        return package;
    }

    #endregion
}
