using System;
using System.IO;
using CovertActionTools.Core.Compression;

namespace CovertActionTools.UnitTests.Core.Parsers.Stubs;

internal class StubLzwDecompression : ILzwDecompression
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
