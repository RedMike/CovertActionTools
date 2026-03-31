using System;
using Xunit;
using Xunit.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Generates base64-encoded CLUES.TXT data for snapshot tests.
/// Run this test to capture new baselines when the format changes intentionally.
/// </summary>
public class ClueSnapshotGenerator
{
    private readonly ITestOutputHelper _output;

    public ClueSnapshotGenerator(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateSnapshots()
    {
        var nonCrime = ClueTestDataGenerator.BuildNonCrimeClueEntry(type: 3, id: 2, source: 1, message: "\r\nAirline ticket to Paris\r\n");
        var crime = ClueTestDataGenerator.BuildCrimeClueEntry(crimeId: 1, participantId: 5, source: 2, type: 7, message: "\r\nIdentity document found\r\n");
        var mixedFile = ClueTestDataGenerator.BuildCluesFile(nonCrime, crime);
        var mixedBase64 = Convert.ToBase64String(mixedFile);

        _output.WriteLine($"MixedClues = \"{mixedBase64}\";");
    }
}
