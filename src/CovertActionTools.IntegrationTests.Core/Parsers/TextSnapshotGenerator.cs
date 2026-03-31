using System;
using Xunit;
using Xunit.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Generates base64-encoded TEXT.DTA data for snapshot tests.
/// Run this test to capture new baselines when the format changes intentionally.
/// </summary>
public class TextSnapshotGenerator
{
    private readonly ITestOutputHelper _output;

    public TextSnapshotGenerator(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateSnapshots()
    {
        var msg = TextTestDataGenerator.BuildMsgEntry(crimeId: 2, id: 1, message: "\r\nThe crime message body\r\n");
        var sorg = TextTestDataGenerator.BuildNonMsgEntry("SOR", 'G', id: 0, message: "\r\nSender org name\r\n");
        var fluf = TextTestDataGenerator.BuildNonMsgEntry("FLU", 'F', id: 3, message: "\r\nSome fluff text\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var mixedFile = TextTestDataGenerator.BuildTextFile(msg, sorg, fluf, end);
        var mixedBase64 = Convert.ToBase64String(mixedFile);

        _output.WriteLine($"MixedTexts = \"{mixedBase64}\";");
    }
}
