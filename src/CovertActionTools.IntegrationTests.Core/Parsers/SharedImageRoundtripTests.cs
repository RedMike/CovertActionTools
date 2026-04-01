using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.IntegrationTests.Core.Parsers.Data;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class SharedImageRoundtripTests
{
    private const int DefaultDictionaryWidth = 11;

    private readonly SharedImageExporter _exporter;
    private readonly SharedImageParser _parser;

    public SharedImageRoundtripTests()
    {
        var compression = new LzwCompression(NullLogger<LzwCompression>.Instance);
        var decompression = new LzwDecompression(NullLogger<LzwDecompression>.Instance);
        _exporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, compression);
        _parser = new SharedImageParser(NullLogger<SharedImageParser>.Instance, decompression);
    }

    #region Format 0x07 roundtrips

    [Fact]
    public void Roundtrip_Format0x07_4x4_Uniform()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        var model = ExportThenParse(pixels, 4, 4);

        Assert.Equal(pixels, model.RawVgaImageData);
        Assert.Equal(4, model.Data.Width);
        Assert.Equal(4, model.Data.Height);
        Assert.Null(model.Data.LegacyColorMappings);
    }

    [Fact]
    public void Roundtrip_Format0x07_4x4_Varied()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4);
        var model = ExportThenParse(pixels, 4, 4);

        Assert.Equal(pixels, model.RawVgaImageData);
    }

    [Fact]
    public void Roundtrip_Format0x07_320x200_Varied()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(320, 200);
        var model = ExportThenParse(pixels, 320, 200);

        Assert.Equal(pixels, model.RawVgaImageData);
        Assert.Equal(320, model.Data.Width);
        Assert.Equal(200, model.Data.Height);
    }

    [Fact]
    public void Roundtrip_Format0x07_512x2_WideImage()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(512, 2);
        var model = ExportThenParse(pixels, 512, 2);

        Assert.Equal(pixels, model.RawVgaImageData);
        Assert.Equal(512, model.Data.Width);
        Assert.Equal(2, model.Data.Height);
    }

    [Fact]
    public void Roundtrip_Format0x07_5x3_OddDimensions()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(5, 3);
        var model = ExportThenParse(pixels, 5, 3);

        Assert.Equal(pixels, model.RawVgaImageData);
    }

    [Fact]
    public void Roundtrip_Format0x07_PreservesDictionaryWidth()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(8, 4);
        var model = ExportThenParse(pixels, 8, 4, dictionaryWidth: 10);

        Assert.Equal(10, model.Data.CompressionDictionaryWidth);
        Assert.Equal(pixels, model.RawVgaImageData);
    }

    #endregion

    #region Format 0x0F roundtrips

    [Fact]
    public void Roundtrip_Format0x0F_4x4_IdentityMappings()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        var mappings = SharedImageTestDataGenerator.CreateIdentityCgaColorMappings();
        var model = ExportThenParse(pixels, 4, 4, colorMappings: mappings);

        Assert.Equal(pixels, model.RawVgaImageData);
        Assert.NotNull(model.Data.LegacyColorMappings);
        Assert.Equal(16, model.Data.LegacyColorMappings.Count);
    }

    [Fact]
    public void Roundtrip_Format0x0F_320x200_OffsetMappings()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(320, 200);
        var mappings = SharedImageTestDataGenerator.CreateOffsetCgaColorMappings(3);
        var model = ExportThenParse(pixels, 320, 200, colorMappings: mappings);

        Assert.Equal(pixels, model.RawVgaImageData);
        for (byte i = 0; i < 16; i++)
        {
            var cga = (byte)((i + 3) % 4);
            var expected = (byte)((cga << 4) | cga);
            Assert.Equal(expected, model.Data.LegacyColorMappings![i]);
        }
    }

    [Fact]
    public void Roundtrip_Format0x0F_ProducesCgaImageData()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4);
        var mappings = SharedImageTestDataGenerator.CreateIdentityCgaColorMappings();
        var model = ExportThenParse(pixels, 4, 4, colorMappings: mappings);

        Assert.NotEmpty(model.CgaImageData);
    }

    #endregion

    private SharedImageModel ExportThenParse(byte[] pixels, int width, int height,
        int dictionaryWidth = DefaultDictionaryWidth,
        System.Collections.Generic.Dictionary<byte, byte>? colorMappings = null)
    {
        var sourceModel = new SharedImageModel
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

        var exportedBytes = _exporter.GetLegacyFileData(sourceModel);

        using var ms = new MemoryStream(exportedBytes);
        using var reader = new BinaryReader(ms);
        return _parser.Parse("test", reader);
    }
}
