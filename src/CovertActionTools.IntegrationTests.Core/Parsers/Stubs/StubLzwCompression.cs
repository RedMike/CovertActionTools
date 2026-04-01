using System;
using CovertActionTools.Core.Compression;

namespace CovertActionTools.IntegrationTests.Core.Parsers.Stubs;

/// <summary>
/// Stub implementation of ILzwCompression that throws if called.
/// SharedImageExporter requires ILzwCompression in its constructor, but the exporter
/// methods used by SimpleImageExporter and FontsExporter (GetVgaImageData, GetImageData)
/// never invoke compression. This stub satisfies the dependency without real logic.
/// </summary>
internal class StubLzwCompression : ILzwCompression
{
    public CompressionResult Compress(int width, int height, int maxWordWidth, byte[] data, bool collectMetrics = false)
    {
        throw new NotImplementedException("StubLzwCompression.Compress should never be called by roundtrip tests.");
    }
}
