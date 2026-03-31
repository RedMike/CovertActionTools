using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class SharedImageSnapshotTests
{
    private readonly SharedImageExporter _exporter;
    private readonly SharedImageParser _parser;

    public SharedImageSnapshotTests()
    {
        var compression = new LzwCompression(NullLogger<LzwCompression>.Instance);
        var decompression = new LzwDecompression(NullLogger<LzwDecompression>.Instance);
        _exporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, compression);
        _parser = new SharedImageParser(NullLogger<SharedImageParser>.Instance, decompression);
    }

    // --- Export snapshot tests (binary format stability) ---

    [Fact]
    public void ExportSnapshot_4x4_Uniform_Format0x07()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        var exported = ExportToBase64(pixels, 4, 4);
        Assert.Equal(SharedImageSnapshotData.Exported_4x4_Uniform_Format0x07, exported);
    }

    [Fact]
    public void ExportSnapshot_4x4_Varied_Format0x07()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4);
        var exported = ExportToBase64(pixels, 4, 4);
        Assert.Equal(SharedImageSnapshotData.Exported_4x4_Varied_Format0x07, exported);
    }

    [Fact]
    public void ExportSnapshot_4x4_Uniform_Format0x0F_Identity()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        var mappings = SharedImageTestDataGenerator.CreateIdentityCgaColorMappings();
        var exported = ExportToBase64(pixels, 4, 4, colorMappings: mappings);
        Assert.Equal(SharedImageSnapshotData.Exported_4x4_Uniform_Format0x0F_Identity, exported);
    }

    [Fact]
    public void ExportSnapshot_4x4_Varied_Format0x0F_Offset3()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4);
        var mappings = SharedImageTestDataGenerator.CreateOffsetCgaColorMappings(3);
        var exported = ExportToBase64(pixels, 4, 4, colorMappings: mappings);
        Assert.Equal(SharedImageSnapshotData.Exported_4x4_Varied_Format0x0F_Offset3, exported);
    }

    [Fact]
    public void ExportSnapshot_512x2_Varied_Format0x07()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(512, 2);
        var exported = ExportToBase64(pixels, 512, 2);
        Assert.Equal(SharedImageSnapshotData.Exported_512x2_Varied_Format0x07, exported);
    }

    // --- Parse snapshot tests (parsed model properties from known binary) ---

    [Fact]
    public void ParseSnapshot_512x2_Varied_Format0x07_HasCorrectPixels()
    {
        var model = ParseFromSnapshot(SharedImageSnapshotData.Exported_512x2_Varied_Format0x07);
        Assert.Equal(512, model.Data.Width);
        Assert.Equal(2, model.Data.Height);
        var expected = SharedImageTestDataGenerator.GenerateVariedPixels(512, 2);
        Assert.Equal(expected, model.RawVgaImageData);
    }

    [Fact]
    public void ParseSnapshot_4x4_Uniform_Format0x07_HasCorrectMetadata()
    {
        var model = ParseFromSnapshot(SharedImageSnapshotData.Exported_4x4_Uniform_Format0x07);
        Assert.Equal(4, model.Data.Width);
        Assert.Equal(4, model.Data.Height);
        Assert.Equal(11, model.Data.CompressionDictionaryWidth);
        Assert.Null(model.Data.LegacyColorMappings);
    }

    [Fact]
    public void ParseSnapshot_4x4_Uniform_Format0x07_HasCorrectPixels()
    {
        var model = ParseFromSnapshot(SharedImageSnapshotData.Exported_4x4_Uniform_Format0x07);
        var expected = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        Assert.Equal(expected, model.RawVgaImageData);
    }

    [Fact]
    public void ParseSnapshot_4x4_Varied_Format0x07_HasCorrectPixels()
    {
        var model = ParseFromSnapshot(SharedImageSnapshotData.Exported_4x4_Varied_Format0x07);
        var expected = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4);
        Assert.Equal(expected, model.RawVgaImageData);
    }

    [Fact]
    public void ParseSnapshot_4x4_Uniform_Format0x0F_HasColorMappings()
    {
        var model = ParseFromSnapshot(SharedImageSnapshotData.Exported_4x4_Uniform_Format0x0F_Identity);
        Assert.NotNull(model.Data.LegacyColorMappings);
        Assert.Equal(16, model.Data.LegacyColorMappings.Count);
        for (byte i = 0; i < 16; i++)
        {
            var cga = (byte)(i % 4);
            var expected = (byte)((cga << 4) | cga);
            Assert.Equal(expected, model.Data.LegacyColorMappings[i]);
        }
    }

    [Fact]
    public void ParseSnapshot_4x4_Varied_Format0x0F_Offset3_HasCorrectMappings()
    {
        var model = ParseFromSnapshot(SharedImageSnapshotData.Exported_4x4_Varied_Format0x0F_Offset3);
        Assert.NotNull(model.Data.LegacyColorMappings);
        for (byte i = 0; i < 16; i++)
        {
            var cga = (byte)((i + 3) % 4);
            var expected = (byte)((cga << 4) | cga);
            Assert.Equal(expected, model.Data.LegacyColorMappings[i]);
        }
    }

    private string ExportToBase64(byte[] pixels, int width, int height,
        int dictionaryWidth = 11, Dictionary<byte, byte>? colorMappings = null)
    {
        var model = new SharedImageModel
        {
            RawVgaImageData = pixels,
            Data = new SharedImageModel.ImageData
            {
                Width = width,
                Height = height,
                CompressionDictionaryWidth = (byte)dictionaryWidth,
                LegacyColorMappings = colorMappings
            }
        };

        return Convert.ToBase64String(_exporter.GetLegacyFileData(model));
    }

    private SharedImageModel ParseFromSnapshot(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms);
        return _parser.Parse("test", reader);
    }
}
