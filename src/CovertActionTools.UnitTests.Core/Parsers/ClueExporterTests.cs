using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class ClueExporterTests : IDisposable
{
    private readonly ClueExporter _exporter;
    private readonly string _tempDir;

    public ClueExporterTests()
    {
        _exporter = new ClueExporter(NullLogger<ClueExporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"ClueExporterTests_{Guid.NewGuid():N}");
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
        Assert.Equal("Processing clues..", _exporter.GetMessage());
    }

    #endregion

    #region Export writes correct file

    [Fact]
    public void Export_SingleClue_WritesCluesJsonFile()
    {
        var model = new PackageModel
        {
            Clues = new Dictionary<string, ClueModel>
            {
                ["C01"] = new ClueModel
                {
                    Type = ClueType.Vehicle,
                    Id = 1,
                    CrimeId = null,
                    Source = ClueModel.ClueSource.WireTap,
                    Message = "A suspicious vehicle was spotted."
                }
            }
        };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "CLUES.json");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Export_SingleClue_FileDeserializesCorrectly()
    {
        var model = new PackageModel
        {
            Clues = new Dictionary<string, ClueModel>
            {
                ["C01"] = new ClueModel
                {
                    Type = ClueType.Weapon,
                    Id = 3,
                    CrimeId = 2,
                    Source = ClueModel.ClueSource.InterpolDatabase,
                    Message = "Weapon traced to arms dealer."
                }
            }
        };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "CLUES.json"));
        var result = JsonSerializer.Deserialize<Dictionary<string, ClueModel>>(json);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(ClueType.Weapon, result["C01"].Type);
        Assert.Equal(3, result["C01"].Id);
        Assert.Equal(2, result["C01"].CrimeId);
        Assert.Equal(ClueModel.ClueSource.InterpolDatabase, result["C01"].Source);
        Assert.Equal("Weapon traced to arms dealer.", result["C01"].Message);
    }

    #endregion

    #region Multiple clues

    [Fact]
    public void Export_MultipleClues_AllEntriesPresent()
    {
        var model = new PackageModel
        {
            Clues = new Dictionary<string, ClueModel>
            {
                ["C01"] = new ClueModel
                {
                    Type = ClueType.Address, Id = 1, Source = ClueModel.ClueSource.LocalInformant,
                    Message = "Address found."
                },
                ["C0200"] = new ClueModel
                {
                    Type = ClueType.AirlineTicket, Id = 0, CrimeId = 2,
                    Source = ClueModel.ClueSource.FileRecordSearch, Message = "Ticket to Berlin."
                },
                ["C11"] = new ClueModel
                {
                    Type = ClueType.Telegram, Id = 1, Source = ClueModel.ClueSource.CovertSurveillance,
                    Message = "Encrypted telegram intercepted."
                }
            }
        };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "CLUES.json"));
        var result = JsonSerializer.Deserialize<Dictionary<string, ClueModel>>(json);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey("C01"));
        Assert.True(result.ContainsKey("C0200"));
        Assert.True(result.ContainsKey("C11"));
    }

    #endregion

    #region Empty data

    [Fact]
    public void Export_EmptyClues_DoesNotWriteFile()
    {
        var model = new PackageModel
        {
            Clues = new Dictionary<string, ClueModel>()
        };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "CLUES.json");
        Assert.False(File.Exists(filePath));
    }

    #endregion

    #region RunStep completion

    [Fact]
    public void RunStep_ReturnsTrueOnCompletion()
    {
        var model = new PackageModel
        {
            Clues = new Dictionary<string, ClueModel>
            {
                ["C01"] = new ClueModel
                {
                    Type = ClueType.MoneyHundreds, Id = 0, Source = ClueModel.ClueSource.LocalAuthorities,
                    Message = "Cash found."
                }
            }
        };

        _exporter.Start(_tempDir, model);
        var done = _exporter.RunStep();

        Assert.True(done);
    }

    #endregion
}
