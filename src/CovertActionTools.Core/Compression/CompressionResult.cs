namespace CovertActionTools.Core.Compression
{
    public class CompressionResult
    {
        public byte[] Data { get; }
        public int OriginalSize { get; }
        public int CompressedSize { get; }
        public double CompressionRatio => OriginalSize > 0 ? (double)CompressedSize / OriginalSize : 0;
        public CompressionStageMetrics? Stages { get; }

        public CompressionResult(byte[] data, int originalSize, CompressionStageMetrics? stages = null)
        {
            Data = data;
            OriginalSize = originalSize;
            CompressedSize = data.Length;
            Stages = stages;
        }
    }

    public class CompressionStageMetrics
    {
        public int RawPixels { get; }
        public int PackedBytes { get; }
        public int RleBytes { get; }
        public int LzwBytes { get; }

        public CompressionStageMetrics(int rawPixels, int packedBytes, int rleBytes, int lzwBytes)
        {
            RawPixels = rawPixels;
            PackedBytes = packedBytes;
            RleBytes = rleBytes;
            LzwBytes = lzwBytes;
        }
    }

    public class DecompressionResult
    {
        public byte[] Data { get; }
        public int CompressedSize { get; }
        public int DecompressedSize { get; }
        public DecompressionStageMetrics? Stages { get; }

        public DecompressionResult(byte[] data, int compressedSize, DecompressionStageMetrics? stages = null)
        {
            Data = data;
            CompressedSize = compressedSize;
            DecompressedSize = data.Length;
            Stages = stages;
        }
    }

    public class DecompressionStageMetrics
    {
        public int LzwBytes { get; }
        public int RleBytes { get; }
        public int PackedBytes { get; }
        public int RawPixels { get; }

        public DecompressionStageMetrics(int lzwBytes, int rleBytes, int packedBytes, int rawPixels)
        {
            LzwBytes = lzwBytes;
            RleBytes = rleBytes;
            PackedBytes = packedBytes;
            RawPixels = rawPixels;
        }
    }
}
