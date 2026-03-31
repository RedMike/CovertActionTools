using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class TextExporterTests : IDisposable
{
    private readonly TextExporter _exporter;
    private readonly string _tempDir;

    public TextExporterTests()
    {
        _exporter = new TextExporter(NullLogger<TextExporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"TextExporterTests_{Guid.NewGuid():N}");
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
        var message = _exporter.GetMessage();

        Assert.Equal("Processing texts..", message);
    }

    #endregion

    #region Export creates TEXT.json

    [Fact]
    public void Export_SingleEntry_CreatesTextJsonFile()
    {
        var model = CreatePackageModelWithTexts(new Dictionary<string, TextModel>
        {
            ["FLUF01"] = new TextModel
            {
                Id = 1,
                Type = TextModel.StringType.Fluff,
                Message = "Some fluff text"
            }
        });

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        Assert.True(File.Exists(Path.Combine(_tempDir, "TEXT.json")));
    }

    [Fact]
    public void Export_SingleEntry_JsonContainsCorrectData()
    {
        var texts = new Dictionary<string, TextModel>
        {
            ["SORG01"] = new TextModel
            {
                Id = 1,
                Type = TextModel.StringType.SenderOrganisation,
                Message = "The Red Hand"
            }
        };
        var model = CreatePackageModelWithTexts(texts);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "TEXT.json"));
        var result = JsonSerializer.Deserialize<Dictionary<string, TextModel>>(json);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result.ContainsKey("SORG01"));
        Assert.Equal(TextModel.StringType.SenderOrganisation, result["SORG01"].Type);
        Assert.Equal(1, result["SORG01"].Id);
        Assert.Equal("The Red Hand", result["SORG01"].Message);
    }

    [Fact]
    public void Export_MultipleEntries_AllEntriesPresent()
    {
        var texts = new Dictionary<string, TextModel>
        {
            ["FLUF01"] = new TextModel
            {
                Id = 1,
                Type = TextModel.StringType.Fluff,
                Message = "Fluff message"
            },
            ["ALRT02"] = new TextModel
            {
                Id = 2,
                Type = TextModel.StringType.Alert,
                Message = "Alert message"
            },
            ["MSG0103"] = new TextModel
            {
                Id = 3,
                Type = TextModel.StringType.CrimeMessage,
                Message = "Crime message",
                CrimeId = 1
            }
        };
        var model = CreatePackageModelWithTexts(texts);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "TEXT.json"));
        var result = JsonSerializer.Deserialize<Dictionary<string, TextModel>>(json);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey("FLUF01"));
        Assert.True(result.ContainsKey("ALRT02"));
        Assert.True(result.ContainsKey("MSG0103"));
    }

    [Fact]
    public void Export_CrimeMessageEntry_PreservesCrimeId()
    {
        var texts = new Dictionary<string, TextModel>
        {
            ["MSG0502"] = new TextModel
            {
                Id = 2,
                Type = TextModel.StringType.CrimeMessage,
                Message = "A crime happened",
                CrimeId = 5
            }
        };
        var model = CreatePackageModelWithTexts(texts);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var json = File.ReadAllText(Path.Combine(_tempDir, "TEXT.json"));
        var result = JsonSerializer.Deserialize<Dictionary<string, TextModel>>(json);

        Assert.NotNull(result);
        Assert.Equal(5, result["MSG0502"].CrimeId);
    }

    #endregion

    #region Empty data handling

    [Fact]
    public void Export_EmptyDictionary_DoesNotCreateFile()
    {
        var model = CreatePackageModelWithTexts(new Dictionary<string, TextModel>());

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        Assert.False(File.Exists(Path.Combine(_tempDir, "TEXT.json")));
    }

    #endregion

    #region RunStep returns done

    [Fact]
    public void RunStep_WithData_ReturnsTrue()
    {
        var texts = new Dictionary<string, TextModel>
        {
            ["RLOC01"] = new TextModel
            {
                Id = 1,
                Type = TextModel.StringType.ReceiverLocation,
                Message = "Moscow"
            }
        };
        var model = CreatePackageModelWithTexts(texts);

        _exporter.Start(_tempDir, model);
        var done = _exporter.RunStep();

        Assert.True(done);
    }

    #endregion

    #region Helpers

    private static PackageModel CreatePackageModelWithTexts(Dictionary<string, TextModel> texts)
    {
        return new PackageModel
        {
            Texts = texts
        };
    }

    #endregion
}
