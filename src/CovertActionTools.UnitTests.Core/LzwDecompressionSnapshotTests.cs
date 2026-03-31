using System;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.UnitTests.Core.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace CovertActionTools.UnitTests.Core;

public class LzwDecompressionSnapshotTests
{
    private static byte[] Decompress(string compressedBase64, int width, int height, int maxWordWidth = 11)
    {
        var compressed = Convert.FromBase64String(compressedBase64);
        using var ms = new MemoryStream(compressed);
        using var reader = new BinaryReader(ms);
        var decompression = new LzwDecompression(NullLogger<LzwDecompression>.Instance);
        return decompression.Decompress(width, height, maxWordWidth, reader).Data;
    }

    [Fact]
    public void DecompressSnapshot_4x4_Uniform()
    {
        var result = Decompress(SnapshotData.Compressed_4x4_Uniform, 4, 4);
        var expected = TestDataGenerator.GenerateUniformPixels(4, 4);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DecompressSnapshot_4x4_NonUniform()
    {
        var result = Decompress(SnapshotData.Compressed_4x4_NonUniform, 4, 4);
        var expected = TestDataGenerator.GenerateVariedPixels(4, 4);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DecompressSnapshot_512x512_Uniform()
    {
        var result = Decompress(SnapshotData.Compressed_512x512_Uniform, 512, 512);
        var expected = TestDataGenerator.GenerateUniformPixels(512, 512);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DecompressSnapshot_512x48_NonUniform()
    {
        var result = Decompress(SnapshotData.Compressed_512x48_NonUniform, 512, 48);
        var expected = TestDataGenerator.GenerateVariedPixels(512, 48);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DecompressSnapshot_513x32()
    {
        var result = Decompress(SnapshotData.Compressed_513x32, 513, 32);
        var expected = TestDataGenerator.GenerateVariedPixels(513, 32);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DecompressSnapshot_513x33()
    {
        var result = Decompress(SnapshotData.Compressed_513x33, 513, 33);
        var expected = TestDataGenerator.GenerateVariedPixels(513, 33);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DecompressSnapshot_512x32_MaxWordWidth10()
    {
        var result = Decompress(SnapshotData.Compressed_512x32_MaxWordWidth10, 512, 32, maxWordWidth: 10);
        var expected = TestDataGenerator.GenerateVariedPixels(512, 32);
        Assert.Equal(expected, result);
    }
}
