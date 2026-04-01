using System;
using System.IO;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Parsers.Data;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class LegacyProseParserTests : IDisposable
{
    private readonly LegacyProseParser _parser;
    private readonly string _tempDir;

    public LegacyProseParserTests()
    {
        _parser = new LegacyProseParser(NullLogger<LegacyProseParser>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyProseParserTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Advice entries

    [Fact]
    public void Parse_AdviceEntry_SetsAdviceType()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("advice1", "\r\nSome advice\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        var prose = package.Prose["advice1"];
        Assert.Equal(ProseModel.ProseType.Advice, prose.Type);
        Assert.Equal("1", prose.SecondaryId);
    }

    [Fact]
    public void Parse_AdviceEntry_SetsMessage()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("advicea", "\r\nAdvice text here\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        var prose = package.Prose["advicea"];
        Assert.Equal("Advice text here", prose.Message);
    }

    #endregion

    #region CharacterCapture entries

    [Fact]
    public void Parse_NiceEntry_SetsCharacterCaptureType()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("nice0", "\r\nCapture text\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        var prose = package.Prose["nice0"];
        Assert.Equal(ProseModel.ProseType.CharacterCapture, prose.Type);
        Assert.Equal("0", prose.SecondaryId);
    }

    #endregion

    #region MastermindCapture entries

    [Fact]
    public void Parse_Nice2Entry_SetsMastermindCaptureType()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("nice2", "\r\nMastermind caught\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        var prose = package.Prose["nice2"];
        Assert.Equal(ProseModel.ProseType.MastermindCapture, prose.Type);
        Assert.Equal("2", prose.SecondaryId);
    }

    #endregion

    #region Simple type entries

    [Fact]
    public void Parse_LoungeEntry_SetsLoungeType()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("lounge", "\r\nLounge text\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        var prose = package.Prose["lounge"];
        Assert.Equal(ProseModel.ProseType.Lounge, prose.Type);
        Assert.Equal(string.Empty, prose.SecondaryId);
    }

    [Fact]
    public void Parse_FollowedEntry_SetsCarFollowedType()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("followed", "\r\nFollowed text\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        var prose = package.Prose["followed"];
        Assert.Equal(ProseModel.ProseType.CarFollowed, prose.Type);
    }

    [Fact]
    public void Parse_CarcapEntry_SetsCarFollowedEndType()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("carcap", "\r\nCar captured\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        var prose = package.Prose["carcap"];
        Assert.Equal(ProseModel.ProseType.CarFollowedEnd, prose.Type);
    }

    [Fact]
    public void Parse_GrilledEntry_SetsInterrogatedType()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("grilled", "\r\nGrilled text\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        var prose = package.Prose["grilled"];
        Assert.Equal(ProseModel.ProseType.Interrogated, prose.Type);
    }

    [Fact]
    public void Parse_EscapeEntry_SetsEscapeType()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("escape", "\r\nEscaped\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        var prose = package.Prose["escape"];
        Assert.Equal(ProseModel.ProseType.Escape, prose.Type);
    }

    [Fact]
    public void Parse_DoubleaEntry_SetsDoubleAgentAgreeType()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("doublea", "\r\nDouble agent\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        var prose = package.Prose["doublea"];
        Assert.Equal(ProseModel.ProseType.DoubleAgentAgree, prose.Type);
    }

    [Fact]
    public void Parse_SurpriseEntry_SetsAmbushedType()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("surprise", "\r\nAmbushed\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        var prose = package.Prose["surprise"];
        Assert.Equal(ProseModel.ProseType.Ambushed, prose.Type);
    }

    #endregion

    #region Surprise variants

    [Fact]
    public void Parse_SurpriseLEntry_SetsAmbushedLostType()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("surpriseL", "\r\nLost ambush\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        var prose = package.Prose["surpriseL"];
        Assert.Equal(ProseModel.ProseType.AmbushedLost, prose.Type);
        Assert.Equal("L", prose.SecondaryId);
    }

    [Fact]
    public void Parse_SurpriseWEntry_SetsAmbushedWonType()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("surpriseW", "\r\nWon ambush\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        var prose = package.Prose["surpriseW"];
        Assert.Equal(ProseModel.ProseType.AmbushedWon, prose.Type);
        Assert.Equal("W", prose.SecondaryId);
    }

    #endregion

    #region CharacterInterrogate entries

    [Fact]
    public void Parse_InterEntry_SetsCharacterInterrogateType()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("inter1", "\r\nInterrogate text\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        var prose = package.Prose["inter1"];
        Assert.Equal(ProseModel.ProseType.CharacterInterrogate, prose.Type);
        Assert.Equal("1", prose.SecondaryId);
    }

    #endregion

    #region Leading bytes before '*'

    [Fact]
    public void Parse_LeadingBytesBeforeStar_SkipsToFirstStar()
    {
        var leading = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        var entry = ProseTestDataGenerator.BuildProseEntry("lounge", "\r\nAfter leading\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end }, leadingBytes: leading);
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        Assert.Single(package.Prose);
        Assert.Equal("After leading", package.Prose["lounge"].Message);
    }

    #endregion

    #region Multiple entries

    [Fact]
    public void Parse_MultipleEntries_ReturnsAll()
    {
        var entry1 = ProseTestDataGenerator.BuildProseEntry("lounge", "\r\nFirst\r\n");
        var entry2 = ProseTestDataGenerator.BuildProseEntry("escape", "\r\nSecond\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry1, entry2, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        Assert.Equal(2, package.Prose.Count);
        Assert.Equal("First", package.Prose["lounge"].Message);
        Assert.Equal("Second", package.Prose["escape"].Message);
    }

    #endregion

    #region Message trimming

    [Fact]
    public void Parse_MessageWithLeadingAndTrailingNewlines_TrimsOnePair()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("lounge", "\r\nLine1\r\nLine2\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        var prose = package.Prose["lounge"];
        Assert.Equal("Line1\r\nLine2", prose.Message);
    }

    #endregion

    #region SetResult

    [Fact]
    public void SetResult_PopulatesProseOnPackageModel()
    {
        var entry = ProseTestDataGenerator.BuildProseEntry("lounge", "\r\nTest\r\n");
        var end = ProseTestDataGenerator.BuildEndEntry();
        var fileData = ProseTestDataGenerator.BuildProseFile(new[] { entry, end });
        ProseTestDataGenerator.WriteProseFile(_tempDir, fileData);

        var package = RunParser();

        Assert.NotEmpty(package.Prose);
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
