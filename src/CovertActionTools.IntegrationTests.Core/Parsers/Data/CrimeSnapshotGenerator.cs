using System;
using Xunit;
using Xunit.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers.Data;

/// <summary>
/// Generates base64-encoded crime file data for snapshot tests.
/// Run this test to capture new baselines when the format changes intentionally.
/// Output appears in the test runner's output window (ITestOutputHelper).
/// </summary>
public class CrimeSnapshotGenerator
{
    private readonly ITestOutputHelper _output;

    public CrimeSnapshotGenerator(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateSnapshots()
    {
        var standardFile = CrimeTestDataGenerator.BuildStandardCrimeFile();
        var standardBase64 = Convert.ToBase64String(standardFile);
        _output.WriteLine($"StandardCrimeFile = \"{standardBase64}\";");
    }
}
