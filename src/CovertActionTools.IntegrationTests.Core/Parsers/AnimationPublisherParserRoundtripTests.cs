using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Exporting.Publishers;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.IntegrationTests.Core.Parsers.Data;

using Opcode = CovertActionTools.Core.Models.AnimationModel.AnimationInstruction.AnimationOpcode;
using StepType = CovertActionTools.Core.Models.AnimationModel.AnimationStep.StepType;

namespace CovertActionTools.IntegrationTests.Core.Parsers;

public class AnimationPublisherParserRoundtripTests : IDisposable
{
    private readonly AnimationPublisher _publisher;
    private readonly LegacyAnimationParser _parser;
    private readonly string _tempDir;

    public AnimationPublisherParserRoundtripTests()
    {
        var compression = new LzwCompression(NullLogger<LzwCompression>.Instance);
        var decompression = new LzwDecompression(NullLogger<LzwDecompression>.Instance);
        var imageExporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, compression);
        var imageParser = new SharedImageParser(NullLogger<SharedImageParser>.Instance, decompression);
        _publisher = new AnimationPublisher(NullLogger<AnimationPublisher>.Instance, imageExporter);
        _parser = new LegacyAnimationParser(NullLogger<LegacyAnimationParser>.Instance, imageParser);
        _tempDir = Path.Combine(Path.GetTempPath(), $"AnimationPublisherRoundtrip_{Guid.NewGuid():N}");
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

    [Theory]
    [InlineData(AnimationModel.ImageFormat.Compressed)]
    [InlineData(AnimationModel.ImageFormat.Raw)]
    public void Roundtrip_ImagesAndBackground_Preserved(AnimationModel.ImageFormat imageFormat)
    {
        var animation = AnimationIntegrationTestDataGenerator.CreateSpriteAnimation();
        animation.Data.ImageFormat = imageFormat;
        animation.Data.BackgroundType = AnimationModel.BackgroundType.ClearToImage;
        animation.Images[-1] = AnimationIntegrationTestDataGenerator.CreateImage(8, 6, seed: 3);

        var result = PublishAndParse(animation);

        Assert.Equal(imageFormat, result.Data.ImageFormat);
        Assert.Equal(animation.Images[-1].RawVgaImageData, result.Images[-1].RawVgaImageData);
        Assert.Equal(animation.Images[0].RawVgaImageData, result.Images[0].RawVgaImageData);
        Assert.Equal(animation.Images[1].RawVgaImageData, result.Images[1].RawVgaImageData);
        Assert.Equal(animation.Data.ImageIdToIndex, result.Data.ImageIdToIndex);
        Assert.Equal(animation.Data.ImageIndexToUnknownData, result.Data.ImageIndexToUnknownData);
    }

    [Theory]
    [InlineData(AnimationModel.ColorMappingType.None)]
    [InlineData(AnimationModel.ColorMappingType.Embedded)]
    [InlineData(AnimationModel.ColorMappingType.Previous)]
    [InlineData(AnimationModel.ColorMappingType.Palette)]
    public void Roundtrip_HeaderVariants_Preserved(AnimationModel.ColorMappingType colorMappingType)
    {
        var animation = AnimationIntegrationTestDataGenerator.CreateSpriteAnimation();
        animation.Data.ColorMappingType = colorMappingType;
        animation.Data.PositionX = 12;
        animation.Data.PositionY = 34;
        animation.Data.FrameDelay = 4;
        if (colorMappingType == AnimationModel.ColorMappingType.Embedded)
        {
            animation.Data.ColorMapping[0] = 7;
            animation.Data.ColorMapping[5] = 0;
            animation.Data.ColorMapping[15] = 12;
            animation.Data.BorderColor = 4;
        }
        else
        {
            animation.Data.ColorMapping.Clear();
        }
        if (colorMappingType == AnimationModel.ColorMappingType.Palette)
        {
            animation.Data.PaletteData = Enumerable.Range(0, 774).Select(x => (byte)(x % 13)).ToArray();
        }

        var result = PublishAndParse(animation);

        Assert.Equal(colorMappingType, result.Data.ColorMappingType);
        Assert.Equal(12, result.Data.PositionX);
        Assert.Equal(34, result.Data.PositionY);
        Assert.Equal(4, result.Data.FrameDelay);
        Assert.Equal(animation.Data.PaletteData, result.Data.PaletteData);
        Assert.Equal(animation.Data.BorderColor, result.Data.BorderColor);
        if (colorMappingType == AnimationModel.ColorMappingType.Embedded)
        {
            Assert.Equal(7, result.Data.ColorMapping[0]);
            Assert.Equal(0, result.Data.ColorMapping[5]);
            Assert.Equal(12, result.Data.ColorMapping[15]);
        }
    }

    [Fact]
    public void Roundtrip_ControlData_PreservesInstructionsStepsAndLabels()
    {
        var animation = AnimationIntegrationTestDataGenerator.CreateSpriteAnimation();

        var result = PublishAndParse(animation);

        Assert.Equal(Signature(animation.Control), Signature(result.Control));
    }

    [Fact]
    public void Roundtrip_AllOpcodes_PreservesInstructionsStepsAndLabels()
    {
        var animation = AnimationIntegrationTestDataGenerator.CreateAllOpcodesAnimation();

        var result = PublishAndParse(animation);

        Assert.Equal(Signature(animation.Control), Signature(result.Control));
    }

    [Fact]
    public void Roundtrip_ParsedFile_PublishesIdenticalDataSection()
    {
        var animation = AnimationIntegrationTestDataGenerator.CreateAllOpcodesAnimation();
        var first = _publisher.GetLegacyFile(animation);
        File.WriteAllBytes(Path.Combine(_tempDir, "ANIM.PAN"), first);
        var parsed = RunParser()["ANIM"];

        var second = _publisher.GetLegacyFile(parsed);

        Assert.Equal(first, second);
    }

    #endregion

    #region Helpers

    private AnimationModel PublishAndParse(AnimationModel animation)
    {
        File.WriteAllBytes(Path.Combine(_tempDir, $"{animation.Key}.PAN"), _publisher.GetLegacyFile(animation));
        return RunParser()[animation.Key];
    }

    private Dictionary<string, AnimationModel> RunParser()
    {
        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);
        return model.Animations;
    }

    /// <summary>
    /// Describes control data independently of label names by resolving labels to indices
    /// </summary>
    private static string Signature(AnimationModel.ControlData control)
    {
        var instructions = control.Instructions.Select(x =>
        {
            var target = string.IsNullOrEmpty(x.Label) ? "" : $"->{control.InstructionLabels[x.Label]}";
            var step = string.IsNullOrEmpty(x.StepLabel) ? "" : $"=>{control.StepLabels[x.StepLabel]}";
            return $"{x.Opcode}{target}{step} [{string.Join(",", x.Data)}] ({string.Join(",", x.StackParameters)})";
        });
        var steps = control.Steps.Select(x =>
        {
            var target = string.IsNullOrEmpty(x.StepLabel) ? "" : $"=>{control.StepLabels[x.StepLabel]}";
            return $"{x.Type}{target} [{string.Join(",", x.Data)}]";
        });
        return string.Join("\n", instructions) + "\n---\n" + string.Join("\n", steps);
    }

    #endregion
}
