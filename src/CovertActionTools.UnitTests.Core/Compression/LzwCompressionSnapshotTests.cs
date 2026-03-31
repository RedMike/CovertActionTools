using System;
using CovertActionTools.Core.Compression;
using Microsoft.Extensions.Logging.Abstractions;

namespace CovertActionTools.UnitTests.Core.Compression;

public class LzwCompressionSnapshotTests
{
    private static byte[] Compress(byte[] pixels, int width, int height, int maxWordWidth = 11)
    {
        var compression = new LzwCompression(NullLogger<LzwCompression>.Instance);
        return compression.Compress(width, height, maxWordWidth, pixels).Data;
    }

    [Fact]
    public void CompressSnapshot_4x4_Uniform()
    {
        var pixels = LzwTestDataGenerator.GenerateUniformPixels(4, 4);
        var compressed = Compress(pixels, 4, 4);
        Assert.Equal(LzwSnapshotData.Compressed_4x4_Uniform, Convert.ToBase64String(compressed));
    }

    [Fact]
    public void CompressSnapshot_4x4_NonUniform()
    {
        var pixels = LzwTestDataGenerator.GenerateVariedPixels(4, 4);
        var compressed = Compress(pixels, 4, 4);
        Assert.Equal(LzwSnapshotData.Compressed_4x4_NonUniform, Convert.ToBase64String(compressed));
    }

    [Fact]
    public void CompressSnapshot_512x512_Uniform()
    {
        var pixels = LzwTestDataGenerator.GenerateUniformPixels(512, 512);
        var compressed = Compress(pixels, 512, 512);
        Assert.Equal(LzwSnapshotData.Compressed_512x512_Uniform, Convert.ToBase64String(compressed));
    }

    [Fact]
    public void CompressSnapshot_512x48_NonUniform()
    {
        var pixels = LzwTestDataGenerator.GenerateVariedPixels(512, 48);
        var compressed = Compress(pixels, 512, 48);
        Assert.Equal(LzwSnapshotData.Compressed_512x48_NonUniform, Convert.ToBase64String(compressed));
    }

    [Fact]
    public void CompressSnapshot_513x32()
    {
        var pixels = LzwTestDataGenerator.GenerateVariedPixels(513, 32);
        var compressed = Compress(pixels, 513, 32);
        Assert.Equal(LzwSnapshotData.Compressed_513x32, Convert.ToBase64String(compressed));
    }

    [Fact]
    public void CompressSnapshot_513x33()
    {
        var pixels = LzwTestDataGenerator.GenerateVariedPixels(513, 33);
        var compressed = Compress(pixels, 513, 33);
        Assert.Equal(LzwSnapshotData.Compressed_513x33, Convert.ToBase64String(compressed));
    }

    [Fact]
    public void CompressSnapshot_512x32_MaxWordWidth10()
    {
        var pixels = LzwTestDataGenerator.GenerateVariedPixels(512, 32);
        var compressed = Compress(pixels, 512, 32, maxWordWidth: 10);
        Assert.Equal(LzwSnapshotData.Compressed_512x32_MaxWordWidth10, Convert.ToBase64String(compressed));
    }
}
