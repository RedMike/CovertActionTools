using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.IntegrationTests.Core.Parsers.Data;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class SharedImageImporterRoundtripTests : IDisposable
{
    private readonly SharedImageImporter _importer;
    private readonly string _tempDir;

    public SharedImageImporterRoundtripTests()
    {
        _importer = new SharedImageImporter(NullLogger<SharedImageImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"SharedImageImporterRoundtrip_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Format 0x07 roundtrips (no CGA mappings)

    [Fact]
    public void Roundtrip_Format0x07_4x4_Uniform()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        var model = WriteAndImport(pixels, 4, 4);

        Assert.Equal(pixels, model.RawVgaImageData);
        Assert.Equal(4, model.Data.Width);
        Assert.Equal(4, model.Data.Height);
        Assert.Null(model.Data.LegacyColorMappings);
        Assert.Empty(model.CgaImageData);
    }

    [Fact]
    public void Roundtrip_Format0x07_4x4_Varied()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4);
        var model = WriteAndImport(pixels, 4, 4);

        Assert.Equal(pixels, model.RawVgaImageData);
    }

    [Fact]
    public void Roundtrip_Format0x07_320x200_Varied()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(320, 200);
        var model = WriteAndImport(pixels, 320, 200);

        Assert.Equal(pixels, model.RawVgaImageData);
        Assert.Equal(320, model.Data.Width);
        Assert.Equal(200, model.Data.Height);
    }

    [Fact]
    public void Roundtrip_Format0x07_5x3_OddDimensions()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(5, 3);
        var model = WriteAndImport(pixels, 5, 3);

        Assert.Equal(pixels, model.RawVgaImageData);
    }

    [Fact]
    public void Roundtrip_Format0x07_PreservesDictionaryWidth()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(8, 4);
        var model = WriteAndImport(pixels, 8, 4, dictionaryWidth: 10);

        Assert.Equal(10, model.Data.CompressionDictionaryWidth);
    }

    [Fact]
    public void Roundtrip_Format0x07_VgaTextureDataIsNonEmpty()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        var model = WriteAndImport(pixels, 4, 4);

        Assert.NotEmpty(model.VgaImageData);
        Assert.Equal(4 * 4 * 4, model.VgaImageData.Length);
    }

    #endregion

    #region Format 0x0F roundtrips (with CGA mappings)

    [Fact]
    public void Roundtrip_Format0x0F_4x4_IdentityMappings()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4, value: 1);
        var mappings = SharedImageTestDataGenerator.CreateIdentityCgaColorMappings();
        var model = WriteAndImport(pixels, 4, 4, colorMappings: mappings);

        Assert.Equal(pixels, model.RawVgaImageData);
        Assert.NotNull(model.Data.LegacyColorMappings);
        Assert.Equal(16, model.Data.LegacyColorMappings.Count);
        Assert.NotEmpty(model.CgaImageData);
    }

    [Fact]
    public void Roundtrip_Format0x0F_320x200_OffsetMappings()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(320, 200);
        var mappings = SharedImageTestDataGenerator.CreateOffsetCgaColorMappings(3);
        var model = WriteAndImport(pixels, 320, 200, colorMappings: mappings);

        Assert.Equal(pixels, model.RawVgaImageData);
        for (byte i = 0; i < 16; i++)
        {
            var cga = (byte)((i + 3) % 4);
            var expected = (byte)((cga << 4) | cga);
            Assert.Equal(expected, model.Data.LegacyColorMappings![i]);
        }
    }

    [Fact]
    public void Roundtrip_Format0x0F_CgaImageDataHasCorrectLength()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4, value: 1);
        var mappings = SharedImageTestDataGenerator.CreateIdentityCgaColorMappings();
        var model = WriteAndImport(pixels, 4, 4, colorMappings: mappings);

        Assert.Equal(4 * 4 * 4, model.CgaImageData.Length);
    }

    #endregion

    #region Helpers

    private SharedImageModel WriteAndImport(byte[] rawPixels, int width, int height,
        int dictionaryWidth = 11, Dictionary<byte, byte>? colorMappings = null)
    {
        var imageData = new SharedImageModel.ImageData
        {
            Width = width,
            Height = height,
            CompressionDictionaryWidth = (byte)dictionaryWidth,
            LegacyColorMappings = colorMappings
        };

        var json = JsonSerializer.Serialize(imageData, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_tempDir, "test_IMG.json"), json);

        var pngBytes = ImageConversion.VgaToTexture(width, height, rawPixels);
        File.WriteAllBytes(Path.Combine(_tempDir, "test_VGA.png"), pngBytes);

        return _importer.ReadImage(_tempDir, "test", "IMG");
    }

    #endregion
}
