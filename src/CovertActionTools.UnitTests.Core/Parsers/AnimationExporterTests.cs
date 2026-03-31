using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class AnimationExporterTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly AnimationExporter _exporter;
    private readonly string _tempDir;

    public AnimationExporterTests()
    {
        var stubCompression = new StubLzwCompression();
        var imageExporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, stubCompression);
        _exporter = new AnimationExporter(NullLogger<AnimationExporter>.Instance, imageExporter);
        _tempDir = Path.Combine(Path.GetTempPath(), $"AnimationExporterTests_{Guid.NewGuid():N}");
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
        Assert.Equal("Processing animations..", _exporter.GetMessage());
    }

    #endregion

    #region Export creates subdirectory and writes files

    [Fact]
    public void Export_SingleAnimation_CreatesAnimationSubdirectory()
    {
        var model = CreatePackageModel(CreateSampleAnimation("anim1"));

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        Assert.True(Directory.Exists(Path.Combine(_tempDir, "animation")));
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "animation", "anim1")));
    }

    [Fact]
    public void Export_SingleAnimation_WritesMetadataFile()
    {
        var model = CreatePackageModel(CreateSampleAnimation("anim1"));

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "animation", "anim1", "anim1_metadata.json");
        Assert.True(File.Exists(filePath));

        var json = File.ReadAllText(filePath);
        var metadata = JsonSerializer.Deserialize<SharedMetadata>(json, JsonOptions);
        Assert.NotNull(metadata);
        Assert.Equal("Test Animation anim1", metadata.Name);
    }

    [Fact]
    public void Export_SingleAnimation_WritesGlobalDataFile()
    {
        var anim = CreateSampleAnimation("anim1");
        var model = CreatePackageModel(anim);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var filePath = Path.Combine(_tempDir, "animation", "anim1", "anim1_global.json");
        Assert.True(File.Exists(filePath));

        var json = File.ReadAllText(filePath);
        var globalData = JsonSerializer.Deserialize<AnimationModel.GlobalData>(json, JsonOptions);
        Assert.NotNull(globalData);
        Assert.Equal(99, globalData.BoundingWidth);
        Assert.Equal(79, globalData.BoundingHeight);
        Assert.Equal(1, globalData.GlobalFrameSkip);
        Assert.Equal(AnimationModel.BackgroundType.ClearToColor, globalData.BackgroundType);
    }

    [Fact]
    public void Export_SingleAnimation_WritesInstructionsAndStepsFiles()
    {
        var model = CreatePackageModel(CreateSampleAnimation("anim1"));

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var instructionsPath = Path.Combine(_tempDir, "animation", "anim1", "anim1_instructions.txt");
        var stepsPath = Path.Combine(_tempDir, "animation", "anim1", "anim1_steps.txt");
        Assert.True(File.Exists(instructionsPath));
        Assert.True(File.Exists(stepsPath));

        var instructionsText = File.ReadAllText(instructionsPath);
        Assert.Contains("End", instructionsText);

        var stepsText = File.ReadAllText(stepsPath);
        Assert.Contains("Stop", stepsText);
    }

    [Fact]
    public void Export_AnimationWithImages_WritesImageFiles()
    {
        var anim = CreateSampleAnimation("anim1");
        anim.Images[0] = CreateTestImage(4, 4);
        anim.Images[1] = CreateTestImage(4, 4);
        var model = CreatePackageModel(anim);

        _exporter.Start(_tempDir, model);
        _exporter.RunStep();

        var imagesPath = Path.Combine(_tempDir, "animation", "anim1", "images");
        Assert.True(Directory.Exists(imagesPath));
        Assert.True(File.Exists(Path.Combine(imagesPath, "anim1_0_VGA_metadata.json")));
        Assert.True(File.Exists(Path.Combine(imagesPath, "anim1_0_VGA.png")));
        Assert.True(File.Exists(Path.Combine(imagesPath, "anim1_1_VGA_metadata.json")));
        Assert.True(File.Exists(Path.Combine(imagesPath, "anim1_1_VGA.png")));
    }

    #endregion

    #region Multiple animations

    [Fact]
    public void Export_MultipleAnimations_WritesOneSubdirectoryPerAnimation()
    {
        var model = CreatePackageModel(
            CreateSampleAnimation("animA"),
            CreateSampleAnimation("animB"));

        _exporter.Start(_tempDir, model);
        var done1 = _exporter.RunStep();
        Assert.False(done1);

        var done2 = _exporter.RunStep();
        Assert.True(done2);

        Assert.True(Directory.Exists(Path.Combine(_tempDir, "animation", "animA")));
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "animation", "animB")));
    }

    #endregion

    #region GetItemCount tracks progress

    [Fact]
    public void GetItemCount_TracksProgressCorrectly()
    {
        var model = CreatePackageModel(
            CreateSampleAnimation("a1"),
            CreateSampleAnimation("a2"),
            CreateSampleAnimation("a3"));

        _exporter.Start(_tempDir, model);
        var (current0, total0) = _exporter.GetItemCount();
        Assert.Equal(0, current0);
        Assert.Equal(3, total0);

        _exporter.RunStep();
        var (current1, total1) = _exporter.GetItemCount();
        Assert.Equal(0, current1);
        Assert.Equal(3, total1);

        _exporter.RunStep();
        var (current2, total2) = _exporter.GetItemCount();
        Assert.Equal(1, current2);
        Assert.Equal(3, total2);
    }

    #endregion

    #region RunStep completion

    [Fact]
    public void RunStep_SingleAnimation_ReturnsTrueImmediately()
    {
        var model = CreatePackageModel(CreateSampleAnimation("a1"));

        _exporter.Start(_tempDir, model);
        var done = _exporter.RunStep();

        Assert.True(done);
    }

    #endregion

    #region Helpers

    private static AnimationModel CreateSampleAnimation(string key)
    {
        return new AnimationModel
        {
            Key = key,
            Metadata = new SharedMetadata
            {
                Name = $"Test Animation {key}",
                Comment = "Test animation data"
            },
            Data = new AnimationModel.GlobalData
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
            },
            Control = new AnimationModel.ControlData
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
            },
            Images = new Dictionary<int, SharedImageModel>()
        };
    }

    private static SharedImageModel CreateTestImage(int width, int height)
    {
        var pixels = new byte[width * height];
        Array.Fill(pixels, (byte)5);
        return new SharedImageModel
        {
            Data = new SharedImageModel.ImageData
            {
                Width = width,
                Height = height,
                CompressionDictionaryWidth = 11
            },
            RawVgaImageData = pixels,
            VgaImageData = new byte[width * height * 4],
            CgaImageData = Array.Empty<byte>()
        };
    }

    private static PackageModel CreatePackageModel(params AnimationModel[] animations)
    {
        var dict = new Dictionary<string, AnimationModel>();
        foreach (var anim in animations)
        {
            dict[anim.Key] = anim;
        }

        return new PackageModel { Animations = dict };
    }

    #endregion
}
