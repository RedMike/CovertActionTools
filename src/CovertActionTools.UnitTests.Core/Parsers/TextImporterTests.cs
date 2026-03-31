using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class TextImporterTests : IDisposable
{
    private readonly TextImporter _importer;
    private readonly string _tempDir;

    public TextImporterTests()
    {
        _importer = new TextImporter(NullLogger<TextImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"TextImporterTests_{Guid.NewGuid():N}");
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

        Assert.Equal("Processing texts..", message);
    }

    #endregion

    #region CheckIfValid

    [Fact]
    public void CheckIfValid_WithTextJson_ReturnsTrue()
    {
        WriteTextJson(new Dictionary<string, TextModel>
        {
            ["FLUF01"] = new TextModel { Id = 1, Type = TextModel.StringType.Fluff, Message = "Hello" }
        });

        var result = _importer.CheckIfValid(_tempDir);

        Assert.True(result);
    }

    [Fact]
    public void CheckIfValid_WithoutTextJson_ReturnsFalse()
    {
        var result = _importer.CheckIfValid(_tempDir);

        Assert.False(result);
    }

    #endregion

    #region Import and SetResult

    [Fact]
    public void Import_SingleEntry_SetsResultOnPackageModel()
    {
        var texts = new Dictionary<string, TextModel>
        {
            ["SORG01"] = new TextModel
            {
                Id = 1,
                Type = TextModel.StringType.SenderOrganisation,
                Message = "The Stasi"
            }
        };
        WriteTextJson(texts);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var packageModel = new PackageModel();
        _importer.SetResult(packageModel);

        Assert.Single(packageModel.Texts);
        Assert.True(packageModel.Texts.ContainsKey("SORG01"));
        Assert.Equal(TextModel.StringType.SenderOrganisation, packageModel.Texts["SORG01"].Type);
        Assert.Equal(1, packageModel.Texts["SORG01"].Id);
        Assert.Equal("The Stasi", packageModel.Texts["SORG01"].Message);
    }

    [Fact]
    public void Import_MultipleEntries_AllEntriesImported()
    {
        var texts = new Dictionary<string, TextModel>
        {
            ["FLUF01"] = new TextModel
            {
                Id = 1,
                Type = TextModel.StringType.Fluff,
                Message = "Fluff text"
            },
            ["ALRT02"] = new TextModel
            {
                Id = 2,
                Type = TextModel.StringType.Alert,
                Message = "Alert text"
            },
            ["RLOC03"] = new TextModel
            {
                Id = 3,
                Type = TextModel.StringType.ReceiverLocation,
                Message = "Berlin"
            }
        };
        WriteTextJson(texts);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var packageModel = new PackageModel();
        _importer.SetResult(packageModel);

        Assert.Equal(3, packageModel.Texts.Count);
        Assert.True(packageModel.Texts.ContainsKey("FLUF01"));
        Assert.True(packageModel.Texts.ContainsKey("ALRT02"));
        Assert.True(packageModel.Texts.ContainsKey("RLOC03"));
    }

    [Fact]
    public void Import_RunStep_ReturnsTrue()
    {
        WriteTextJson(new Dictionary<string, TextModel>
        {
            ["SLOC01"] = new TextModel
            {
                Id = 1,
                Type = TextModel.StringType.SenderLocation,
                Message = "Paris"
            }
        });

        _importer.Start(_tempDir);
        var done = _importer.RunStep();

        Assert.True(done);
    }

    [Fact]
    public void Import_CrimeMessageEntry_PreservesCrimeId()
    {
        var texts = new Dictionary<string, TextModel>
        {
            ["MSG0301"] = new TextModel
            {
                Id = 1,
                Type = TextModel.StringType.CrimeMessage,
                Message = "Crime in progress",
                CrimeId = 3
            }
        };
        WriteTextJson(texts);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var packageModel = new PackageModel();
        _importer.SetResult(packageModel);

        Assert.Equal(3, packageModel.Texts["MSG0301"].CrimeId);
        Assert.Equal(TextModel.StringType.CrimeMessage, packageModel.Texts["MSG0301"].Type);
    }

    [Fact]
    public void Import_NonCrimeEntry_CrimeIdIsNull()
    {
        var texts = new Dictionary<string, TextModel>
        {
            ["AIDD01"] = new TextModel
            {
                Id = 1,
                Type = TextModel.StringType.AidingOrganisation,
                Message = "Aiding org text",
                CrimeId = null
            }
        };
        WriteTextJson(texts);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var packageModel = new PackageModel();
        _importer.SetResult(packageModel);

        Assert.Null(packageModel.Texts["AIDD01"].CrimeId);
    }

    #endregion

    #region Helpers

    private void WriteTextJson(Dictionary<string, TextModel> texts)
    {
        var json = JsonSerializer.Serialize(texts, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_tempDir, "TEXT.json"), json);
    }

    #endregion
}
