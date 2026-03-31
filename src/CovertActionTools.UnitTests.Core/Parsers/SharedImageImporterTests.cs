using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class SharedImageImporterTests : IDisposable
{
    private readonly SharedImageImporter _importer;
    private readonly string _tempDir;

    public SharedImageImporterTests()
    {
        _importer = new SharedImageImporter(NullLogger<SharedImageImporter>.Instance);
        _tempDir = Path.Combine(Path.GetTempPath(), $"SharedImageImporterTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region ReadImageData

    [Fact]
    public void ReadImageData_DeserializesWidthAndHeight()
    {
        var imageData = new SharedImageModel.ImageData { Width = 32, Height = 16, CompressionDictionaryWidth = 11 };
        WriteJsonFile(imageData, "test", "IMG");

        var result = _importer.ReadImageData(_tempDir, "test", "IMG");

        Assert.Equal(32, result.Width);
        Assert.Equal(16, result.Height);
    }

    [Fact]
    public void ReadImageData_DeserializesDictionaryWidth()
    {
        var imageData = new SharedImageModel.ImageData { Width = 4, Height = 4, CompressionDictionaryWidth = 10 };
        WriteJsonFile(imageData, "test", "IMG");

        var result = _importer.ReadImageData(_tempDir, "test", "IMG");

        Assert.Equal(10, result.CompressionDictionaryWidth);
    }

    [Fact]
    public void ReadImageData_DeserializesColorMappings()
    {
        var mappings = SharedImageTestDataGenerator.CreateIdentityCgaColorMappings();
        var imageData = new SharedImageModel.ImageData
        {
            Width = 4, Height = 4, CompressionDictionaryWidth = 11, LegacyColorMappings = mappings
        };
        WriteJsonFile(imageData, "test", "IMG");

        var result = _importer.ReadImageData(_tempDir, "test", "IMG");

        Assert.NotNull(result.LegacyColorMappings);
        Assert.Equal(16, result.LegacyColorMappings.Count);
    }

    [Fact]
    public void ReadImageData_NullColorMappings_RemainsNull()
    {
        var imageData = new SharedImageModel.ImageData
        {
            Width = 4, Height = 4, CompressionDictionaryWidth = 11, LegacyColorMappings = null
        };
        WriteJsonFile(imageData, "test", "IMG");

        var result = _importer.ReadImageData(_tempDir, "test", "IMG");

        Assert.Null(result.LegacyColorMappings);
    }

    [Fact]
    public void ReadImageData_MissingFile_Throws()
    {
        Assert.Throws<Exception>(() => _importer.ReadImageData(_tempDir, "nonexistent", "IMG"));
    }

    #endregion

    #region ReadMetadata

    [Fact]
    public void ReadMetadata_DeserializesNameAndComment()
    {
        var metadata = new SharedMetadata { Name = "TestImage", Comment = "A test comment" };
        WriteJsonFile(metadata, "test", "META");

        var result = _importer.ReadMetadata(_tempDir, "test", "META");

        Assert.Equal("TestImage", result.Name);
        Assert.Equal("A test comment", result.Comment);
    }

    [Fact]
    public void ReadMetadata_MissingFile_Throws()
    {
        Assert.Throws<Exception>(() => _importer.ReadMetadata(_tempDir, "nonexistent", "META"));
    }

    #endregion

    #region ReadVgaImageData

    [Fact]
    public void ReadVgaImageData_ReturnsRawAndTextureBytes()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        WritePngFile(pixels, 4, 4, "test");

        var (raw, texture) = _importer.ReadVgaImageData(_tempDir, "test", 4, 4);

        Assert.Equal(4 * 4, raw.Length);
        Assert.Equal(4 * 4 * 4, texture.Length);
    }

    [Fact]
    public void ReadVgaImageData_RawBytesMatchOriginalPixels()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4);
        WritePngFile(pixels, 4, 4, "test");

        var (raw, _) = _importer.ReadVgaImageData(_tempDir, "test", 4, 4);

        Assert.Equal(pixels, raw);
    }

    [Fact]
    public void ReadVgaImageData_MissingFile_Throws()
    {
        Assert.Throws<Exception>(() => _importer.ReadVgaImageData(_tempDir, "nonexistent", 4, 4));
    }

    #endregion

    #region ReadImage

    [Fact]
    public void ReadImage_Format0x07_AssemblesModelCorrectly()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
        var imageData = new SharedImageModel.ImageData
        {
            Width = 4, Height = 4, CompressionDictionaryWidth = 11, LegacyColorMappings = null
        };
        WriteJsonFile(imageData, "test", "IMG");
        WritePngFile(pixels, 4, 4, "test");

        var model = _importer.ReadImage(_tempDir, "test", "IMG");

        Assert.Equal(4, model.Data.Width);
        Assert.Equal(4, model.Data.Height);
        Assert.Equal(pixels, model.RawVgaImageData);
        Assert.NotEmpty(model.VgaImageData);
        Assert.Empty(model.CgaImageData);
    }

    [Fact]
    public void ReadImage_Format0x0F_ProducesCgaImageData()
    {
        var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4, value: 1);
        var mappings = SharedImageTestDataGenerator.CreateIdentityCgaColorMappings();
        var imageData = new SharedImageModel.ImageData
        {
            Width = 4, Height = 4, CompressionDictionaryWidth = 11, LegacyColorMappings = mappings
        };
        WriteJsonFile(imageData, "test", "IMG");
        WritePngFile(pixels, 4, 4, "test");

        var model = _importer.ReadImage(_tempDir, "test", "IMG");

        Assert.NotNull(model.Data.LegacyColorMappings);
        Assert.NotEmpty(model.CgaImageData);
    }

    [Fact]
    public void ReadImage_Format0x07_VariedPixels_PreservesData()
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(8, 4);
        var imageData = new SharedImageModel.ImageData
        {
            Width = 8, Height = 4, CompressionDictionaryWidth = 11, LegacyColorMappings = null
        };
        WriteJsonFile(imageData, "test", "IMG");
        WritePngFile(pixels, 8, 4, "test");

        var model = _importer.ReadImage(_tempDir, "test", "IMG");

        Assert.Equal(pixels, model.RawVgaImageData);
    }

    #endregion

    #region Helpers

    private void WriteJsonFile<T>(T data, string key, string suffix)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_tempDir, $"{key}_{suffix}.json"), json);
    }

    private void WritePngFile(byte[] rawPixels, int width, int height, string filename)
    {
        var pngBytes = ImageConversion.VgaToTexture(width, height, rawPixels);
        File.WriteAllBytes(Path.Combine(_tempDir, $"{filename}_VGA.png"), pngBytes);
    }

    #endregion
}
