using System;
using Xunit;
using Xunit.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers.Data;

/// <summary>
/// Generates base64-encoded PAN file data for snapshot tests.
/// Run this test to capture new baselines when the format changes intentionally.
/// </summary>
public class AnimationSnapshotGenerator
{
    private readonly ITestOutputHelper _output;

    public AnimationSnapshotGenerator(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateSnapshots()
    {
        var result1 = Convert.ToBase64String(
            AnimationIntegrationTestDataGenerator.BuildMinimalPanFile());

        var result2 = Convert.ToBase64String(
            AnimationIntegrationTestDataGenerator.BuildMinimalPanFile(
                boundingWidth: 159, boundingHeight: 99, frameDelay: 3, clearColor: 7));

        _output.WriteLine($"PanFile_Minimal_Default = \"{result1}\";");
        _output.WriteLine($"PanFile_Minimal_CustomValues = \"{result2}\";");
    }
}
