using System;
using System.IO;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class LegacyPlotParserSnapshotTests : IDisposable
{
    private readonly LegacyPlotParser _parser;
    private readonly string _tempDir;

    public LegacyPlotParserSnapshotTests()
    {
        _parser = new LegacyPlotParser(NullLogger<LegacyPlotParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyPlotParserSnapshotTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Snapshot data stability

    [Fact]
    public void Snapshot_MixedPlots_MatchesExpectedBase64()
    {
        var briefing = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 3, crimeIndex: 9, messageNumber: 0, message: "\r\nBriefing for mission\r\n", isLast: false);
        var success = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 3, crimeIndex: 0, messageNumber: 2, message: "\r\nYou succeeded\r\n", isLast: false);
        var failure = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 3, crimeIndex: 1, messageNumber: 7, message: "\r\nYou failed\r\n", isLast: false);
        var prevFail = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 3, crimeIndex: 2, messageNumber: 0xA, message: "\r\nPrevious failure\r\n", isLast: true);
        var fileData = PlotTestDataGenerator.BuildPlotFile(briefing, success, failure, prevFail);
        var base64 = Convert.ToBase64String(fileData);
        Assert.Equal(PlotSnapshotData.MixedPlots, base64);
    }

    #endregion

    #region Parse snapshot tests

    [Fact]
    public void ParseSnapshot_MixedPlots_HasCorrectEntryCount()
    {
        var package = ParseFromSnapshot(PlotSnapshotData.MixedPlots);
        Assert.Equal(4, package.Plots.Count);
    }

    [Fact]
    public void ParseSnapshot_MixedPlots_Briefing_HasCorrectProperties()
    {
        var package = ParseFromSnapshot(PlotSnapshotData.MixedPlots);

        var plot = package.Plots["PL0390"];
        Assert.Equal(PlotModel.PlotStringType.Briefing, plot.StringType);
        Assert.Equal(3, plot.MissionSetId);
        Assert.Null(plot.CrimeIndex);
        Assert.Equal(0, plot.MessageNumber);
        Assert.Equal("Briefing for mission", plot.Message);
    }

    [Fact]
    public void ParseSnapshot_MixedPlots_Success_HasCorrectProperties()
    {
        var package = ParseFromSnapshot(PlotSnapshotData.MixedPlots);

        var plot = package.Plots["PL0302"];
        Assert.Equal(PlotModel.PlotStringType.Success, plot.StringType);
        Assert.Equal(0, plot.CrimeIndex);
        Assert.Equal(2, plot.MessageNumber);
        Assert.Equal("You succeeded", plot.Message);
    }

    [Fact]
    public void ParseSnapshot_MixedPlots_Failure_HasCorrectProperties()
    {
        var package = ParseFromSnapshot(PlotSnapshotData.MixedPlots);

        var plot = package.Plots["PL0317"];
        Assert.Equal(PlotModel.PlotStringType.Failure, plot.StringType);
        Assert.Equal(1, plot.CrimeIndex);
        Assert.Equal(2, plot.MessageNumber);
        Assert.Equal("You failed", plot.Message);
    }

    [Fact]
    public void ParseSnapshot_MixedPlots_BriefingPreviousFailure_HasCorrectProperties()
    {
        var package = ParseFromSnapshot(PlotSnapshotData.MixedPlots);

        var plot = package.Plots["PL032A"];
        Assert.Equal(PlotModel.PlotStringType.BriefingPreviousFailure, plot.StringType);
        Assert.Equal(2, plot.CrimeIndex);
        Assert.Equal(0, plot.MessageNumber);
        Assert.Equal("Previous failure", plot.Message);
    }

    #endregion

    #region Helpers

    private PackageModel ParseFromSnapshot(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var filePath = Path.Combine(_tempDir, "PLOT.TXT");
        File.WriteAllBytes(filePath, bytes);

        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        var package = new PackageModel();
        _parser.SetResult(package);
        return package;
    }

    #endregion
}
