using System;
using System.Linq;
using CovertActionTools.Core.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Compression.Data;

namespace CovertActionTools.UnitTests.Core.Compression;

public class ExepackRoundtripTests
{
    private readonly ExepackCompression _compression =
        new ExepackCompression(NullLogger<ExepackCompression>.Instance);
    private readonly ExepackDecompression _decompression =
        new ExepackDecompression(NullLogger<ExepackDecompression>.Instance);

    private byte[] CompressThenDecompress(byte[] payload)
    {
        var compressed = _compression.Compress(payload);

        // Destination length in paragraphs (rounded up)
        var destParagraphs = (payload.Length + 15) / 16;
        var result = _decompression.Decompress(compressed, destParagraphs);

        // The decompressed output is destParagraphs * 16 bytes, which may be larger
        // than the original payload due to paragraph rounding. Trim to original size.
        var trimmed = new byte[payload.Length];
        Array.Copy(result.Data, result.DeadZoneBoundary, trimmed, 0,
            Math.Min(payload.Length, result.Data.Length - result.DeadZoneBoundary));
        return trimmed;
    }

    #region Basic roundtrips

    [Fact]
    public void Roundtrip_UniformData_PreservesPayload()
    {
        var payload = ExepackTestDataGenerator.GenerateUniformPayload(256, 0xAA);
        var result = CompressThenDecompress(payload);
        Assert.Equal(payload, result);
    }

    [Fact]
    public void Roundtrip_VariedData_PreservesPayload()
    {
        var payload = ExepackTestDataGenerator.GenerateVariedPayload(256);
        var result = CompressThenDecompress(payload);
        Assert.Equal(payload, result);
    }

    [Fact]
    public void Roundtrip_MixedData_PreservesPayload()
    {
        var payload = ExepackTestDataGenerator.GenerateMixedPayload();
        var result = CompressThenDecompress(payload);
        Assert.Equal(payload, result);
    }

    #endregion

    #region Various sizes

    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(256)]
    [InlineData(1024)]
    [InlineData(4096)]
    [InlineData(65536)]
    public void Roundtrip_UniformPayload_VariousSizes(int size)
    {
        var payload = ExepackTestDataGenerator.GenerateUniformPayload(size, 0x42);
        var result = CompressThenDecompress(payload);
        Assert.Equal(payload, result);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(256)]
    [InlineData(1024)]
    [InlineData(4096)]
    public void Roundtrip_VariedPayload_VariousSizes(int size)
    {
        var payload = ExepackTestDataGenerator.GenerateVariedPayload(size);
        var result = CompressThenDecompress(payload);
        Assert.Equal(payload, result);
    }

    #endregion

    #region Paragraph-aligned sizes

    [Fact]
    public void Roundtrip_ExactParagraphSize_PreservesPayload()
    {
        // 48 bytes = exactly 3 paragraphs, no rounding needed
        var payload = ExepackTestDataGenerator.GenerateVariedPayload(48);
        var result = CompressThenDecompress(payload);
        Assert.Equal(payload, result);
    }

    [Fact]
    public void Roundtrip_NonParagraphAligned_PreservesPayload()
    {
        // 50 bytes = 3 paragraphs + 2 extra bytes
        var payload = ExepackTestDataGenerator.GenerateVariedPayload(50);
        var result = CompressThenDecompress(payload);
        Assert.Equal(payload, result);
    }

    #endregion

    #region Edge case: all zeros

    [Fact]
    public void Roundtrip_AllZeros_PreservesPayload()
    {
        var payload = new byte[512];
        var result = CompressThenDecompress(payload);
        Assert.Equal(payload, result);
    }

    #endregion
}
