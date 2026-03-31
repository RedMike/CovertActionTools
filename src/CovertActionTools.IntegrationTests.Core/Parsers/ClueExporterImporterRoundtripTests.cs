using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class ClueExporterImporterRoundtripTests : IDisposable
{
    private readonly ClueExporter _exporter;
    private readonly ClueImporter _importer;
    private readonly string _tempDir;

    public ClueExporterImporterRoundtripTests()
    {
        _exporter = new ClueExporter(NullLogger<ClueExporter>.Instance);
        _importer = new ClueImporter(NullLogger<ClueImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"ClueRoundtrip_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Roundtrip tests

    [Fact]
    public void Roundtrip_SingleClue_AllPropertiesMatch()
    {
        var original = new Dictionary<string, ClueModel>
        {
            ["C01"] = new ClueModel
            {
                Type = ClueType.Vehicle,
                Id = 1,
                CrimeId = null,
                Source = ClueModel.ClueSource.WireTap,
                Message = "A suspicious vehicle was spotted near the embassy."
            }
        };

        var result = ExportAndImport(original);

        Assert.Single(result);
        var clue = result["C01"];
        Assert.Equal(ClueType.Vehicle, clue.Type);
        Assert.Equal(1, clue.Id);
        Assert.Null(clue.CrimeId);
        Assert.Equal(ClueModel.ClueSource.WireTap, clue.Source);
        Assert.Equal("A suspicious vehicle was spotted near the embassy.", clue.Message);
    }

    [Fact]
    public void Roundtrip_ClueWithCrimeId_PreservesCrimeId()
    {
        var original = new Dictionary<string, ClueModel>
        {
            ["C0203"] = new ClueModel
            {
                Type = ClueType.Weapon,
                Id = 3,
                CrimeId = 2,
                Source = ClueModel.ClueSource.InterpolDatabase,
                Message = "Weapon traced to arms dealer in Berlin."
            }
        };

        var result = ExportAndImport(original);

        Assert.Equal(2, result["C0203"].CrimeId);
        Assert.Equal(3, result["C0203"].Id);
    }

    [Fact]
    public void Roundtrip_MultipleClues_AllPreserved()
    {
        var original = new Dictionary<string, ClueModel>
        {
            ["C01"] = new ClueModel
            {
                Type = ClueType.Address, Id = 1, Source = ClueModel.ClueSource.LocalInformant,
                Message = "Safe house at 42 Elm Street."
            },
            ["C0100"] = new ClueModel
            {
                Type = ClueType.AirlineTicket, Id = 0, CrimeId = 1,
                Source = ClueModel.ClueSource.FileRecordSearch, Message = "Flight to Moscow."
            },
            ["C41"] = new ClueModel
            {
                Type = ClueType.Telegram, Id = 1, Source = ClueModel.ClueSource.CovertSurveillance,
                Message = "Encrypted message intercepted."
            },
            ["C0300"] = new ClueModel
            {
                Type = ClueType.MoneyThousands, Id = 0, CrimeId = 3,
                Source = ClueModel.ClueSource.LocalAuthorities, Message = "Large cash transfer detected."
            },
            ["C71"] = new ClueModel
            {
                Type = ClueType.IdentityDocument, Id = 1,
                Source = ClueModel.ClueSource.ClandestinePhoto, Message = "Forged passport found."
            }
        };

        var result = ExportAndImport(original);

        Assert.Equal(5, result.Count);
        foreach (var key in original.Keys)
        {
            Assert.True(result.ContainsKey(key));
            Assert.Equal(original[key].Type, result[key].Type);
            Assert.Equal(original[key].Id, result[key].Id);
            Assert.Equal(original[key].CrimeId, result[key].CrimeId);
            Assert.Equal(original[key].Source, result[key].Source);
            Assert.Equal(original[key].Message, result[key].Message);
        }
    }

    [Fact]
    public void Roundtrip_AllClueTypes_PreservedCorrectly()
    {
        var original = new Dictionary<string, ClueModel>
        {
            ["V"] = new ClueModel { Type = ClueType.Vehicle, Id = 0, Message = "v" },
            ["W"] = new ClueModel { Type = ClueType.Weapon, Id = 0, Message = "w" },
            ["A"] = new ClueModel { Type = ClueType.Address, Id = 0, Message = "a" },
            ["T"] = new ClueModel { Type = ClueType.AirlineTicket, Id = 0, Message = "t" },
            ["G"] = new ClueModel { Type = ClueType.Telegram, Id = 0, Message = "g" },
            ["H"] = new ClueModel { Type = ClueType.MoneyHundreds, Id = 0, Message = "h" },
            ["K"] = new ClueModel { Type = ClueType.MoneyThousands, Id = 0, Message = "k" },
            ["I"] = new ClueModel { Type = ClueType.IdentityDocument, Id = 0, Message = "i" }
        };

        var result = ExportAndImport(original);

        Assert.Equal(8, result.Count);
        Assert.Equal(ClueType.Vehicle, result["V"].Type);
        Assert.Equal(ClueType.Weapon, result["W"].Type);
        Assert.Equal(ClueType.Address, result["A"].Type);
        Assert.Equal(ClueType.AirlineTicket, result["T"].Type);
        Assert.Equal(ClueType.Telegram, result["G"].Type);
        Assert.Equal(ClueType.MoneyHundreds, result["H"].Type);
        Assert.Equal(ClueType.MoneyThousands, result["K"].Type);
        Assert.Equal(ClueType.IdentityDocument, result["I"].Type);
    }

    [Fact]
    public void Roundtrip_AllClueSources_PreservedCorrectly()
    {
        var original = new Dictionary<string, ClueModel>
        {
            ["S0"] = new ClueModel
            {
                Type = ClueType.Vehicle, Id = 0, Source = ClueModel.ClueSource.ClandestinePhoto, Message = "s0"
            },
            ["S1"] = new ClueModel
            {
                Type = ClueType.Vehicle, Id = 0, Source = ClueModel.ClueSource.WireTap, Message = "s1"
            },
            ["S2"] = new ClueModel
            {
                Type = ClueType.Vehicle, Id = 0, Source = ClueModel.ClueSource.CovertSurveillance, Message = "s2"
            },
            ["S3"] = new ClueModel
            {
                Type = ClueType.Vehicle, Id = 0, Source = ClueModel.ClueSource.FileRecordSearch, Message = "s3"
            },
            ["S4"] = new ClueModel
            {
                Type = ClueType.Vehicle, Id = 0, Source = ClueModel.ClueSource.LocalInformant, Message = "s4"
            },
            ["S5"] = new ClueModel
            {
                Type = ClueType.Vehicle, Id = 0, Source = ClueModel.ClueSource.InterpolDatabase, Message = "s5"
            },
            ["S6"] = new ClueModel
            {
                Type = ClueType.Vehicle, Id = 0, Source = ClueModel.ClueSource.LocalAuthorities, Message = "s6"
            }
        };

        var result = ExportAndImport(original);

        Assert.Equal(7, result.Count);
        Assert.Equal(ClueModel.ClueSource.ClandestinePhoto, result["S0"].Source);
        Assert.Equal(ClueModel.ClueSource.WireTap, result["S1"].Source);
        Assert.Equal(ClueModel.ClueSource.CovertSurveillance, result["S2"].Source);
        Assert.Equal(ClueModel.ClueSource.FileRecordSearch, result["S3"].Source);
        Assert.Equal(ClueModel.ClueSource.LocalInformant, result["S4"].Source);
        Assert.Equal(ClueModel.ClueSource.InterpolDatabase, result["S5"].Source);
        Assert.Equal(ClueModel.ClueSource.LocalAuthorities, result["S6"].Source);
    }

    #endregion

    #region Helpers

    private Dictionary<string, ClueModel> ExportAndImport(Dictionary<string, ClueModel> clues)
    {
        var exportModel = new PackageModel { Clues = clues };
        _exporter.Start(_tempDir, exportModel);
        _exporter.RunStep();

        _importer.Start(_tempDir);
        _importer.RunStep();

        var importModel = new PackageModel();
        _importer.SetResult(importModel);
        return importModel.Clues;
    }

    #endregion
}
