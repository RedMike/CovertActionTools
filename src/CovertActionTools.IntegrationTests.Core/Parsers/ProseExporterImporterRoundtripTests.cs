using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class ProseExporterImporterRoundtripTests : IDisposable
{
    private readonly ProseExporter _exporter;
    private readonly ProseImporter _importer;
    private readonly string _tempDir;

    public ProseExporterImporterRoundtripTests()
    {
        _exporter = new ProseExporter(NullLogger<ProseExporter>.Instance);
        _importer = new ProseImporter(NullLogger<ProseImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"ProseExporterImporterRoundtrip_{Guid.NewGuid():N}");
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
    public void Roundtrip_AdviceEntry_PreservesAllProperties()
    {
        var prose = new Dictionary<string, ProseModel>
        {
            ["advice1"] = new ProseModel
            {
                Type = ProseModel.ProseType.Advice,
                SecondaryId = "1",
                Message = "You should be careful in this area."
            }
        };

        var result = ExportThenImport(prose);

        Assert.Single(result);
        Assert.True(result.ContainsKey("advice1"));
        Assert.Equal(ProseModel.ProseType.Advice, result["advice1"].Type);
        Assert.Equal("1", result["advice1"].SecondaryId);
        Assert.Equal("You should be careful in this area.", result["advice1"].Message);
    }

    [Fact]
    public void Roundtrip_LoungeEntry_PreservesEmptySecondaryId()
    {
        var prose = new Dictionary<string, ProseModel>
        {
            ["lounge"] = new ProseModel
            {
                Type = ProseModel.ProseType.Lounge,
                SecondaryId = "",
                Message = "The lounge is quiet tonight."
            }
        };

        var result = ExportThenImport(prose);

        Assert.Equal("", result["lounge"].SecondaryId);
        Assert.Equal(ProseModel.ProseType.Lounge, result["lounge"].Type);
    }

    [Fact]
    public void Roundtrip_EscapeEntry_PreservesMessage()
    {
        var prose = new Dictionary<string, ProseModel>
        {
            ["escape"] = new ProseModel
            {
                Type = ProseModel.ProseType.Escape,
                SecondaryId = "",
                Message = "The suspect has escaped through the back door!"
            }
        };

        var result = ExportThenImport(prose);

        Assert.Equal("The suspect has escaped through the back door!", result["escape"].Message);
    }

    #endregion

    #region Multiple entry roundtrips

    [Fact]
    public void Roundtrip_MultipleEntries_AllPreserved()
    {
        var prose = new Dictionary<string, ProseModel>
        {
            ["advice1"] = new ProseModel
            {
                Type = ProseModel.ProseType.Advice,
                SecondaryId = "1",
                Message = "Advice one"
            },
            ["nice0"] = new ProseModel
            {
                Type = ProseModel.ProseType.CharacterCapture,
                SecondaryId = "0",
                Message = "Character captured"
            },
            ["nice2"] = new ProseModel
            {
                Type = ProseModel.ProseType.MastermindCapture,
                SecondaryId = "2",
                Message = "Mastermind captured"
            },
            ["followed"] = new ProseModel
            {
                Type = ProseModel.ProseType.CarFollowed,
                SecondaryId = "",
                Message = "Your car is being followed"
            },
            ["surprise"] = new ProseModel
            {
                Type = ProseModel.ProseType.Ambushed,
                SecondaryId = "",
                Message = "Ambush!"
            }
        };

        var result = ExportThenImport(prose);

        Assert.Equal(5, result.Count);
        foreach (var kvp in prose)
        {
            Assert.True(result.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value.Type, result[kvp.Key].Type);
            Assert.Equal(kvp.Value.SecondaryId, result[kvp.Key].SecondaryId);
            Assert.Equal(kvp.Value.Message, result[kvp.Key].Message);
        }
    }

    [Fact]
    public void Roundtrip_AllProseTypes_PreservesTypeEnumValues()
    {
        var prose = new Dictionary<string, ProseModel>
        {
            ["grilled"] = new ProseModel { Type = ProseModel.ProseType.Interrogated, SecondaryId = "", Message = "Grilled" },
            ["carcap"] = new ProseModel { Type = ProseModel.ProseType.CarFollowedEnd, SecondaryId = "", Message = "Car captured" },
            ["doublea"] = new ProseModel { Type = ProseModel.ProseType.DoubleAgentAgree, SecondaryId = "", Message = "Double agent" },
            ["inter1"] = new ProseModel { Type = ProseModel.ProseType.CharacterInterrogate, SecondaryId = "1", Message = "Interrogate" },
            ["surpriseL"] = new ProseModel { Type = ProseModel.ProseType.AmbushedLost, SecondaryId = "L", Message = "Lost" },
            ["surpriseW"] = new ProseModel { Type = ProseModel.ProseType.AmbushedWon, SecondaryId = "W", Message = "Won" }
        };

        var result = ExportThenImport(prose);

        Assert.Equal(6, result.Count);
        Assert.Equal(ProseModel.ProseType.Interrogated, result["grilled"].Type);
        Assert.Equal(ProseModel.ProseType.CarFollowedEnd, result["carcap"].Type);
        Assert.Equal(ProseModel.ProseType.DoubleAgentAgree, result["doublea"].Type);
        Assert.Equal(ProseModel.ProseType.CharacterInterrogate, result["inter1"].Type);
        Assert.Equal(ProseModel.ProseType.AmbushedLost, result["surpriseL"].Type);
        Assert.Equal(ProseModel.ProseType.AmbushedWon, result["surpriseW"].Type);
    }

    #endregion

    #region CheckIfValid after export

    [Fact]
    public void Roundtrip_AfterExport_ImporterConsidersPathValid()
    {
        var prose = new Dictionary<string, ProseModel>
        {
            ["lounge"] = new ProseModel
            {
                Type = ProseModel.ProseType.Lounge,
                SecondaryId = "",
                Message = "Lounge text"
            }
        };
        var model = new PackageModel { Prose = prose };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        Assert.True(_importer.CheckIfValid(_tempDir));
    }

    #endregion

    #region Helpers

    private Dictionary<string, ProseModel> ExportThenImport(Dictionary<string, ProseModel> prose)
    {
        var model = new PackageModel { Prose = prose };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        _importer.Start(_tempDir);
        _importer.RunStep();

        var resultModel = new PackageModel();
        _importer.SetResult(resultModel);

        return resultModel.Prose;
    }

    #endregion
}
