using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class TextExporterImporterRoundtripTests : IDisposable
{
    private readonly TextExporter _exporter;
    private readonly TextImporter _importer;
    private readonly string _tempDir;

    public TextExporterImporterRoundtripTests()
    {
        _exporter = new TextExporter(NullLogger<TextExporter>.Instance);
        _importer = new TextImporter(NullLogger<TextImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"TextExporterImporterRoundtrip_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Single entry roundtrips

    [Fact]
    public void Roundtrip_FluffEntry_PreservesAllProperties()
    {
        var texts = new Dictionary<string, TextModel>
        {
            ["FLUF01"] = new TextModel
            {
                Id = 1,
                Type = TextModel.StringType.Fluff,
                Message = "The weather is nice today.",
                CrimeId = null
            }
        };

        var result = ExportThenImport(texts);

        Assert.Single(result);
        Assert.True(result.ContainsKey("FLUF01"));
        Assert.Equal(1, result["FLUF01"].Id);
        Assert.Equal(TextModel.StringType.Fluff, result["FLUF01"].Type);
        Assert.Equal("The weather is nice today.", result["FLUF01"].Message);
        Assert.Null(result["FLUF01"].CrimeId);
    }

    [Fact]
    public void Roundtrip_CrimeMessageEntry_PreservesCrimeId()
    {
        var texts = new Dictionary<string, TextModel>
        {
            ["MSG0502"] = new TextModel
            {
                Id = 2,
                Type = TextModel.StringType.CrimeMessage,
                Message = "A bank robbery is underway at $SNDLOC.",
                CrimeId = 5
            }
        };

        var result = ExportThenImport(texts);

        Assert.Equal(5, result["MSG0502"].CrimeId);
        Assert.Equal(TextModel.StringType.CrimeMessage, result["MSG0502"].Type);
        Assert.Equal(2, result["MSG0502"].Id);
    }

    [Fact]
    public void Roundtrip_AlertEntry_PreservesMessage()
    {
        var texts = new Dictionary<string, TextModel>
        {
            ["ALRT03"] = new TextModel
            {
                Id = 3,
                Type = TextModel.StringType.Alert,
                Message = "URGENT: Suspect spotted near the embassy!"
            }
        };

        var result = ExportThenImport(texts);

        Assert.Equal("URGENT: Suspect spotted near the embassy!", result["ALRT03"].Message);
    }

    #endregion

    #region Multiple entry roundtrips

    [Fact]
    public void Roundtrip_MultipleEntries_AllPreserved()
    {
        var texts = new Dictionary<string, TextModel>
        {
            ["SORG01"] = new TextModel
            {
                Id = 1,
                Type = TextModel.StringType.SenderOrganisation,
                Message = "The Stasi"
            },
            ["RORG02"] = new TextModel
            {
                Id = 2,
                Type = TextModel.StringType.ReceiverOrganisation,
                Message = "PLA"
            },
            ["SLOC03"] = new TextModel
            {
                Id = 3,
                Type = TextModel.StringType.SenderLocation,
                Message = "East Berlin"
            },
            ["RLOC04"] = new TextModel
            {
                Id = 4,
                Type = TextModel.StringType.ReceiverLocation,
                Message = "Moscow"
            }
        };

        var result = ExportThenImport(texts);

        Assert.Equal(4, result.Count);
        foreach (var kvp in texts)
        {
            Assert.True(result.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value.Id, result[kvp.Key].Id);
            Assert.Equal(kvp.Value.Type, result[kvp.Key].Type);
            Assert.Equal(kvp.Value.Message, result[kvp.Key].Message);
            Assert.Equal(kvp.Value.CrimeId, result[kvp.Key].CrimeId);
        }
    }

    [Fact]
    public void Roundtrip_AllStringTypes_PreservesTypeEnumValues()
    {
        var texts = new Dictionary<string, TextModel>
        {
            ["MSG0101"] = new TextModel { Id = 1, Type = TextModel.StringType.CrimeMessage, Message = "Crime msg", CrimeId = 1 },
            ["SORG01"] = new TextModel { Id = 1, Type = TextModel.StringType.SenderOrganisation, Message = "Sender org" },
            ["RORG01"] = new TextModel { Id = 1, Type = TextModel.StringType.ReceiverOrganisation, Message = "Receiver org" },
            ["SLOC01"] = new TextModel { Id = 1, Type = TextModel.StringType.SenderLocation, Message = "Sender loc" },
            ["RLOC01"] = new TextModel { Id = 1, Type = TextModel.StringType.ReceiverLocation, Message = "Receiver loc" },
            ["FLUF01"] = new TextModel { Id = 1, Type = TextModel.StringType.Fluff, Message = "Fluff" },
            ["ALRT01"] = new TextModel { Id = 1, Type = TextModel.StringType.Alert, Message = "Alert" },
            ["AIDD01"] = new TextModel { Id = 1, Type = TextModel.StringType.AidingOrganisation, Message = "Aiding org" }
        };

        var result = ExportThenImport(texts);

        Assert.Equal(8, result.Count);
        Assert.Equal(TextModel.StringType.CrimeMessage, result["MSG0101"].Type);
        Assert.Equal(TextModel.StringType.SenderOrganisation, result["SORG01"].Type);
        Assert.Equal(TextModel.StringType.ReceiverOrganisation, result["RORG01"].Type);
        Assert.Equal(TextModel.StringType.SenderLocation, result["SLOC01"].Type);
        Assert.Equal(TextModel.StringType.ReceiverLocation, result["RLOC01"].Type);
        Assert.Equal(TextModel.StringType.Fluff, result["FLUF01"].Type);
        Assert.Equal(TextModel.StringType.Alert, result["ALRT01"].Type);
        Assert.Equal(TextModel.StringType.AidingOrganisation, result["AIDD01"].Type);
    }

    [Fact]
    public void Roundtrip_MultipleCrimeMessages_PreservesDistinctCrimeIds()
    {
        var texts = new Dictionary<string, TextModel>
        {
            ["MSG0101"] = new TextModel { Id = 1, Type = TextModel.StringType.CrimeMessage, Message = "Crime 1 msg 1", CrimeId = 1 },
            ["MSG0102"] = new TextModel { Id = 2, Type = TextModel.StringType.CrimeMessage, Message = "Crime 1 msg 2", CrimeId = 1 },
            ["MSG0301"] = new TextModel { Id = 1, Type = TextModel.StringType.CrimeMessage, Message = "Crime 3 msg 1", CrimeId = 3 }
        };

        var result = ExportThenImport(texts);

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result["MSG0101"].CrimeId);
        Assert.Equal(1, result["MSG0102"].CrimeId);
        Assert.Equal(3, result["MSG0301"].CrimeId);
    }

    #endregion

    #region CheckIfValid after export

    [Fact]
    public void Roundtrip_AfterExport_ImporterConsidersPathValid()
    {
        var texts = new Dictionary<string, TextModel>
        {
            ["FLUF01"] = new TextModel
            {
                Id = 1,
                Type = TextModel.StringType.Fluff,
                Message = "Some text"
            }
        };
        var model = new PackageModel { Texts = texts };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        Assert.True(_importer.CheckIfValid(_tempDir));
    }

    #endregion

    #region Helpers

    private Dictionary<string, TextModel> ExportThenImport(Dictionary<string, TextModel> texts)
    {
        var model = new PackageModel { Texts = texts };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        _importer.Start(_tempDir);
        _importer.RunStep();

        var resultModel = new PackageModel();
        _importer.SetResult(resultModel);

        return resultModel.Texts;
    }

    #endregion
}
