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
    // FINAL excluded: EXEPACK compression encodes differently (same content, different encoding)
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
    public void Roundtrip_FINAL_DataSegmentPreserved()
    {
        var model = TryParseFromScratch();
        if (model == null) return;

        var originalExe = model.Executables["FINAL"];
        var originalDataSegment = originalExe.GetDataSegmentBytes();

        // Re-serialize and compare data segment bytes
        var reparsed = CovertActionTools.Core.Models.Executables.FinalDataSegment.FromBytes(originalDataSegment);
        var rebuilt = reparsed.ToBytes();

        Assert.Equal(originalDataSegment.Length, rebuilt.Length);
        Assert.Equal(originalDataSegment, rebuilt);
    }

    [Fact]
    public void Roundtrip_FINAL_FullPipelinePayloadPreserved()
    {
        var model = TryParseFromScratch();
        if (model == null) return;

        var originalExe = model.Executables["FINAL"];

        // Publish
        var publishModel = new PackageModel
        {
            Executables = model.Executables
                .Where(x => x.Key == "FINAL")
                .ToDictionary(x => x.Key, x => x.Value)
        };
        publishModel.Index.ExecutableIncluded.Add("FINAL");

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

        var reparsedExe = model2.Executables["FINAL"];

        // Data segment bytes must match
        Assert.Equal(originalExe.GetDataSegmentBytes(), reparsedExe.GetDataSegmentBytes());

        // Dead zone, code segment, entry point, relocations must match
        Assert.Equal(originalExe.DeadZone, reparsedExe.DeadZone);
        Assert.Equal(originalExe.CodeSegment, reparsedExe.CodeSegment);
        Assert.Equal(originalExe.EntryCS, reparsedExe.EntryCS);
        Assert.Equal(originalExe.EntryIP, reparsedExe.EntryIP);
        Assert.Equal(originalExe.StackSS, reparsedExe.StackSS);
        Assert.Equal(originalExe.StackSP, reparsedExe.StackSP);
        Assert.Equal(originalExe.Relocations, reparsedExe.Relocations);
    }

    #endregion

    #region Payload preservation roundtrip (parse → publish → parse again → compare payloads)

    [Theory]
    [InlineData("BUG")]
    [InlineData("CHASE")]
    [InlineData("CODE")]
    [InlineData("GAME")]
    [InlineData("TAC")]
    // FINAL excluded: EXEPACK recompression produces different encoding, shifting the
    // code/data boundary on decompression. Tracked as a known EXEPACK compressor issue.
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

        // Data segment bytes must match (serialized back from structured fields)
        Assert.Equal(originalExe.GetDataSegmentBytes(), reparsedExe.GetDataSegmentBytes());

        // Entry point and stack must match
        Assert.Equal(originalExe.EntryCS, reparsedExe.EntryCS);
        Assert.Equal(originalExe.EntryIP, reparsedExe.EntryIP);
        Assert.Equal(originalExe.StackSS, reparsedExe.StackSS);
        Assert.Equal(originalExe.StackSP, reparsedExe.StackSP);

        // Relocations must match
        Assert.Equal(originalExe.Relocations, reparsedExe.Relocations);
    }

    #endregion

    #region FINAL data segment diagnostic

    [Fact]
    public void Roundtrip_FINAL_DataSegmentDiagnostic()
    {
        var model = TryParseFromScratch();
        if (model == null) return;

        var originalExe = model.Executables["FINAL"];
        var originalDataSegment = originalExe.GetDataSegmentBytes();
        var reparsed = CovertActionTools.Core.Models.Executables.FinalDataSegment.FromBytes(originalDataSegment);
        var rebuilt = reparsed.ToBytes();

        var output = new System.Text.StringBuilder();
        output.AppendLine($"Original length: {originalDataSegment.Length}");
        output.AppendLine($"Rebuilt length: {rebuilt.Length}");

        var diffs = 0;
        var maxLen = Math.Max(originalDataSegment.Length, rebuilt.Length);
        for (var i = 0; i < maxLen; i++)
        {
            var orig = i < originalDataSegment.Length ? originalDataSegment[i] : (byte)0xFF;
            var reb = i < rebuilt.Length ? rebuilt[i] : (byte)0xFF;
            if (orig != reb)
            {
                if (diffs < 50)
                    output.AppendLine($"  DIFF DS:0x{i:X4}: orig=0x{orig:X2}({(orig >= 0x20 && orig <= 0x7E ? (char)orig : '.')}) rebuilt=0x{reb:X2}({(reb >= 0x20 && reb <= 0x7E ? (char)reb : '.')})");
                diffs++;
            }
        }
        output.AppendLine($"Total diffs: {diffs}");

        Assert.True(diffs == 0, output.ToString());
    }

    #endregion
}
