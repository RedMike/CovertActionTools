using CovertActionTools.Core.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Compression.Data;

namespace CovertActionTools.UnitTests.Core.Compression;

public class ExepackDecompressionTests
{
    private readonly ExepackDecompression _decompression =
        new ExepackDecompression(NullLogger<ExepackDecompression>.Instance);

    #region Fill command

    [Fact]
    public void Decompress_FillCommand_ProducesRepeatedBytes()
    {
        // Single FILL+STOP: fill 16 bytes with 0xAA
        var packed = ExepackTestDataGenerator.BuildSingleFillCommand(0xAA, 16);
        var destParagraphs = 1; // 16 bytes

        var result = _decompression.Decompress(packed, destParagraphs);

        Assert.Equal(16, result.Data.Length);
        for (var i = 0; i < 16; i++)
        {
            Assert.Equal(0xAA, result.Data[i]);
        }
    }

    [Fact]
    public void Decompress_FillCommand_LargeLength()
    {
        var packed = ExepackTestDataGenerator.BuildSingleFillCommand(0x42, 256);
        var destParagraphs = 16; // 256 bytes

        var result = _decompression.Decompress(packed, destParagraphs);

        Assert.Equal(256, result.Data.Length);
        for (var i = 0; i < 256; i++)
        {
            Assert.Equal(0x42, result.Data[i]);
        }
    }

    #endregion

    #region Copy command

    [Fact]
    public void Decompress_CopyCommand_CopiesLiterally()
    {
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                                0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        var packed = ExepackTestDataGenerator.BuildSingleCopyCommand(data);
        var destParagraphs = 1; // 16 bytes

        var result = _decompression.Decompress(packed, destParagraphs);

        Assert.Equal(data, result.Data);
    }

    #endregion

    #region Mixed commands

    [Fact]
    public void Decompress_MixedCommands_ProducesCorrectOutput()
    {
        // Two commands: FILL 8 bytes of 0x00 at the start, then COPY 8 bytes at the end
        var copyData = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 };
        var packed = ExepackTestDataGenerator.BuildTwoCommands(0x00, 8, copyData);
        var destParagraphs = 1; // 16 bytes

        var result = _decompression.Decompress(packed, destParagraphs);

        // First 8 bytes should be from COPY (processed last by decompressor, written to lower addresses)
        Assert.Equal(copyData, result.Data[0..8]);
        // Last 8 bytes should be filled with 0x00 (processed first, written to higher addresses)
        for (var i = 8; i < 16; i++)
        {
            Assert.Equal(0x00, result.Data[i]);
        }
    }

    #endregion

    #region Dead zone boundary

    [Fact]
    public void Decompress_ReturnsCorrectDeadZoneBoundary()
    {
        // FILL 8 bytes leaves the first 8 bytes of a 16-byte output as dead zone
        var packed = ExepackTestDataGenerator.BuildSingleFillCommand(0xCC, 8);
        var destParagraphs = 1; // 16 bytes

        var result = _decompression.Decompress(packed, destParagraphs);

        Assert.Equal(8, result.DeadZoneBoundary);
        // Dead zone bytes should be 0 (untouched)
        for (var i = 0; i < 8; i++)
        {
            Assert.Equal(0x00, result.Data[i]);
        }
        // Written bytes should be 0xCC
        for (var i = 8; i < 16; i++)
        {
            Assert.Equal(0xCC, result.Data[i]);
        }
    }

    [Fact]
    public void Decompress_FullFill_DeadZoneBoundaryIsZero()
    {
        // FILL covers entire output → dead zone boundary is 0
        var packed = ExepackTestDataGenerator.BuildSingleFillCommand(0xDD, 16);
        var destParagraphs = 1;

        var result = _decompression.Decompress(packed, destParagraphs);

        Assert.Equal(0, result.DeadZoneBoundary);
    }

    #endregion

    #region FF padding

    [Fact]
    public void Decompress_SkipsTrailingFFPadding()
    {
        var baseCommand = ExepackTestDataGenerator.BuildSingleFillCommand(0xBB, 16);
        // Add trailing FF padding
        var packed = new byte[baseCommand.Length + 4];
        System.Array.Copy(baseCommand, 0, packed, 0, baseCommand.Length);
        packed[baseCommand.Length] = 0xFF;
        packed[baseCommand.Length + 1] = 0xFF;
        packed[baseCommand.Length + 2] = 0xFF;
        packed[baseCommand.Length + 3] = 0xFF;

        var result = _decompression.Decompress(packed, 1);

        for (var i = 0; i < 16; i++)
        {
            Assert.Equal(0xBB, result.Data[i]);
        }
    }

    #endregion

    #region Error cases

    [Fact]
    public void Decompress_AllFF_Throws()
    {
        var packed = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };

        Assert.Throws<System.InvalidOperationException>(() =>
            _decompression.Decompress(packed, 1));
    }

    [Fact]
    public void Decompress_UnknownCommand_Throws()
    {
        // Command 0xC0 is not a valid EXEPACK command
        var packed = new byte[] { 0x00, 0x01, 0x00, 0xC0 };

        Assert.Throws<System.InvalidOperationException>(() =>
            _decompression.Decompress(packed, 1));
    }

    #endregion
}
