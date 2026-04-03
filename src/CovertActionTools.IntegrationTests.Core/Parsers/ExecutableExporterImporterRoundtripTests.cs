using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class ExecutableExporterImporterRoundtripTests : IDisposable
{
    private readonly ExecutableExporter _exporter;
    private readonly ExecutableImporter _importer;
    private readonly string _tempDir;

    public ExecutableExporterImporterRoundtripTests()
    {
        _exporter = new ExecutableExporter(NullLogger<ExecutableExporter>.Instance);
        _importer = new ExecutableImporter(NullLogger<ExecutableImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"ExecutableExporterImporterRoundtrip_{Guid.NewGuid():N}");
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
    public void Roundtrip_SingleExecutable_PreservesAllFields()
    {
        var executables = new Dictionary<string, ExecutableModel>
        {
            ["TEST"] = new ExecutableModel
            {
                DeadZone = new byte[] { 0x01, 0x02, 0x03 },
                RawPayloadData = new byte[] { 0x10, 0x20, 0x30, 0x40 },
                OriginalMzHeader = new byte[] { (byte)'M', (byte)'Z', 0x00, 0x01 },
                ExepackStub = new byte[] { 0x8C, 0xC0, 0x05 },
                Relocations = new ushort[] { 0x0000, 0x0010, 0x1000, 0x0020 },
                EntryCS = 0x0100,
                EntryIP = 0x0000,
                StackSS = 0x0200,
                StackSP = 0x0800
            }
        };

        var result = ExportThenImport(executables);

        Assert.Single(result);
        Assert.True(result.ContainsKey("TEST"));

        var exe = result["TEST"];
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, exe.DeadZone);
        Assert.Equal(new byte[] { 0x10, 0x20, 0x30, 0x40 }, exe.RawPayloadData);
        Assert.Equal(new byte[] { (byte)'M', (byte)'Z', 0x00, 0x01 }, exe.OriginalMzHeader);
        Assert.Equal(new byte[] { 0x8C, 0xC0, 0x05 }, exe.ExepackStub);
        Assert.Equal(new ushort[] { 0x0000, 0x0010, 0x1000, 0x0020 }, exe.Relocations);
        Assert.Equal(0x0100, exe.EntryCS);
        Assert.Equal(0x0000, exe.EntryIP);
        Assert.Equal(0x0200, exe.StackSS);
        Assert.Equal(0x0800, exe.StackSP);
    }

    [Fact]
    public void Roundtrip_MultipleExecutables_AllPreserved()
    {
        var executables = new Dictionary<string, ExecutableModel>
        {
            ["BUG"] = new ExecutableModel
            {
                DeadZone = new byte[] { 0xAA },
                RawPayloadData = new byte[] { 0xBB, 0xCC },
                OriginalMzHeader = new byte[] { (byte)'M', (byte)'Z' },
                ExepackStub = new byte[] { 0x01 },
                Relocations = new ushort[] { 0x0000, 0x0010 },
                EntryCS = 1, EntryIP = 2, StackSS = 3, StackSP = 4
            },
            ["TAC"] = new ExecutableModel
            {
                DeadZone = new byte[] { 0xDD },
                RawPayloadData = new byte[] { 0xEE, 0xFF },
                OriginalMzHeader = new byte[] { (byte)'M', (byte)'Z' },
                ExepackStub = new byte[] { 0x02 },
                Relocations = new ushort[] { 0x1000, 0x0020 },
                EntryCS = 5, EntryIP = 6, StackSS = 7, StackSP = 8
            }
        };

        var result = ExportThenImport(executables);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result["BUG"].EntryCS);
        Assert.Equal(5, result["TAC"].EntryCS);
    }

    [Fact]
    public void Roundtrip_EmptyArrays_PreservesCorrectly()
    {
        var executables = new Dictionary<string, ExecutableModel>
        {
            ["EMPTY"] = new ExecutableModel()
        };

        var result = ExportThenImport(executables);

        Assert.Single(result);
        var exe = result["EMPTY"];
        Assert.Empty(exe.DeadZone);
        Assert.Empty(exe.RawPayloadData);
        Assert.Empty(exe.OriginalMzHeader);
        Assert.Empty(exe.ExepackStub);
        Assert.Empty(exe.Relocations);
        Assert.Equal(0, exe.EntryCS);
    }

    #endregion

    #region CheckIfValid

    [Fact]
    public void Roundtrip_AfterExport_ImporterConsidersPathValid()
    {
        var executables = new Dictionary<string, ExecutableModel>
        {
            ["TEST"] = new ExecutableModel { RawPayloadData = new byte[] { 0x01 } }
        };
        var model = new PackageModel { Executables = executables };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        Assert.True(_importer.CheckIfValid(_tempDir));
    }

    #endregion

    #region Helpers

    private Dictionary<string, ExecutableModel> ExportThenImport(Dictionary<string, ExecutableModel> executables)
    {
        var model = new PackageModel { Executables = executables };

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        _importer.Start(_tempDir);
        _importer.RunStep();

        var resultModel = new PackageModel();
        _importer.SetResult(resultModel);

        return resultModel.Executables;
    }

    #endregion
}
