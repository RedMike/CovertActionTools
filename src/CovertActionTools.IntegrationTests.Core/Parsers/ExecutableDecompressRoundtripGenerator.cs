using System;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

using CovertActionTools.IntegrationTests.Core.Parsers.Data;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Generates decompressed and roundtrip EXE files in the scratch directory.
/// Not run as part of the normal test suite — invoke manually with:
///   dotnet test --filter "Category=Generator&FullyQualifiedName~ExecutableDecompressRoundtripGenerator"
/// </summary>
[Trait("Category", "Generator")]
public class ExecutableDecompressRoundtripGenerator
{
    private readonly ITestOutputHelper _output;
    private readonly LegacyExecutableParser _parser;
    private readonly ExepackCompression _compression;

    public ExecutableDecompressRoundtripGenerator(ITestOutputHelper output)
    {
        _output = output;
        var decompression = new ExepackDecompression(NullLogger<ExepackDecompression>.Instance);
        _parser = new LegacyExecutableParser(
            NullLogger<LegacyExecutableParser>.Instance, decompression);
        _compression = new ExepackCompression(NullLogger<ExepackCompression>.Instance);
    }

    [Fact]
    public void GenerateDecompressedAndRoundtripExes()
    {
        var scratchDir = ExecutableTestDataGenerator.FindScratchDirectory();
        if (scratchDir == null)
        {
            _output.WriteLine("SKIP: scratch directory not found");
            return;
        }

        _parser.Start(scratchDir);
        while (!_parser.RunStep()) { }
        var model = new PackageModel();
        _parser.SetResult(model);

        foreach (var name in ExecutableTestDataGenerator.KnownExecutables)
        {
            Assert.True(model.Executables.ContainsKey(name), $"Missing executable: {name}");
            var exe = model.Executables[name];

            // Build decompressed flat MZ EXE
            var decompressedBytes = BuildDecompressedExe(exe);
            var decompressedPath = Path.Combine(scratchDir, $"{name}.decompressed.EXE");
            File.WriteAllBytes(decompressedPath, decompressedBytes);
            _output.WriteLine($"Written: {decompressedPath} ({decompressedBytes.Length} bytes)");

            // Build roundtrip repacked EXE
            var roundtripBytes = BuildRoundtripExe(exe);
            var roundtripPath = Path.Combine(scratchDir, $"{name}.roundtrip.EXE");
            File.WriteAllBytes(roundtripPath, roundtripBytes);
            _output.WriteLine($"Written: {roundtripPath} ({roundtripBytes.Length} bytes)");
        }

        _output.WriteLine("Done — all decompressed and roundtrip EXEs written.");
    }

    #region Decompressed EXE builder

    private static byte[] BuildDecompressedExe(ExecutableModel exe)
    {
        // Reconstruct the flat payload: dead zone + code segment + data segment
        var dataSegmentBytes = exe.GetDataSegmentBytes();
        var payload = new byte[exe.DeadZone.Length + exe.CodeSegment.Length + dataSegmentBytes.Length];
        Array.Copy(exe.DeadZone, 0, payload, 0, exe.DeadZone.Length);
        Array.Copy(exe.CodeSegment, 0, payload, exe.DeadZone.Length, exe.CodeSegment.Length);
        Array.Copy(dataSegmentBytes, 0, payload, exe.DeadZone.Length + exe.CodeSegment.Length,
            dataSegmentBytes.Length);

        // Build standard MZ relocation table (4 bytes each: offset:2, segment:2)
        var relocCount = exe.Relocations.Length / 2;
        var relocTableSize = relocCount * 4;

        // Header layout: 28 bytes fixed + relocation table, padded to paragraph boundary
        var rawHeaderSize = 28 + relocTableSize;
        var headerParagraphs = (rawHeaderSize + 15) / 16;
        var headerSize = headerParagraphs * 16;

        var totalSize = headerSize + payload.Length;
        var pages = (totalSize + 511) / 512;
        var lastPageBytes = totalSize % 512;

        // Build header
        var header = new byte[headerSize];
        header[0] = (byte)'M';
        header[1] = (byte)'Z';
        WriteUInt16(header, 2, (ushort)lastPageBytes);
        WriteUInt16(header, 4, (ushort)pages);
        WriteUInt16(header, 6, (ushort)relocCount);
        WriteUInt16(header, 8, (ushort)headerParagraphs);
        WriteUInt16(header, 10, 0);      // min_extra
        WriteUInt16(header, 12, 0xFFFF); // max_extra
        WriteUInt16(header, 14, exe.StackSS);
        WriteUInt16(header, 16, exe.StackSP);
        WriteUInt16(header, 18, 0);      // checksum
        WriteUInt16(header, 20, exe.EntryIP);
        WriteUInt16(header, 22, exe.EntryCS);
        WriteUInt16(header, 24, 28);     // reloc table offset (immediately after fixed header)
        WriteUInt16(header, 26, 0);      // overlay number

        // Write relocation entries
        var relocOffset = 28;
        for (var i = 0; i < exe.Relocations.Length - 1; i += 2)
        {
            var seg = exe.Relocations[i];
            var off = exe.Relocations[i + 1];
            WriteUInt16(header, relocOffset, off);
            WriteUInt16(header, relocOffset + 2, seg);
            relocOffset += 4;
        }

        // Assemble final file
        var result = new byte[totalSize];
        Array.Copy(header, 0, result, 0, headerSize);
        Array.Copy(payload, 0, result, headerSize, payload.Length);
        return result;
    }

    #endregion

    #region Roundtrip EXE builder

    private byte[] BuildRoundtripExe(ExecutableModel exe)
    {
        var dataSegmentBytes = exe.GetDataSegmentBytes();
        var rawPayload = new byte[exe.CodeSegment.Length + dataSegmentBytes.Length];
        Array.Copy(exe.CodeSegment, 0, rawPayload, 0, exe.CodeSegment.Length);
        Array.Copy(dataSegmentBytes, 0, rawPayload, exe.CodeSegment.Length, dataSegmentBytes.Length);

        var compressedPayload = _compression.Compress(rawPayload);
        var fullPayloadLength = exe.DeadZone.Length + rawPayload.Length;

        return ExepackUtilities.BuildPackedExe(
            exe.DeadZone,
            compressedPayload,
            exe.ExepackStub,
            exe.OriginalMzHeader,
            exe.Relocations,
            exe.EntryCS,
            exe.EntryIP,
            exe.StackSS,
            exe.StackSP,
            fullPayloadLength);
    }

    #endregion

    #region Helpers

    private static void WriteUInt16(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
    }

    #endregion
}
