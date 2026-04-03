using System;
using CovertActionTools.Core.Compression;
using Xunit;

using CovertActionTools.UnitTests.Core.Compression.Data;

namespace CovertActionTools.UnitTests.Core.Compression;

public class ExepackUtilitiesTests
{
    #region ParseMzHeader

    [Fact]
    public void ParseMzHeader_ValidHeader_ReturnsCorrectFields()
    {
        var header = new byte[28];
        header[0] = (byte)'M';
        header[1] = (byte)'Z';
        // last_page_bytes = 0x0100
        header[2] = 0x00; header[3] = 0x01;
        // pages = 0x0002
        header[4] = 0x02; header[5] = 0x00;
        // reloc_count = 5
        header[6] = 0x05; header[7] = 0x00;
        // header_paras = 32 (512 bytes)
        header[8] = 0x20; header[9] = 0x00;
        // init_cs = 0x0100
        header[22] = 0x00; header[23] = 0x01;
        // init_ip = 0x0010
        header[20] = 0x10; header[21] = 0x00;

        var result = ExepackUtilities.ParseMzHeader(header);

        Assert.NotNull(result);
        Assert.Equal(0x0100, result.LastPageBytes);
        Assert.Equal(2, result.Pages);
        Assert.Equal(5, result.RelocCount);
        Assert.Equal(0x20, result.HeaderParagraphs);
        Assert.Equal(512, result.HeaderSize);
        Assert.Equal(0x0100, result.InitCS);
        Assert.Equal(0x0010, result.InitIP);
    }

    [Fact]
    public void ParseMzHeader_TooShort_ReturnsNull()
    {
        var header = new byte[10];
        header[0] = (byte)'M';
        header[1] = (byte)'Z';

        var result = ExepackUtilities.ParseMzHeader(header);

        Assert.Null(result);
    }

    [Fact]
    public void ParseMzHeader_NotMZ_ReturnsNull()
    {
        var header = new byte[28];
        header[0] = (byte)'P';
        header[1] = (byte)'E';

        var result = ExepackUtilities.ParseMzHeader(header);

        Assert.Null(result);
    }

    [Fact]
    public void ParseMzHeader_Null_ReturnsNull()
    {
        var result = ExepackUtilities.ParseMzHeader(null);

        Assert.Null(result);
    }

    #endregion

    #region Relocation table roundtrip

    [Fact]
    public void RelocationTable_Roundtrip_PreservesEntries()
    {
        // Create relocations: (seg, off) pairs in flat array
        var relocations = new ushort[]
        {
            0x0000, 0x0010, // block 0, offset 0x10
            0x0000, 0x0100, // block 0, offset 0x100
            0x1000, 0x0020, // block 1, offset 0x20
            0x3000, 0x0050, // block 3, offset 0x50
        };

        var table = ExepackUtilities.BuildExepackRelocationTable(relocations);

        // Build a fake EXEPACK segment with the table after a stub + error string
        var errorString = System.Text.Encoding.ASCII.GetBytes("Packed file is corrupt");
        var segment = new byte[0x10 + 255 + errorString.Length + table.Length];
        // Header + stub fill
        Array.Copy(errorString, 0, segment, 0x10 + 255, errorString.Length);
        Array.Copy(table, 0, segment, 0x10 + 255 + errorString.Length, table.Length);

        var extracted = ExepackUtilities.ExtractRelocations(segment);

        Assert.Equal(relocations, extracted);
    }

    [Fact]
    public void RelocationTable_Empty_ProducesEmptyResult()
    {
        var relocations = Array.Empty<ushort>();
        var table = ExepackUtilities.BuildExepackRelocationTable(relocations);

        // Should have 16 blocks, each with count=0
        Assert.Equal(32, table.Length); // 16 blocks × 2 bytes each

        // Build segment and extract
        var errorString = System.Text.Encoding.ASCII.GetBytes("Packed file is corrupt");
        var segment = new byte[0x10 + 255 + errorString.Length + table.Length];
        Array.Copy(errorString, 0, segment, 0x10 + 255, errorString.Length);
        Array.Copy(table, 0, segment, 0x10 + 255 + errorString.Length, table.Length);

        var extracted = ExepackUtilities.ExtractRelocations(segment);

        Assert.Empty(extracted);
    }

    #endregion
}
