using System;
using Xunit;
using Xunit.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Generates base64-encoded PROSE.DTA data for snapshot tests.
/// Run this test to capture new baselines when the format changes intentionally.
/// </summary>
public class ProseSnapshotGenerator
{
    private readonly ITestOutputHelper _output;

    public ProseSnapshotGenerator(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateSnapshots()
    {
        var advice = ProseTestDataGenerator.BuildProseEntry("advice1", "\r\nBe careful out there\r\n");
        var lounge = ProseTestDataGenerator.BuildProseEntry("lounge", "\r\nYou enter the lounge\r\n");
        var surpriseL = ProseTestDataGenerator.BuildProseEntry("surpriseL", "\r\nYou lost the ambush\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var mixedFile = ProseTestDataGenerator.BuildProseFile(new[] { advice, lounge, surpriseL, end });
        var mixedBase64 = Convert.ToBase64String(mixedFile);

        _output.WriteLine($"MixedProse = \"{mixedBase64}\";");
    }
}
