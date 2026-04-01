using System;
using System.IO;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.IntegrationTests.Core.Parsers.Data;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class LegacyTextParserSnapshotTests : IDisposable
{
    private readonly LegacyTextParser _parser;
    private readonly string _tempDir;

    public LegacyTextParserSnapshotTests()
    {
        _parser = new LegacyTextParser(NullLogger<LegacyTextParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyTextParserSnapshotTests_{Guid.NewGuid():N}");
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
    public void Snapshot_MixedTexts_MatchesExpectedBase64()
    {
        var msg = TextTestDataGenerator.BuildMsgEntry(crimeId: 2, id: 1, message: "\r\nThe crime message body\r\n");
        var sorg = TextTestDataGenerator.BuildNonMsgEntry("SOR", 'G', id: 0, message: "\r\nSender org name\r\n");
        var fluf = TextTestDataGenerator.BuildNonMsgEntry("FLU", 'F', id: 3, message: "\r\nSome fluff text\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var fileData = TextTestDataGenerator.BuildTextFile(msg, sorg, fluf, end);
        var base64 = Convert.ToBase64String(fileData);
        Assert.Equal(TextSnapshotData.MixedTexts, base64);
    }

    #endregion

    #region Parse snapshot tests

    [Fact]
    public void ParseSnapshot_MixedTexts_HasCorrectEntryCount()
    {
        var package = ParseFromSnapshot(TextSnapshotData.MixedTexts);
        Assert.Equal(3, package.Texts.Count);
    }

    [Fact]
    public void ParseSnapshot_MixedTexts_MsgEntry_HasCorrectProperties()
    {
        var package = ParseFromSnapshot(TextSnapshotData.MixedTexts);

        var text = package.Texts["MSG0201"];
        Assert.Equal(TextModel.StringType.CrimeMessage, text.Type);
        Assert.Equal(1, text.Id);
        Assert.Equal(2, text.CrimeId);
        Assert.Equal("The crime message body", text.Message);
    }

    [Fact]
    public void ParseSnapshot_MixedTexts_SorgEntry_HasCorrectProperties()
    {
        var package = ParseFromSnapshot(TextSnapshotData.MixedTexts);

        var text = package.Texts["SORG00"];
        Assert.Equal(TextModel.StringType.SenderOrganisation, text.Type);
        Assert.Equal(0, text.Id);
        Assert.Null(text.CrimeId);
        Assert.Equal("Sender org name", text.Message);
    }

    [Fact]
    public void ParseSnapshot_MixedTexts_FlufEntry_HasCorrectProperties()
    {
        var package = ParseFromSnapshot(TextSnapshotData.MixedTexts);

        var text = package.Texts["FLUF03"];
        Assert.Equal(TextModel.StringType.Fluff, text.Type);
        Assert.Equal(3, text.Id);
        Assert.Null(text.CrimeId);
        Assert.Equal("Some fluff text", text.Message);
    }

    #endregion

    #region Helpers

    private PackageModel ParseFromSnapshot(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var filePath = Path.Combine(_tempDir, "TEXT.DTA");
        File.WriteAllBytes(filePath, bytes);

        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        var package = new PackageModel();
        _parser.SetResult(package);
        return package;
    }

    #endregion
}
