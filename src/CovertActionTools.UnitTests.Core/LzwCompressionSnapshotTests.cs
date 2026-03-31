using System;
using CovertActionTools.Core.Compression;
using CovertActionTools.UnitTests.Core.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace CovertActionTools.UnitTests.Core;

public class LzwCompressionSnapshotTests
{
    private static byte[] Compress(byte[] pixels, int width, int height, int maxWordWidth = 11)
    {
        var compressor = new LzwCompression(NullLogger.Instance, maxWordWidth, pixels);
        return compressor.Compress(width, height);
    }

    [Fact]
    public void CompressSnapshot_4x4_Uniform()
    {
        var pixels = TestDataGenerator.GenerateUniformPixels(4, 4);
        var compressed = Compress(pixels, 4, 4);
        Assert.Equal(SnapshotData.Compressed_4x4_Uniform, Convert.ToBase64String(compressed));
    }

    [Fact]
    public void CompressSnapshot_4x4_NonUniform()
    {
        var pixels = TestDataGenerator.GenerateVariedPixels(4, 4);
        var compressed = Compress(pixels, 4, 4);
        Assert.Equal(SnapshotData.Compressed_4x4_NonUniform, Convert.ToBase64String(compressed));
    }

    [Fact]
    public void CompressSnapshot_512x512_Uniform()
    {
        var pixels = TestDataGenerator.GenerateUniformPixels(512, 512);
        var compressed = Compress(pixels, 512, 512);
        Assert.Equal(SnapshotData.Compressed_512x512_Uniform, Convert.ToBase64String(compressed));
    }

    [Fact]
    public void CompressSnapshot_512x48_NonUniform()
    {
        var pixels = TestDataGenerator.GenerateVariedPixels(512, 48);
        var compressed = Compress(pixels, 512, 48);
        Assert.Equal(SnapshotData.Compressed_512x48_NonUniform, Convert.ToBase64String(compressed));
    }

    [Fact]
    public void CompressSnapshot_513x32()
    {
        var pixels = TestDataGenerator.GenerateVariedPixels(513, 32);
        var compressed = Compress(pixels, 513, 32);
        Assert.Equal(SnapshotData.Compressed_513x32, Convert.ToBase64String(compressed));
    }

    [Fact]
    public void CompressSnapshot_513x33()
    {
        var pixels = TestDataGenerator.GenerateVariedPixels(513, 33);
        var compressed = Compress(pixels, 513, 33);
        Assert.Equal(SnapshotData.Compressed_513x33, Convert.ToBase64String(compressed));
    }

    [Fact]
    public void CompressSnapshot_512x32_MaxWordWidth10()
    {
        var pixels = TestDataGenerator.GenerateVariedPixels(512, 32);
        var compressed = Compress(pixels, 512, 32, maxWordWidth: 10);
        Assert.Equal(SnapshotData.Compressed_512x32_MaxWordWidth10, Convert.ToBase64String(compressed));
    }
}
