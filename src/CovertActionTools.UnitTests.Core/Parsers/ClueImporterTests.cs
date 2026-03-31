using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class ClueImporterTests : IDisposable
{
    private readonly ClueImporter _importer;
    private readonly string _tempDir;

    public ClueImporterTests()
    {
        _importer = new ClueImporter(NullLogger<ClueImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"ClueImporterTests_{Guid.NewGuid():N}");
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
    public void GetMessage_ReturnsExpectedString()
    {
        Assert.Equal("Processing clues..", _importer.GetMessage());
    }

    #endregion

    #region CheckIfValid

    [Fact]
    public void CheckIfValid_FileExists_ReturnsTrue()
    {
        WriteCluesJson(new Dictionary<string, ClueModel>
        {
            ["C01"] = new ClueModel { Type = ClueType.Vehicle, Id = 1, Message = "test" }
        });

        Assert.True(_importer.CheckIfValid(_tempDir));
    }

    [Fact]
    public void CheckIfValid_FileMissing_ReturnsFalse()
    {
        Assert.False(_importer.CheckIfValid(_tempDir));
    }

    #endregion

    #region Import deserializes correctly

    [Fact]
    public void Import_DeserializesClueModel()
    {
        var clues = new Dictionary<string, ClueModel>
        {
            ["C0102"] = new ClueModel
            {
                Type = ClueType.AirlineTicket,
                Id = 2,
                CrimeId = 1,
                Source = ClueModel.ClueSource.FileRecordSearch,
                Message = "Ticket to Paris."
            }
        };
        WriteCluesJson(clues);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.Single(model.Clues);
        var clue = model.Clues["C0102"];
        Assert.Equal(ClueType.AirlineTicket, clue.Type);
        Assert.Equal(2, clue.Id);
        Assert.Equal(1, clue.CrimeId);
        Assert.Equal(ClueModel.ClueSource.FileRecordSearch, clue.Source);
        Assert.Equal("Ticket to Paris.", clue.Message);
    }

    [Fact]
    public void Import_MultipleClues_AllPresent()
    {
        var clues = new Dictionary<string, ClueModel>
        {
            ["C01"] = new ClueModel { Type = ClueType.Vehicle, Id = 1, Message = "Car spotted." },
            ["C11"] = new ClueModel { Type = ClueType.Weapon, Id = 1, Message = "Gun found." },
            ["C0200"] = new ClueModel
            {
                Type = ClueType.IdentityDocument, Id = 0, CrimeId = 2, Message = "Fake passport."
            }
        };
        WriteCluesJson(clues);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.Equal(3, model.Clues.Count);
    }

    #endregion

    #region SetResult populates correct field

    [Fact]
    public void SetResult_PopulatesCluesField()
    {
        var clues = new Dictionary<string, ClueModel>
        {
            ["C51"] = new ClueModel
            {
                Type = ClueType.MoneyHundreds, Id = 1, Source = ClueModel.ClueSource.LocalAuthorities,
                Message = "Cash discovered."
            }
        };
        WriteCluesJson(clues);

        _importer.Start(_tempDir);
        _importer.RunStep();

        var model = new PackageModel();
        _importer.SetResult(model);

        Assert.NotNull(model.Clues);
        Assert.True(model.Clues.ContainsKey("C51"));
        Assert.Empty(model.Plots);
    }

    #endregion

    #region Helpers

    private void WriteCluesJson(Dictionary<string, ClueModel> clues)
    {
        var json = JsonSerializer.Serialize(clues, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_tempDir, "CLUES.json"), json);
    }

    #endregion
}
