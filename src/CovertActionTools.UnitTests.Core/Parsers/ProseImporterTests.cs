using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class ProseImporterTests : IDisposable
{
    private readonly ProseImporter _importer;
    private readonly string _tempDir;

    public ProseImporterTests()
    {
        _importer = new ProseImporter(NullLogger<ProseImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"ProseImporterTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region GetMessage

    [Fact]
    public void GetMessage_ReturnsExpectedMessage()
    {
        var message = _importer.GetMessage();

        Assert.Equal("Processing prose..", message);
    }

    #endregion

    #region CheckIfValid

    [Fact]
    public void CheckIfValid_WithProseJson_ReturnsTrue()
    {
        WriteProseJson(new Dictionary<string, ProseModel>
        {
            ["lounge"] = new ProseModel { Type = ProseModel.ProseType.Lounge, Message = "Hello" }
        });

        var result = _importer.CheckIfValid(_tempDir);

        Assert.True(result);
    }

    [Fact]
    public void CheckIfValid_WithoutProseJson_ReturnsFalse()
    {
        var result = _importer.CheckIfValid(_tempDir);

        Assert.False(result);
    }

    #endregion

    #region Import and SetResult

    [Fact]
    public void Import_SingleEntry_SetsResultOnPackageModel()
    {
        var prose = new Dictionary<string, ProseModel>
        {
            ["advice1"] = new ProseModel
            {
                Type = ProseModel.ProseType.Advice,
                SecondaryId = "1",
                Message = "Take this advice"
            }
        };
        WriteProseJson(prose);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var packageModel = new PackageModel();
        _importer.SetResult(packageModel);

        Assert.Single(packageModel.Prose);
        Assert.True(packageModel.Prose.ContainsKey("advice1"));
        Assert.Equal(ProseModel.ProseType.Advice, packageModel.Prose["advice1"].Type);
        Assert.Equal("1", packageModel.Prose["advice1"].SecondaryId);
        Assert.Equal("Take this advice", packageModel.Prose["advice1"].Message);
    }

    [Fact]
    public void Import_MultipleEntries_AllEntriesImported()
    {
        var prose = new Dictionary<string, ProseModel>
        {
            ["nice0"] = new ProseModel
            {
                Type = ProseModel.ProseType.CharacterCapture,
                SecondaryId = "0",
                Message = "Character captured"
            },
            ["escape"] = new ProseModel
            {
                Type = ProseModel.ProseType.Escape,
                SecondaryId = "",
                Message = "You escaped"
            },
            ["doublea"] = new ProseModel
            {
                Type = ProseModel.ProseType.DoubleAgentAgree,
                SecondaryId = "",
                Message = "Double agent agrees"
            }
        };
        WriteProseJson(prose);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var packageModel = new PackageModel();
        _importer.SetResult(packageModel);

        Assert.Equal(3, packageModel.Prose.Count);
        Assert.True(packageModel.Prose.ContainsKey("nice0"));
        Assert.True(packageModel.Prose.ContainsKey("escape"));
        Assert.True(packageModel.Prose.ContainsKey("doublea"));
    }

    [Fact]
    public void Import_RunStep_ReturnsTrue()
    {
        WriteProseJson(new Dictionary<string, ProseModel>
        {
            ["grilled"] = new ProseModel
            {
                Type = ProseModel.ProseType.Interrogated,
                SecondaryId = "",
                Message = "Interrogation text"
            }
        });

        _importer.Start(_tempDir);
        var done = _importer.RunStep();

        Assert.True(done);
    }

    [Fact]
    public void Import_PreservesAllProseTypes()
    {
        var prose = new Dictionary<string, ProseModel>
        {
            ["nice2"] = new ProseModel
            {
                Type = ProseModel.ProseType.MastermindCapture,
                SecondaryId = "2",
                Message = "Mastermind captured"
            },
            ["surpriseL"] = new ProseModel
            {
                Type = ProseModel.ProseType.AmbushedLost,
                SecondaryId = "L",
                Message = "Ambush lost"
            }
        };
        WriteProseJson(prose);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var packageModel = new PackageModel();
        _importer.SetResult(packageModel);

        Assert.Equal(ProseModel.ProseType.MastermindCapture, packageModel.Prose["nice2"].Type);
        Assert.Equal(ProseModel.ProseType.AmbushedLost, packageModel.Prose["surpriseL"].Type);
    }

    #endregion

    #region Helpers

    private void WriteProseJson(Dictionary<string, ProseModel> prose)
    {
        var json = JsonSerializer.Serialize(prose, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_tempDir, "PROSE.json"), json);
    }

    #endregion
}
