using System;
using System.IO;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Parsers.Data;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class LegacyTextParserTests : IDisposable
{
    private readonly LegacyTextParser _parser;
    private readonly string _tempDir;

    public LegacyTextParserTests()
    {
        _parser = new LegacyTextParser(NullLogger<LegacyTextParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyTextParserTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region MSG entries

    [Fact]
    public void Parse_MsgEntry_SetsCrimeMessageType()
    {
        var entry = TextTestDataGenerator.BuildMsgEntry(crimeId: 3, id: 1, message: "\r\nCrime message\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var fileData = TextTestDataGenerator.BuildTextFile(entry, end);
        TextTestDataGenerator.WriteTextFile(_tempDir, fileData);

        var package = RunParser();

        var text = package.Texts["MSG0301"];
        Assert.Equal(TextModel.StringType.CrimeMessage, text.Type);
    }

    [Fact]
    public void Parse_MsgEntry_SetsCrimeId()
    {
        var entry = TextTestDataGenerator.BuildMsgEntry(crimeId: 5, id: 2, message: "\r\nTest\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var fileData = TextTestDataGenerator.BuildTextFile(entry, end);
        TextTestDataGenerator.WriteTextFile(_tempDir, fileData);

        var package = RunParser();

        var text = package.Texts["MSG0502"];
        Assert.Equal(5, text.CrimeId);
    }

    [Fact]
    public void Parse_MsgEntry_SetsIdAndMessage()
    {
        var entry = TextTestDataGenerator.BuildMsgEntry(crimeId: 0, id: 3, message: "\r\nHello world\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var fileData = TextTestDataGenerator.BuildTextFile(entry, end);
        TextTestDataGenerator.WriteTextFile(_tempDir, fileData);

        var package = RunParser();

        var text = package.Texts["MSG0003"];
        Assert.Equal(3, text.Id);
        Assert.Equal("Hello world", text.Message);
    }

    #endregion

    #region Non-MSG entries

    [Fact]
    public void Parse_SorgEntry_SetsSenderOrganisationType()
    {
        var entry = TextTestDataGenerator.BuildNonMsgEntry("SOR", 'G', id: 1, message: "\r\nOrg name\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var fileData = TextTestDataGenerator.BuildTextFile(entry, end);
        TextTestDataGenerator.WriteTextFile(_tempDir, fileData);

        var package = RunParser();

        var text = package.Texts["SORG01"];
        Assert.Equal(TextModel.StringType.SenderOrganisation, text.Type);
        Assert.Equal(1, text.Id);
        Assert.Equal("Org name", text.Message);
    }

    [Fact]
    public void Parse_RorgEntry_SetsReceiverOrganisationType()
    {
        var entry = TextTestDataGenerator.BuildNonMsgEntry("ROR", 'G', id: 2, message: "\r\nReceiver org\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var fileData = TextTestDataGenerator.BuildTextFile(entry, end);
        TextTestDataGenerator.WriteTextFile(_tempDir, fileData);

        var package = RunParser();

        var text = package.Texts["RORG02"];
        Assert.Equal(TextModel.StringType.ReceiverOrganisation, text.Type);
    }

    [Fact]
    public void Parse_SlocEntry_SetsSenderLocationType()
    {
        var entry = TextTestDataGenerator.BuildNonMsgEntry("SLO", 'C', id: 0, message: "\r\nLocation\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var fileData = TextTestDataGenerator.BuildTextFile(entry, end);
        TextTestDataGenerator.WriteTextFile(_tempDir, fileData);

        var package = RunParser();

        var text = package.Texts["SLOC00"];
        Assert.Equal(TextModel.StringType.SenderLocation, text.Type);
    }

    [Fact]
    public void Parse_RlocEntry_SetsReceiverLocationType()
    {
        var entry = TextTestDataGenerator.BuildNonMsgEntry("RLO", 'C', id: 5, message: "\r\nRecv loc\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var fileData = TextTestDataGenerator.BuildTextFile(entry, end);
        TextTestDataGenerator.WriteTextFile(_tempDir, fileData);

        var package = RunParser();

        var text = package.Texts["RLOC05"];
        Assert.Equal(TextModel.StringType.ReceiverLocation, text.Type);
    }

    [Fact]
    public void Parse_FlufEntry_SetsFluffType()
    {
        var entry = TextTestDataGenerator.BuildNonMsgEntry("FLU", 'F', id: 3, message: "\r\nFluff text\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var fileData = TextTestDataGenerator.BuildTextFile(entry, end);
        TextTestDataGenerator.WriteTextFile(_tempDir, fileData);

        var package = RunParser();

        var text = package.Texts["FLUF03"];
        Assert.Equal(TextModel.StringType.Fluff, text.Type);
    }

    [Fact]
    public void Parse_AlrtEntry_SetsAlertType()
    {
        var entry = TextTestDataGenerator.BuildNonMsgEntry("ALR", 'T', id: 7, message: "\r\nAlert text\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var fileData = TextTestDataGenerator.BuildTextFile(entry, end);
        TextTestDataGenerator.WriteTextFile(_tempDir, fileData);

        var package = RunParser();

        var text = package.Texts["ALRT07"];
        Assert.Equal(TextModel.StringType.Alert, text.Type);
    }

    [Fact]
    public void Parse_AidEntry_SetsAidingOrganisationType()
    {
        var entry = TextTestDataGenerator.BuildNonMsgEntry("AID", 'D', id: 4, message: "\r\nAid org\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var fileData = TextTestDataGenerator.BuildTextFile(entry, end);
        TextTestDataGenerator.WriteTextFile(_tempDir, fileData);

        var package = RunParser();

        var text = package.Texts["AIDD04"];
        Assert.Equal(TextModel.StringType.AidingOrganisation, text.Type);
    }

    #endregion

    #region Multiple entries

    [Fact]
    public void Parse_MultipleEntries_ReturnsAll()
    {
        var entry1 = TextTestDataGenerator.BuildNonMsgEntry("SOR", 'G', id: 0, message: "\r\nFirst\r\n");
        var entry2 = TextTestDataGenerator.BuildNonMsgEntry("FLU", 'F', id: 1, message: "\r\nSecond\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var fileData = TextTestDataGenerator.BuildTextFile(entry1, entry2, end);
        TextTestDataGenerator.WriteTextFile(_tempDir, fileData);

        var package = RunParser();

        Assert.Equal(2, package.Texts.Count);
        Assert.Equal("First", package.Texts["SORG00"].Message);
        Assert.Equal("Second", package.Texts["FLUF01"].Message);
    }

    #endregion

    #region Non-MSG entries have no CrimeId

    [Fact]
    public void Parse_NonMsgEntry_HasNullCrimeId()
    {
        var entry = TextTestDataGenerator.BuildNonMsgEntry("SOR", 'G', id: 0, message: "\r\nTest\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var fileData = TextTestDataGenerator.BuildTextFile(entry, end);
        TextTestDataGenerator.WriteTextFile(_tempDir, fileData);

        var package = RunParser();

        var text = package.Texts["SORG00"];
        Assert.Null(text.CrimeId);
    }

    #endregion

    #region Message trimming

    [Fact]
    public void Parse_MessageWithLeadingAndTrailingNewlines_TrimsOnePair()
    {
        var entry = TextTestDataGenerator.BuildNonMsgEntry("FLU", 'F', id: 0, message: "\r\nLine1\r\nLine2\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var fileData = TextTestDataGenerator.BuildTextFile(entry, end);
        TextTestDataGenerator.WriteTextFile(_tempDir, fileData);

        var package = RunParser();

        var text = package.Texts["FLUF00"];
        Assert.Equal("Line1\r\nLine2", text.Message);
    }

    #endregion

    #region SetResult

    [Fact]
    public void SetResult_PopulatesTextsOnPackageModel()
    {
        var entry = TextTestDataGenerator.BuildNonMsgEntry("SOR", 'G', id: 0, message: "\r\nTest\r\n");
        var end = TextTestDataGenerator.BuildEndEntry();
        var fileData = TextTestDataGenerator.BuildTextFile(entry, end);
        TextTestDataGenerator.WriteTextFile(_tempDir, fileData);

        var package = RunParser();

        Assert.NotEmpty(package.Texts);
    }

    #endregion

    #region Helpers

    private PackageModel RunParser()
    {
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        var package = new PackageModel();
        _parser.SetResult(package);
        return package;
    }

    #endregion
}
