using System;
using System.IO;
using System.Linq;
using System.Text;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using CovertActionTools.Core.Models.Executables;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

/// <summary>
/// Tests that pointer values are correctly recomputed when array elements are added.
/// </summary>
public class ExecutablePointerRecomputationTests
{
    private readonly string _scratchDir;

    public ExecutablePointerRecomputationTests()
    {
        _scratchDir = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "scratch");
    }

    [Fact]
    public void TAC_AddEquipmentName_PointersShiftCorrectly()
    {
        if (!File.Exists(Path.Combine(_scratchDir, "TAC.EXE")))
        {
            return; // Skip if scratch EXEs not available (CI)
        }

        // Parse
        var decompression = new ExepackDecompression(NullLogger<ExepackDecompression>.Instance);
        var parser = new LegacyExecutableParser(NullLogger<LegacyExecutableParser>.Instance, decompression);
        parser.Start(_scratchDir);
        while (!parser.RunStep()) { }
        var model = new PackageModel();
        parser.SetResult(model);

        var tac = model.Executables["TAC"];
        var originalNames = tac.TacData.EquipmentNames.ToArray();
        var originalDataSegment = tac.GetDataSegmentBytes();

        // Add a new equipment name
        var newNames = tac.TacData.EquipmentNames.ToList();
        newNames.Add("Test Weapon");
        tac.TacData.EquipmentNames = newNames.ToArray();

        // Serialize
        var newDataSegment = tac.GetDataSegmentBytes();

        // The new data segment should be longer (new string + null + 2 bytes for new pointer)
        Assert.True(newDataSegment.Length > originalDataSegment.Length);

        // Verify the pointer table in the serialized output resolves correctly
        // The pointer table follows MidSectionPostEquipNames in the serialization order
        // Find the pointer table by searching for the known first equipment name
        var firstNameBytes = Encoding.ASCII.GetBytes(originalNames[0]);
        var firstNamePos = FindSequence(newDataSegment, firstNameBytes);
        Assert.True(firstNamePos >= 0, "First equipment name not found in serialized data");

        // The pointer table should contain a value pointing to firstNamePos
        // Scan the data segment for a uint16 LE value equal to firstNamePos
        var found = false;
        for (var i = 0; i < newDataSegment.Length - 1; i++)
        {
            var val = (ushort)(newDataSegment[i] | (newDataSegment[i + 1] << 8));
            if (val == firstNamePos)
            {
                // Found the first pointer — verify subsequent pointers point to valid strings
                var ptrCount = newNames.Count;
                for (var p = 0; p < ptrCount; p++)
                {
                    var ptr = (ushort)(newDataSegment[i + p * 2] | (newDataSegment[i + p * 2 + 1] << 8));
                    // Each pointer should point to a null-terminated string matching our name
                    var end = (int)ptr;
                    while (end < newDataSegment.Length && newDataSegment[end] != 0) end++;
                    var name = Encoding.ASCII.GetString(newDataSegment, ptr, end - ptr);
                    Assert.Equal(newNames[p], name);
                }
                found = true;
                break;
            }
        }
        Assert.True(found, "Pointer table not found in serialized data");
    }

    [Fact]
    public void CODE_AddGraphicsDoc_PointersShiftCorrectly()
    {
        if (!File.Exists(Path.Combine(_scratchDir, "CODE.EXE")))
        {
            return; // Skip if scratch EXEs not available (CI)
        }

        // Parse
        var decompression = new ExepackDecompression(NullLogger<ExepackDecompression>.Instance);
        var parser = new LegacyExecutableParser(NullLogger<LegacyExecutableParser>.Instance, decompression);
        parser.Start(_scratchDir);
        while (!parser.RunStep()) { }
        var model = new PackageModel();
        parser.SetResult(model);

        var code = model.Executables["CODE"];
        var originalDocs = code.CodeData.GraphicsLibraryDocs.ToArray();
        var originalDataSegment = code.GetDataSegmentBytes();

        // Add a new doc string
        var newDocs = code.CodeData.GraphicsLibraryDocs.ToList();
        newDocs.Add("This is a test documentation string.");
        code.CodeData.GraphicsLibraryDocs = newDocs.ToArray();

        // Serialize
        var newDataSegment = code.GetDataSegmentBytes();

        // The new data segment should be longer
        Assert.True(newDataSegment.Length > originalDataSegment.Length);

        // Verify the pointer table has grown and points to valid strings
        // The docs start at a known offset (PreDocData.Length)
        var docsStart = code.CodeData.PreDocData.Length;
        var docsBytes = DataSegmentHelper.NullTerminatedStringsToBytes(newDocs.ToArray());

        // The pointer table follows: PreDocData + docs + Unknown1 + NibbleSpriteData
        var ptrTableOffset = docsStart + docsBytes.Length
            + code.CodeData.Unknown1.Length + code.CodeData.NibbleSpriteData.Length;

        // Read pointer values from the serialized data
        for (var i = 0; i < newDocs.Count; i++)
        {
            var ptr = (ushort)(newDataSegment[ptrTableOffset + i * 2] | (newDataSegment[ptrTableOffset + i * 2 + 1] << 8));
            // Resolve the pointer to a string
            var end = (int)ptr;
            while (end < newDataSegment.Length && newDataSegment[end] != 0) end++;
            var resolved = Encoding.ASCII.GetString(newDataSegment, ptr, end - ptr);
            Assert.Equal(newDocs[i], resolved);
        }
    }

    private static int FindSequence(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j]) { match = false; break; }
            }
            if (match) return i;
        }
        return -1;
    }

    [Fact]
    public void FINAL_DataSegmentSize_Preserved()
    {
        if (!File.Exists(Path.Combine(_scratchDir, "FINAL.EXE"))) return;

        var decompression = new ExepackDecompression(NullLogger<ExepackDecompression>.Instance);
        var parser = new LegacyExecutableParser(NullLogger<LegacyExecutableParser>.Instance, decompression);
        parser.Start(_scratchDir);
        while (!parser.RunStep()) { }
        var model = new PackageModel();
        parser.SetResult(model);

        var final = model.Executables["FINAL"];

        var dsBytes = final.GetDataSegmentBytes();

        // Original FINAL data segment is 31200 bytes (from Python analysis)
        Assert.Equal(31200, dsBytes.Length);
    }
}
