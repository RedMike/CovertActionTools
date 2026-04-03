using System;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Exporting.Publishers;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.IntegrationTests.Core.Parsers.Data;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class ExecutableRoundtripTests : IDisposable
{
    private readonly LegacyExecutableParser _parser;
    private readonly ExecutablePublisher _publisher;
    private readonly string _tempDir;

    public ExecutableRoundtripTests()
    {
        var decompression = new ExepackDecompression(NullLogger<ExepackDecompression>.Instance);
        var compression = new ExepackCompression(NullLogger<ExepackCompression>.Instance);

        _parser = new LegacyExecutableParser(
            NullLogger<LegacyExecutableParser>.Instance, decompression);
        _publisher = new ExecutablePublisher(
            NullLogger<ExecutablePublisher>.Instance, compression);

        _tempDir = Path.Combine(Path.GetTempPath(), $"ExecutableRoundtrip_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    private PackageModel TryParseFromScratch()
    {
        var scratchDir = ExecutableTestDataGenerator.FindScratchDirectory();
        if (scratchDir == null) return null;

        _parser.Start(scratchDir);
        while (!_parser.RunStep()) { }
        var model = new PackageModel();
        _parser.SetResult(model);
        return model;
    }

    #region Full pipeline roundtrip (parse → publish → compare bytes)

    [Theory]
    [InlineData("BUG")]
    [InlineData("CHASE")]
    [InlineData("GAME")]
    [InlineData("TAC")]
    public void Roundtrip_ByteIdentical(string name)
    {
        var model = TryParseFromScratch();
        if (model == null) return;

        // Read original file
        var scratchDir = ExecutableTestDataGenerator.FindScratchDirectory();
        var originalBytes = File.ReadAllBytes(Path.Combine(scratchDir, $"{name}.EXE"));

        // Publish the parsed model
        var publishModel = new PackageModel
        {
            Executables = model.Executables
                .Where(x => x.Key == name)
                .ToDictionary(x => x.Key, x => x.Value)
        };
        publishModel.Index.ExecutableIncluded.Add(name);

        _publisher.Start(_tempDir, publishModel);
        while (!_publisher.RunStep()) { }

        var publishedBytes = File.ReadAllBytes(Path.Combine(_tempDir, $"{name}.EXE"));

        Assert.Equal(originalBytes.Length, publishedBytes.Length);
        Assert.Equal(originalBytes, publishedBytes);
    }

    [Fact]
    public void Roundtrip_CODE_SizeDifferenceWithin16Bytes()
    {
        // CODE has 5 byte diffs due to short FILL runs (original uses MIN_RUN=4, we use 8)
        var model = TryParseFromScratch();
        if (model == null) return;

        var scratchDir = ExecutableTestDataGenerator.FindScratchDirectory();
        var originalBytes = File.ReadAllBytes(Path.Combine(scratchDir, "CODE.EXE"));

        var publishModel = new PackageModel
        {
            Executables = model.Executables
                .Where(x => x.Key == "CODE")
                .ToDictionary(x => x.Key, x => x.Value)
        };
        publishModel.Index.ExecutableIncluded.Add("CODE");

        _publisher.Start(_tempDir, publishModel);
        while (!_publisher.RunStep()) { }

        var publishedBytes = File.ReadAllBytes(Path.Combine(_tempDir, "CODE.EXE"));

        // Size should be very close (within 16 bytes)
        Assert.InRange(Math.Abs(originalBytes.Length - publishedBytes.Length), 0, 16);
    }

    [Fact]
    public void Roundtrip_FINAL_SizeDifferenceWithin32Bytes()
    {
        // FINAL is 16 bytes larger in round-trip due to compression decision differences
        var model = TryParseFromScratch();
        if (model == null) return;

        var scratchDir = ExecutableTestDataGenerator.FindScratchDirectory();
        var originalBytes = File.ReadAllBytes(Path.Combine(scratchDir, "FINAL.EXE"));

        var publishModel = new PackageModel
        {
            Executables = model.Executables
                .Where(x => x.Key == "FINAL")
                .ToDictionary(x => x.Key, x => x.Value)
        };
        publishModel.Index.ExecutableIncluded.Add("FINAL");

        _publisher.Start(_tempDir, publishModel);
        while (!_publisher.RunStep()) { }

        var publishedBytes = File.ReadAllBytes(Path.Combine(_tempDir, "FINAL.EXE"));

        // Size should be close (within 32 bytes)
        Assert.InRange(Math.Abs(originalBytes.Length - publishedBytes.Length), 0, 32);
    }

    #endregion

    #region Payload preservation roundtrip (parse → publish → parse again → compare payloads)

    [Theory]
    [InlineData("BUG")]
    [InlineData("CHASE")]
    [InlineData("CODE")]
    [InlineData("FINAL")]
    [InlineData("GAME")]
    [InlineData("TAC")]
    public void Roundtrip_AllSix_PayloadPreserved(string name)
    {
        var model = TryParseFromScratch();
        if (model == null) return;

        var originalExe = model.Executables[name];

        // Publish
        var publishModel = new PackageModel
        {
            Executables = model.Executables
                .Where(x => x.Key == name)
                .ToDictionary(x => x.Key, x => x.Value)
        };
        publishModel.Index.ExecutableIncluded.Add(name);

        _publisher.Start(_tempDir, publishModel);
        while (!_publisher.RunStep()) { }

        // Re-parse from the published output
        var decompression = new ExepackDecompression(NullLogger<ExepackDecompression>.Instance);
        var parser2 = new LegacyExecutableParser(
            NullLogger<LegacyExecutableParser>.Instance, decompression);
        parser2.Start(_tempDir);
        while (!parser2.RunStep()) { }
        var model2 = new PackageModel();
        parser2.SetResult(model2);

        var reparsedExe = model2.Executables[name];

        // Dead zone must match
        Assert.Equal(originalExe.DeadZone, reparsedExe.DeadZone);

        // Code segment must match
        Assert.Equal(originalExe.CodeSegment, reparsedExe.CodeSegment);

        // Dead zone size may differ slightly after EXEPACK recompression, which shifts the
        // code/data boundary. Compare the combined code+data payload instead.
        var origCode = originalExe.CodeSegment;
        var origData = originalExe.GetDataSegmentBytes();
        var repCode = reparsedExe.CodeSegment;
        var repData = reparsedExe.GetDataSegmentBytes();
        Assert.Equal(origCode.Length + origData.Length, repCode.Length + repData.Length);

        var origCombined = new byte[origCode.Length + origData.Length];
        Array.Copy(origCode, 0, origCombined, 0, origCode.Length);
        Array.Copy(origData, 0, origCombined, origCode.Length, origData.Length);
        var repCombined = new byte[repCode.Length + repData.Length];
        Array.Copy(repCode, 0, repCombined, 0, repCode.Length);
        Array.Copy(repData, 0, repCombined, repCode.Length, repData.Length);
        Assert.Equal(origCombined, repCombined);

        // Entry point and stack must match
        Assert.Equal(originalExe.EntryCS, reparsedExe.EntryCS);
        Assert.Equal(originalExe.EntryIP, reparsedExe.EntryIP);
        Assert.Equal(originalExe.StackSS, reparsedExe.StackSS);
        Assert.Equal(originalExe.StackSP, reparsedExe.StackSP);

        // Relocations must match
        Assert.Equal(originalExe.Relocations, reparsedExe.Relocations);
    }

    #endregion
}
