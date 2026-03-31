using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class SharedImageParserTests
{
    private readonly SharedImageParser _parser;
    private readonly StubLzwDecompression _stubDecompression;

    public SharedImageParserTests()
    {
        _stubDecompression = new StubLzwDecompression();
        _parser = new SharedImageParser(NullLogger<SharedImageParser>.Instance, _stubDecompression);
    }

    #region Format 0x07 (no CGA mappings)

    [Fact]
    public void Parse_Format0x07_SetsWidthAndHeight()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(8, 4);
        _stubDecompression.SetResult(pixels);
        var data = SharedImageTestDataGenerator.BuildFormat0x07Header(8, 4, 11);

        var model = ParseFromBytes(data);

        Assert.Equal(8, model.Data.Width);
        Assert.Equal(4, model.Data.Height);
    }

    [Fact]
    public void Parse_Format0x07_SetsDictionaryWidth()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = SharedImageTestDataGenerator.BuildFormat0x07Header(4, 4, 10);

        var model = ParseFromBytes(data);

        Assert.Equal(10, model.Data.CompressionDictionaryWidth);
    }

    [Fact]
    public void Parse_Format0x07_HasNoCgaColorMappings()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = SharedImageTestDataGenerator.BuildFormat0x07Header(4, 4, 11);

        var model = ParseFromBytes(data);

        Assert.Null(model.Data.LegacyColorMappings);
    }

    [Fact]
    public void Parse_Format0x07_StoresRawPixelData()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = SharedImageTestDataGenerator.BuildFormat0x07Header(4, 4, 11);

        var model = ParseFromBytes(data);

        Assert.Equal(pixels, model.RawVgaImageData);
    }

    [Fact]
    public void Parse_Format0x07_ProducesEmptyCgaImageData()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = SharedImageTestDataGenerator.BuildFormat0x07Header(4, 4, 11);

        var model = ParseFromBytes(data);

        Assert.Empty(model.CgaImageData);
    }

    [Fact]
    public void Parse_Format0x07_ProducesNonEmptyVgaImageData()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = SharedImageTestDataGenerator.BuildFormat0x07Header(4, 4, 11);

        var model = ParseFromBytes(data);

        Assert.NotEmpty(model.VgaImageData);
    }

    #endregion

    #region Format 0x0F (with CGA mappings)

    [Fact]
    public void Parse_Format0x0F_HasCgaColorMappings()
    {
        var mappings = SharedImageTestDataGenerator.CreateIdentityCgaColorMappings();
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = SharedImageTestDataGenerator.BuildFormat0x0FHeader(4, 4, 11, mappings);

        var model = ParseFromBytes(data);

        Assert.NotNull(model.Data.LegacyColorMappings);
        Assert.Equal(16, model.Data.LegacyColorMappings.Count);
    }

    [Fact]
    public void Parse_Format0x0F_ReadsColorMappingsCorrectly()
    {
        var mappings = SharedImageTestDataGenerator.CreateOffsetCgaColorMappings(3);
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = SharedImageTestDataGenerator.BuildFormat0x0FHeader(4, 4, 11, mappings);

        var model = ParseFromBytes(data);

        for (byte i = 0; i < 16; i++)
        {
            var cga = (byte)((i + 3) % 4);
            var expected = (byte)((cga << 4) | cga);
            Assert.Equal(expected, model.Data.LegacyColorMappings![i]);
        }
    }

    [Fact]
    public void Parse_Format0x0F_ProducesNonEmptyCgaImageData()
    {
        var mappings = SharedImageTestDataGenerator.CreateIdentityCgaColorMappings();
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = SharedImageTestDataGenerator.BuildFormat0x0FHeader(4, 4, 11, mappings);

        var model = ParseFromBytes(data);

        Assert.NotEmpty(model.CgaImageData);
    }

    #endregion

    #region ImageType from key

    [Fact]
    public void Parse_KeyStartingWithEUROPE_SetsEuropeMapType()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = SharedImageTestDataGenerator.BuildFormat0x07Header(4, 4, 11);

        var model = ParseFromBytes(data, "EUROPE.PIC");

        Assert.Equal(SharedImageModel.ImageType.EuropeMap, model.Data.Type);
    }

    [Fact]
    public void Parse_UnknownKey_SetsUnknownType()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = SharedImageTestDataGenerator.BuildFormat0x07Header(4, 4, 11);

        var model = ParseFromBytes(data, "SOMETHING_ELSE");

        Assert.Equal(SharedImageModel.ImageType.Unknown, model.Data.Type);
    }

    #endregion

    #region Edge cases

    [Fact]
    public void Parse_MinimumDimensions_1x1()
    {
        var pixels = new byte[] { 5 };
        _stubDecompression.SetResult(pixels);
        var data = SharedImageTestDataGenerator.BuildFormat0x07Header(1, 1, 11);

        var model = ParseFromBytes(data);

        Assert.Equal(1, model.Data.Width);
        Assert.Equal(1, model.Data.Height);
        Assert.Single(model.RawVgaImageData);
    }

    [Fact]
    public void Parse_UnsupportedFormatFlag_Throws()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((ushort)0xFF);
        writer.Write((ushort)4);
        writer.Write((ushort)4);
        ms.Position = 0;

        using var reader = new BinaryReader(ms);
        Assert.Throws<Exception>(() => _parser.Parse("test", reader));
    }

    [Fact]
    public void Parse_PassesCorrectParametersToDecompression()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(16, 8);
        _stubDecompression.SetResult(pixels);
        var data = SharedImageTestDataGenerator.BuildFormat0x07Header(16, 8, 10);

        ParseFromBytes(data);

        Assert.Equal(16, _stubDecompression.LastWidth);
        Assert.Equal(8, _stubDecompression.LastHeight);
        Assert.Equal(10, _stubDecompression.LastMaxWordWidth);
    }

    #endregion

    private SharedImageModel ParseFromBytes(byte[] data, string key = "test")
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        return _parser.Parse(key, reader);
    }

    private class StubLzwDecompression : ILzwDecompression
    {
        private byte[] _result = Array.Empty<byte>();

        public int LastWidth { get; private set; }
        public int LastHeight { get; private set; }
        public int LastMaxWordWidth { get; private set; }

        public void SetResult(byte[] data)
        {
            _result = data;
        }

        public DecompressionResult Decompress(int width, int height, int maxWordWidth, BinaryReader reader,
            bool collectMetrics = false)
        {
            LastWidth = width;
            LastHeight = height;
            LastMaxWordWidth = maxWordWidth;
            return new DecompressionResult(_result, 0);
        }
    }
}
