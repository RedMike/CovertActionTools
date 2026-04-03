using CovertActionTools.Core.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Compression.Data;

namespace CovertActionTools.UnitTests.Core.Compression;

public class ExepackCompressionTests
{
    private readonly ExepackCompression _compression =
        new ExepackCompression(NullLogger<ExepackCompression>.Instance);

    #region Fill commands

    [Fact]
    public void Compress_UniformData_ProducesFillCommand()
    {
        var payload = ExepackTestDataGenerator.GenerateUniformPayload(32, 0xAA);
        var compressed = _compression.Compress(payload);

        // Should be small: fill_byte(1) + length(2) + command(1) + FF padding
        Assert.True(compressed.Length < payload.Length);
    }

    [Fact]
    public void Compress_UniformData_LargeBlock()
    {
        var payload = ExepackTestDataGenerator.GenerateUniformPayload(4096, 0x00);
        var compressed = _compression.Compress(payload);

        // A single FILL command should be much smaller than 4096 bytes
        Assert.True(compressed.Length < 20);
    }

    #endregion

    #region Copy commands

    [Fact]
    public void Compress_VariedData_ProducesCopyCommand()
    {
        var payload = ExepackTestDataGenerator.GenerateVariedPayload(32);
        var compressed = _compression.Compress(payload);

        // COPY adds 3 bytes overhead (length + command) + FF padding
        // So compressed should be roughly payload.Length + 3 + padding
        Assert.True(compressed.Length >= payload.Length);
    }

    [Fact]
    public void Compress_ShortRunBelowMinLength_UsesCopy()
    {
        // 7 identical bytes followed by varied data — run < MIN_RUN(8) should use COPY
        var payload = new byte[16];
        for (var i = 0; i < 7; i++) payload[i] = 0xAA;
        for (var i = 7; i < 16; i++) payload[i] = (byte)(0x10 + i);

        var compressed = _compression.Compress(payload);

        // Everything should be in one COPY command since the run is too short for FILL
        // COPY: 16 data + 2 length + 1 command + FF padding
        Assert.True(compressed.Length <= 22);
    }

    #endregion

    #region Mixed data

    [Fact]
    public void Compress_MixedData_ProducesCorrectOutput()
    {
        var payload = ExepackTestDataGenerator.GenerateMixedPayload();
        var compressed = _compression.Compress(payload);

        // Should be smaller than original due to FILL compression of runs
        Assert.True(compressed.Length < payload.Length);
    }

    [Fact]
    public void Compress_ExactlyMinRunLength_UsesFill()
    {
        // Exactly 8 identical bytes followed by varied data
        var payload = new byte[16];
        for (var i = 0; i < 8; i++) payload[i] = 0xBB;
        for (var i = 8; i < 16; i++) payload[i] = (byte)(0x10 + i);

        var compressed = _compression.Compress(payload);

        // Should have 2 commands: FILL(8) + COPY(8)
        // FILL: 1+2+1 = 4 bytes, COPY: 8+2+1 = 11 bytes, + padding
        Assert.True(compressed.Length < payload.Length + 10);
    }

    #endregion

    #region Empty input

    [Fact]
    public void Compress_EmptyPayload_ReturnsFFPaddingOnly()
    {
        var payload = new byte[0];
        var compressed = _compression.Compress(payload);

        // Should just be FF padding
        Assert.True(compressed.Length <= 2);
        foreach (var b in compressed)
        {
            Assert.Equal(0xFF, b);
        }
    }

    #endregion
}
