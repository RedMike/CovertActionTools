using System;
using Xunit;
using Xunit.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Generates base64-encoded PLOT.TXT data for snapshot tests.
/// Run this test to capture new baselines when the format changes intentionally.
/// </summary>
public class PlotSnapshotGenerator
{
    private readonly ITestOutputHelper _output;

    public PlotSnapshotGenerator(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateSnapshots()
    {
        var briefing = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 3, crimeIndex: 9, messageNumber: 0, message: "\r\nBriefing for mission\r\n", isLast: false);
        var success = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 3, crimeIndex: 0, messageNumber: 2, message: "\r\nYou succeeded\r\n", isLast: false);
        var failure = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 3, crimeIndex: 1, messageNumber: 7, message: "\r\nYou failed\r\n", isLast: false);
        var prevFail = PlotTestDataGenerator.BuildPlotEntry(missionSetId: 3, crimeIndex: 2, messageNumber: 0xA, message: "\r\nPrevious failure\r\n", isLast: true);
        var mixedFile = PlotTestDataGenerator.BuildPlotFile(briefing, success, failure, prevFail);
        var mixedBase64 = Convert.ToBase64String(mixedFile);

        _output.WriteLine($"MixedPlots = \"{mixedBase64}\";");
    }
}
