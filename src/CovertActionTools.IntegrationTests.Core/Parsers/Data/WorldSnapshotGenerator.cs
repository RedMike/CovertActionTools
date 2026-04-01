using System;
using Xunit;
using Xunit.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers.Data;

/// <summary>
/// Generates base64-encoded world file data for snapshot tests.
/// Run this test to capture new baselines when the format changes intentionally.
/// Output appears in the test runner's output window (ITestOutputHelper).
/// </summary>
public class WorldSnapshotGenerator
{
    private readonly ITestOutputHelper _output;

    public WorldSnapshotGenerator(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateSnapshots()
    {
        var standardFile = WorldTestDataGenerator.BuildStandardWorldFile();
        var standardBase64 = Convert.ToBase64String(standardFile);
        _output.WriteLine($"StandardWorldFile = \"{standardBase64}\";");
    }
}
