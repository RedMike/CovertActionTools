using System;
using Xunit;
using Xunit.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Generates base64-encoded PIC file data for snapshot tests.
/// Run this test to capture new baselines when the format changes intentionally.
/// </summary>
public class SimpleImageParserSnapshotGenerator
{
    private readonly ITestOutputHelper _output;

    public SimpleImageParserSnapshotGenerator(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateSnapshots()
    {
        var result1 = Convert.ToBase64String(
            SimpleImageParserTestDataGenerator.BuildPicFile(
                SharedImageTestDataGenerator.GenerateUniformPixels(4, 4), 4, 4));

        var result2 = Convert.ToBase64String(
            SimpleImageParserTestDataGenerator.BuildPicFile(
                SharedImageTestDataGenerator.GenerateVariedPixels(4, 4), 4, 4));

        var result3 = Convert.ToBase64String(
            SimpleImageParserTestDataGenerator.BuildPicFile(
                SharedImageTestDataGenerator.GenerateVariedPixels(16, 8), 16, 8));

        _output.WriteLine($"PicFile_4x4_Uniform = \"{result1}\";");
        _output.WriteLine($"PicFile_4x4_Varied = \"{result2}\";");
        _output.WriteLine($"PicFile_16x8_Varied = \"{result3}\";");
    }
}
