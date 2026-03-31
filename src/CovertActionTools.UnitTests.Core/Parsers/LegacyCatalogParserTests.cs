using System;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class LegacyCatalogParserTests : IDisposable
{
    private readonly LegacyCatalogParser _parser;
    private readonly StubLzwDecompression _stubDecompression;
    private readonly string _tempDir;

    public LegacyCatalogParserTests()
    {
        _stubDecompression = new StubLzwDecompression();
        var imageParser = new SharedImageParser(NullLogger<SharedImageParser>.Instance, _stubDecompression);
        _parser = new LegacyCatalogParser(NullLogger<LegacyCatalogParser>.Instance, imageParser);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyCatalogParserTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Entry parsing

    [Fact]
    public void Parse_SingleEntry_ReadsEntryName()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = CatalogTestDataGenerator.BuildCatalogFile(new[]
        {
            new CatalogTestDataGenerator.CatalogEntry { Name = "SPRITE1", Width = 4, Height = 4 }
        });
        CatalogTestDataGenerator.WriteCatalogFile(_tempDir, "TESTCAT", data);

        var catalogs = RunParser();

        Assert.True(catalogs["TESTCAT"].Entries.ContainsKey("SPRITE1"));
    }

    [Fact]
    public void Parse_SingleEntry_ReadsImageDimensions()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(8, 6);
        _stubDecompression.SetResult(pixels);
        var data = CatalogTestDataGenerator.BuildCatalogFile(new[]
        {
            new CatalogTestDataGenerator.CatalogEntry { Name = "IMG", Width = 8, Height = 6 }
        });
        CatalogTestDataGenerator.WriteCatalogFile(_tempDir, "TESTCAT", data);

        var catalogs = RunParser();

        var image = catalogs["TESTCAT"].Entries["IMG"];
        Assert.Equal(8, image.Data.Width);
        Assert.Equal(6, image.Data.Height);
    }

    [Fact]
    public void Parse_SingleEntry_PreservesPixelData()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = CatalogTestDataGenerator.BuildCatalogFile(new[]
        {
            new CatalogTestDataGenerator.CatalogEntry { Name = "IMG", Width = 4, Height = 4 }
        });
        CatalogTestDataGenerator.WriteCatalogFile(_tempDir, "TESTCAT", data);

        var catalogs = RunParser();

        Assert.Equal(pixels, catalogs["TESTCAT"].Entries["IMG"].RawVgaImageData);
    }

    [Fact]
    public void Parse_MultipleEntries_ReadsAllEntries()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = CatalogTestDataGenerator.BuildCatalogFile(new[]
        {
            new CatalogTestDataGenerator.CatalogEntry { Name = "A", Width = 4, Height = 4 },
            new CatalogTestDataGenerator.CatalogEntry { Name = "B", Width = 4, Height = 4 },
            new CatalogTestDataGenerator.CatalogEntry { Name = "C", Width = 4, Height = 4 }
        });
        CatalogTestDataGenerator.WriteCatalogFile(_tempDir, "TESTCAT", data);

        var catalogs = RunParser();

        Assert.Equal(3, catalogs["TESTCAT"].Entries.Count);
        Assert.True(catalogs["TESTCAT"].Entries.ContainsKey("A"));
        Assert.True(catalogs["TESTCAT"].Entries.ContainsKey("B"));
        Assert.True(catalogs["TESTCAT"].Entries.ContainsKey("C"));
    }

    #endregion

    #region Catalog data

    [Fact]
    public void Parse_PopulatesKeysInCatalogData()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = CatalogTestDataGenerator.BuildCatalogFile(new[]
        {
            new CatalogTestDataGenerator.CatalogEntry { Name = "X", Width = 4, Height = 4 },
            new CatalogTestDataGenerator.CatalogEntry { Name = "Y", Width = 4, Height = 4 }
        });
        CatalogTestDataGenerator.WriteCatalogFile(_tempDir, "TESTCAT", data);

        var catalogs = RunParser();

        Assert.Contains("X", catalogs["TESTCAT"].Data.Keys);
        Assert.Contains("Y", catalogs["TESTCAT"].Data.Keys);
    }

    [Fact]
    public void Parse_SetsCatalogKey()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = CatalogTestDataGenerator.BuildCatalogFile(new[]
        {
            new CatalogTestDataGenerator.CatalogEntry { Name = "IMG", Width = 4, Height = 4 }
        });
        CatalogTestDataGenerator.WriteCatalogFile(_tempDir, "MYCAT", data);

        var catalogs = RunParser();

        Assert.Equal("MYCAT", catalogs["MYCAT"].Key);
    }

    #endregion

    #region Metadata

    [Fact]
    public void Parse_SetsMetadata()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        var data = CatalogTestDataGenerator.BuildCatalogFile(new[]
        {
            new CatalogTestDataGenerator.CatalogEntry { Name = "IMG", Width = 4, Height = 4 }
        });
        CatalogTestDataGenerator.WriteCatalogFile(_tempDir, "MYCAT", data);

        var catalogs = RunParser();

        Assert.Equal("MYCAT", catalogs["MYCAT"].Metadata.Name);
        Assert.Equal("Legacy import", catalogs["MYCAT"].Metadata.Comment);
    }

    #endregion

    #region Multiple files

    [Fact]
    public void Parse_MultipleCatFiles_ParsesAll()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        CatalogTestDataGenerator.WriteCatalogFile(_tempDir, "CAT1",
            CatalogTestDataGenerator.BuildCatalogFile(new[]
            {
                new CatalogTestDataGenerator.CatalogEntry { Name = "A", Width = 4, Height = 4 }
            }));
        CatalogTestDataGenerator.WriteCatalogFile(_tempDir, "CAT2",
            CatalogTestDataGenerator.BuildCatalogFile(new[]
            {
                new CatalogTestDataGenerator.CatalogEntry { Name = "B", Width = 4, Height = 4 }
            }));

        var catalogs = RunParser();

        Assert.Equal(2, catalogs.Count);
        Assert.True(catalogs.ContainsKey("CAT1"));
        Assert.True(catalogs.ContainsKey("CAT2"));
    }

    #endregion

    #region SetResult

    [Fact]
    public void SetResult_PopulatesCatalogsOnPackageModel()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        _stubDecompression.SetResult(pixels);
        CatalogTestDataGenerator.WriteCatalogFile(_tempDir, "TESTCAT",
            CatalogTestDataGenerator.BuildCatalogFile(new[]
            {
                new CatalogTestDataGenerator.CatalogEntry { Name = "IMG", Width = 4, Height = 4 }
            }));

        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);

        Assert.NotEmpty(model.Catalogs);
        Assert.True(model.Catalogs.ContainsKey("TESTCAT"));
    }

    #endregion

    #region Helpers

    private System.Collections.Generic.Dictionary<string, CatalogModel> RunParser()
    {
        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);
        return model.Catalogs;
    }

    #endregion

    private class StubLzwDecompression : ILzwDecompression
    {
        private byte[] _result = Array.Empty<byte>();

        public void SetResult(byte[] data)
        {
            _result = data;
        }

        public DecompressionResult Decompress(int width, int height, int maxWordWidth, BinaryReader reader,
            bool collectMetrics = false)
        {
            return new DecompressionResult(_result, 0);
        }
    }
}
