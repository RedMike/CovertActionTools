using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Parsers.Data;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class AnimationImporterTests : IDisposable
{
#if DEBUG
    private static readonly JsonSerializerOptions JsonEnumOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
#else
    private static readonly JsonSerializerOptions JsonEnumOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };
#endif

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    private readonly AnimationImporter _importer;
    private readonly string _tempDir;

    public AnimationImporterTests()
    {
        var imageImporter = new SharedImageImporter(NullLogger<SharedImageImporter>.Instance);
        _importer = new AnimationImporter(NullLogger<AnimationImporter>.Instance, imageImporter);
        _tempDir = Path.Combine(Path.GetTempPath(), $"AnimationImporterTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region GetMessage

    [Fact]
    public void GetMessage_ReturnsExpectedString()
    {
        Assert.Equal("Processing animations..", _importer.GetMessage());
    }

    #endregion

    #region CheckIfValid

    [Fact]
    public void CheckIfValid_WithJsonFiles_ReturnsTrue()
    {
        var animDir = Path.Combine(_tempDir, "animation");
        Directory.CreateDirectory(animDir);
        File.WriteAllText(Path.Combine(animDir, "test_metadata.json"), "{}");

        Assert.True(_importer.CheckIfValid(_tempDir));
    }

    [Fact]
    public void CheckIfValid_EmptySubdirectory_ReturnsFalse()
    {
        var animDir = Path.Combine(_tempDir, "animation");
        Directory.CreateDirectory(animDir);

        Assert.False(_importer.CheckIfValid(_tempDir));
    }

    [Fact]
    public void CheckIfValid_NoSubdirectory_Throws()
    {
        Assert.ThrowsAny<Exception>(() => _importer.CheckIfValid(_tempDir));
    }

    #endregion

    #region Import single item

    [Fact]
    public void Import_SingleAnimation_DeserializesCorrectly()
    {
        WriteAnimationFiles("anim1");

        _importer.Start(_tempDir);
        var done = _importer.RunStep();

        Assert.True(done);

        var result = new PackageModel();
        _importer.SetResult(result);

        Assert.Single(result.Animations);
        Assert.True(result.Animations.ContainsKey("anim1"));
        var anim = result.Animations["anim1"];
        Assert.Equal("anim1", anim.Key);
        Assert.Equal("Test Animation anim1", anim.Metadata.Name);
        Assert.Equal(99, anim.Data.BoundingWidth);
        Assert.Equal(79, anim.Data.BoundingHeight);
        Assert.Equal(AnimationModel.BackgroundType.ClearToColor, anim.Data.BackgroundType);
    }

    [Fact]
    public void Import_SingleAnimation_ParsesControlData()
    {
        WriteAnimationFiles("anim1");

        _importer.Start(_tempDir);
        _importer.RunStep();

        var result = new PackageModel();
        _importer.SetResult(result);

        var anim = result.Animations["anim1"];
        Assert.NotEmpty(anim.Control.Instructions);
        Assert.NotEmpty(anim.Control.Steps);
    }

    [Fact]
    public void Import_AnimationWithImages_ReadsImages()
    {
        WriteAnimationFiles("anim1", imageIds: new[] { 0, 1 });

        _importer.Start(_tempDir);
        _importer.RunStep();

        var result = new PackageModel();
        _importer.SetResult(result);

        var anim = result.Animations["anim1"];
        Assert.Equal(2, anim.Images.Count);
        Assert.True(anim.Images.ContainsKey(0));
        Assert.True(anim.Images.ContainsKey(1));
        Assert.Equal(4, anim.Images[0].Data.Width);
        Assert.Equal(4, anim.Images[0].Data.Height);
    }

    #endregion

    #region Import multiple items

    [Fact]
    public void Import_MultipleAnimations_AllDeserializedCorrectly()
    {
        WriteAnimationFiles("animA");
        WriteAnimationFiles("animB");

        _importer.Start(_tempDir);
        var done1 = _importer.RunStep();
        Assert.False(done1);

        var done2 = _importer.RunStep();
        Assert.True(done2);

        var result = new PackageModel();
        _importer.SetResult(result);

        Assert.Equal(2, result.Animations.Count);
        Assert.True(result.Animations.ContainsKey("animA"));
        Assert.True(result.Animations.ContainsKey("animB"));
    }

    #endregion

    #region SetResult populates correct field

    [Fact]
    public void SetResult_PopulatesAnimationsFieldOnPackageModel()
    {
        WriteAnimationFiles("anim1");

        _importer.Start(_tempDir);
        _importer.RunStep();

        var packageModel = new PackageModel();
        _importer.SetResult(packageModel);

        Assert.NotEmpty(packageModel.Animations);
        Assert.True(packageModel.Animations.ContainsKey("anim1"));
    }

    #endregion

    #region Helpers

    private void WriteAnimationFiles(string key, int[]? imageIds = null)
    {
        var animDir = Path.Combine(_tempDir, "animation");
        if (!Directory.Exists(animDir))
        {
            Directory.CreateDirectory(animDir);
        }

        var keyDir = Path.Combine(animDir, key);
        Directory.CreateDirectory(keyDir);

        // Metadata
        var metadata = new SharedMetadata
        {
            Name = $"Test Animation {key}",
            Comment = "Test animation data"
        };
        File.WriteAllText(Path.Combine(keyDir, $"{key}_metadata.json"),
            JsonSerializer.Serialize(metadata, JsonEnumOptions));

        // Global data (AnimationImporter reads with JsonStringEnumConverter)
        var globalData = new AnimationModel.GlobalData
        {
            BoundingWidth = 99,
            BoundingHeight = 79,
            GlobalFrameSkip = 1,
            BackgroundType = AnimationModel.BackgroundType.ClearToColor,
            ClearColor = 0,
            Unknown2 = 0,
            ColorMapping = new Dictionary<byte, byte>(),
            ImageIdToIndex = new Dictionary<int, int>(),
            ImageIndexToUnknownData = new Dictionary<int, int>()
        };
        File.WriteAllText(Path.Combine(keyDir, $"{key}_global.json"),
            JsonSerializer.Serialize(globalData, JsonEnumOptions));

        // Instructions
        var control = new AnimationModel.ControlData
        {
            Instructions = new List<AnimationModel.AnimationInstruction>
            {
                new AnimationModel.AnimationInstruction
                {
                    Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.End
                }
            },
            InstructionLabels = new Dictionary<string, int>(),
            Steps = new List<AnimationModel.AnimationStep>
            {
                new AnimationModel.AnimationStep
                {
                    Type = AnimationModel.AnimationStep.StepType.Stop
                }
            },
            StepLabels = new Dictionary<string, int>()
        };
        File.WriteAllText(Path.Combine(keyDir, $"{key}_instructions.txt"),
            control.GetSerialisedInstructions());
        File.WriteAllText(Path.Combine(keyDir, $"{key}_steps.txt"),
            control.GetSerialisedSteps());

        // Images directory must always exist (importer expects it)
        var imagesDir = Path.Combine(keyDir, "images");
        Directory.CreateDirectory(imagesDir);

        if (imageIds != null)
        {
            foreach (var imageId in imageIds)
            {
                var imageData = new SharedImageModel.ImageData
                {
                    Width = 4,
                    Height = 4,
                    CompressionDictionaryWidth = 11
                };
                File.WriteAllText(
                    Path.Combine(imagesDir, $"{key}_{imageId}_VGA_metadata.json"),
                    JsonSerializer.Serialize(imageData, JsonOptions));

                var pixels = SharedImageTestDataGenerator.GenerateUniformPixels(4, 4);
                var pngBytes = ImageConversion.VgaToTexture(4, 4, pixels);
                File.WriteAllBytes(
                    Path.Combine(imagesDir, $"{key}_{imageId}_VGA.png"),
                    pngBytes);
            }
        }
    }

    #endregion
}
