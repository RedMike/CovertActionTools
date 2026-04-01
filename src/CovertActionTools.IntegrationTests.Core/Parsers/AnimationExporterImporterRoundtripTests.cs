using System;
using System.Collections.Generic;
using System.IO;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.IntegrationTests.Core.Parsers.Data;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class AnimationExporterImporterRoundtripTests : IDisposable
{
    private readonly AnimationExporter _exporter;
    private readonly AnimationImporter _importer;
    private readonly string _tempDir;

    public AnimationExporterImporterRoundtripTests()
    {
        var compression = new LzwCompression(NullLogger<LzwCompression>.Instance);
        var imageExporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, compression);
        var imageImporter = new SharedImageImporter(NullLogger<SharedImageImporter>.Instance);
        _exporter = new AnimationExporter(NullLogger<AnimationExporter>.Instance, imageExporter);
        _importer = new AnimationImporter(NullLogger<AnimationImporter>.Instance, imageImporter);
        _tempDir = Path.Combine(Path.GetTempPath(), $"AnimationRoundtrip_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Roundtrip tests

    [Fact]
    public void Roundtrip_SingleAnimationNoImages_MetadataPreserved()
    {
        var original = new Dictionary<string, AnimationModel>
        {
            ["anim1"] = CreateSampleAnimation("anim1")
        };

        var result = ExportAndImport(original);

        Assert.Single(result);
        var anim = result["anim1"];
        Assert.Equal("anim1", anim.Key);
        Assert.Equal("Test Animation anim1", anim.Metadata.Name);
        Assert.Equal("Test animation data", anim.Metadata.Comment);
    }

    [Fact]
    public void Roundtrip_SingleAnimation_GlobalDataPreserved()
    {
        var original = new Dictionary<string, AnimationModel>
        {
            ["anim1"] = CreateSampleAnimation("anim1")
        };

        var result = ExportAndImport(original);

        var anim = result["anim1"];
        Assert.Equal(99, anim.Data.BoundingWidth);
        Assert.Equal(79, anim.Data.BoundingHeight);
        Assert.Equal(1, anim.Data.GlobalFrameSkip);
        Assert.Equal(AnimationModel.BackgroundType.ClearToColor, anim.Data.BackgroundType);
        Assert.Equal(5, anim.Data.ClearColor);
        Assert.Equal(3, anim.Data.Unknown2);
    }

    [Fact]
    public void Roundtrip_SingleAnimation_ControlDataPreserved()
    {
        var original = new Dictionary<string, AnimationModel>
        {
            ["anim1"] = CreateSampleAnimation("anim1")
        };

        var result = ExportAndImport(original);

        var anim = result["anim1"];
        Assert.NotEmpty(anim.Control.Instructions);
        Assert.NotEmpty(anim.Control.Steps);

        // Verify the End instruction roundtrips
        Assert.Equal(
            AnimationModel.AnimationInstruction.AnimationOpcode.End,
            anim.Control.Instructions[anim.Control.Instructions.Count - 1].Opcode);

        // Verify the Stop step roundtrips
        Assert.Equal(
            AnimationModel.AnimationStep.StepType.Stop,
            anim.Control.Steps[anim.Control.Steps.Count - 1].Type);
    }

    [Fact]
    public void Roundtrip_AnimationWithImages_ImageDataPreserved()
    {
        var anim = CreateSampleAnimation("anim1");
        var pixels0 = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4, seed: 10);
        anim.Images[0] = CreateTestImage(4, 4, pixels0);
        var pixels1 = SharedImageTestDataGenerator.GenerateVariedPixels(4, 4, seed: 20);
        anim.Images[1] = CreateTestImage(4, 4, pixels1);

        var original = new Dictionary<string, AnimationModel> { ["anim1"] = anim };

        var result = ExportAndImport(original);

        var resultAnim = result["anim1"];
        Assert.Equal(2, resultAnim.Images.Count);
        Assert.True(resultAnim.Images.ContainsKey(0));
        Assert.True(resultAnim.Images.ContainsKey(1));

        Assert.Equal(4, resultAnim.Images[0].Data.Width);
        Assert.Equal(4, resultAnim.Images[0].Data.Height);
        Assert.Equal(pixels0, resultAnim.Images[0].RawVgaImageData);
        Assert.Equal(pixels1, resultAnim.Images[1].RawVgaImageData);
    }

    [Fact]
    public void Roundtrip_MultipleAnimations_AllPreserved()
    {
        var original = new Dictionary<string, AnimationModel>
        {
            ["animA"] = CreateSampleAnimation("animA"),
            ["animB"] = CreateSampleAnimation("animB")
        };
        original["animA"].Images[0] = CreateTestImage(4, 4);
        original["animB"].Images[0] = CreateTestImage(4, 4);

        var result = ExportAndImport(original);

        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("animA"));
        Assert.True(result.ContainsKey("animB"));
        Assert.Equal("Test Animation animA", result["animA"].Metadata.Name);
        Assert.Equal("Test Animation animB", result["animB"].Metadata.Name);
        Assert.Single(result["animA"].Images);
        Assert.Single(result["animB"].Images);
    }

    [Fact]
    public void Roundtrip_AnimationWithLabels_LabelsPreserved()
    {
        var anim = CreateSampleAnimation("anim1");
        anim.Control = new AnimationModel.ControlData
        {
            Instructions = new List<AnimationModel.AnimationInstruction>
            {
                new AnimationModel.AnimationInstruction
                {
                    Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.WaitForFrames,
                    StackParameters = new short[] { 5 }
                },
                new AnimationModel.AnimationInstruction
                {
                    Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.Jump,
                    Label = "loop_start"
                },
                new AnimationModel.AnimationInstruction
                {
                    Opcode = AnimationModel.AnimationInstruction.AnimationOpcode.End
                }
            },
            InstructionLabels = new Dictionary<string, int>
            {
                ["loop_start"] = 0
            },
            Steps = new List<AnimationModel.AnimationStep>
            {
                new AnimationModel.AnimationStep
                {
                    Type = AnimationModel.AnimationStep.StepType.DrawFrame,
                    Data = new byte[] { 0 }
                },
                new AnimationModel.AnimationStep
                {
                    Type = AnimationModel.AnimationStep.StepType.Stop
                }
            },
            StepLabels = new Dictionary<string, int>
            {
                ["step_start"] = 0
            }
        };

        var original = new Dictionary<string, AnimationModel> { ["anim1"] = anim };
        var result = ExportAndImport(original);

        var resultAnim = result["anim1"];
        Assert.Contains("loop_start", resultAnim.Control.InstructionLabels.Keys);
        Assert.Equal(0, resultAnim.Control.InstructionLabels["loop_start"]);
        Assert.Contains("step_start", resultAnim.Control.StepLabels.Keys);
        Assert.Equal(0, resultAnim.Control.StepLabels["step_start"]);
    }

    #endregion

    #region Helpers

    private Dictionary<string, AnimationModel> ExportAndImport(Dictionary<string, AnimationModel> animations)
    {
        var exportModel = new PackageModel { Animations = animations };
        _exporter.Start(_tempDir, exportModel);
        while (!_exporter.RunStep()) { }

        _importer.Start(_tempDir);
        while (!_importer.RunStep()) { }

        var importModel = new PackageModel();
        _importer.SetResult(importModel);
        return importModel.Animations;
    }

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
                ClearColor = 5,
                Unknown2 = 3,
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

    private static SharedImageModel CreateTestImage(int width, int height, byte[]? pixels = null)
    {
        if (pixels == null)
        {
            pixels = new byte[width * height];
            Array.Fill(pixels, (byte)5);
        }

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

    #endregion
}
