using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Exporting.Publishers;
using CovertActionTools.Core.Exporting.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Parsers.Stubs;

using Opcode = CovertActionTools.Core.Models.AnimationModel.AnimationInstruction.AnimationOpcode;
using StepType = CovertActionTools.Core.Models.AnimationModel.AnimationStep.StepType;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class AnimationPublisherTests : IDisposable
{
    private const int HeaderLength = 0x24;
    private const int IndexTableLength = 500;

    private readonly AnimationPublisher _publisher;
    private readonly string _tempDir;

    public AnimationPublisherTests()
    {
        var imageExporter = new SharedImageExporter(NullLogger<SharedImageExporter>.Instance, new StubLzwCompression());
        _publisher = new AnimationPublisher(NullLogger<AnimationPublisher>.Instance, imageExporter);
        _tempDir = Path.Combine(Path.GetTempPath(), $"AnimationPublisherTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Header

    [Fact]
    public void GetLegacyFile_EmbeddedColorBlock_WritesLegacyHeader()
    {
        var animation = CreateAnimation();
        animation.Data.BorderColor = 4;
        animation.Data.PositionX = 10;
        animation.Data.PositionY = 20;

        var bytes = _publisher.GetLegacyFile(animation);

        Assert.Equal(new byte[] { 0x50, 0x41, 0x4E, 0x49, 0x03, 0x01, 0x01, 0x00, 0x03 }, bytes.Take(9));
        Assert.Equal(new byte[] { 1, 2, 3, 4, 0, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 4 }, bytes.Skip(9).Take(16));
        Assert.Equal(new byte[] { 10, 0, 20, 0, 99, 0, 79, 0, 1, 0, 0x02, 5, 3 }, bytes.Skip(0x19).Take(13));
    }

    [Fact]
    public void GetLegacyFile_ExplicitColorMapping_WritesMappedValues()
    {
        var animation = CreateAnimation();
        animation.Data.ColorMapping[5] = 5;
        animation.Data.ColorMapping[15] = 12;

        var bytes = _publisher.GetLegacyFile(animation);

        Assert.Equal(5, bytes[0x08 + 5]);
        Assert.Equal(12, bytes[0x08 + 15]);
    }

    [Fact]
    public void GetLegacyFile_NoColorBlock_SkipsKindAndBlock()
    {
        var animation = CreateAnimation();
        animation.Data.ColorMappingType = AnimationModel.ColorMappingType.None;

        var bytes = _publisher.GetLegacyFile(animation);

        Assert.Equal(0x00, bytes[6]);
        Assert.Equal(new byte[] { 0, 0, 0, 0, 99, 0, 79, 0, 1, 0, 0x02 }, bytes.Skip(7).Take(11));
    }

    [Fact]
    public void GetLegacyFile_PreviousColorBlock_WritesKindOnly()
    {
        var animation = CreateAnimation();
        animation.Data.ColorMappingType = AnimationModel.ColorMappingType.Previous;

        var bytes = _publisher.GetLegacyFile(animation);

        Assert.Equal(new byte[] { 0x01, 0x01, 0, 0, 0, 0, 99, 0 }, bytes.Skip(6).Take(8));
    }

    [Fact]
    public void GetLegacyFile_PaletteBlock_WritesPaddedPalette()
    {
        var animation = CreateAnimation();
        animation.Data.ColorMappingType = AnimationModel.ColorMappingType.Palette;
        animation.Data.PaletteData = new byte[] { 9, 8, 7 };

        var bytes = _publisher.GetLegacyFile(animation);

        Assert.Equal(new byte[] { 0x01, 0x02, 9, 8, 7, 0 }, bytes.Skip(6).Take(6));
        Assert.Equal(99, bytes[8 + 774 + 4]);
    }

    [Fact]
    public void GetLegacyFile_RawImageFormat_WritesFormatByte()
    {
        var animation = CreateAnimation();
        animation.Data.ImageFormat = AnimationModel.ImageFormat.Raw;

        var bytes = _publisher.GetLegacyFile(animation);

        Assert.Equal(0x00, bytes[5]);
    }

    #endregion

    #region Images

    [Fact]
    public void GetLegacyFile_RawImages_WritesPixelsAligned()
    {
        var animation = CreateAnimation();
        animation.Data.ImageFormat = AnimationModel.ImageFormat.Raw;
        animation.Data.ImageIdToIndex[0] = 0;
        animation.Data.ImageIdToIndex[2] = 1;
        animation.Data.ImageIndexToUnknownData[0] = 5;
        animation.Data.ImageIndexToUnknownData[1] = 6;
        animation.Images[0] = CreateImage(3, 1, new byte[] { 1, 2, 3 });
        animation.Images[1] = CreateImage(1, 1, new byte[] { 9 });

        var bytes = _publisher.GetLegacyFile(animation);

        var table = HeaderLength + 2;
        Assert.Equal(new byte[] { 5, 0, 0, 0, 6, 0 }, bytes.Skip(table).Take(6));
        var images = table + IndexTableLength;
        Assert.Equal(new byte[] { 0, 0, 3, 0, 1, 0, 1, 2, 3, 0 }, bytes.Skip(images).Take(10));
        Assert.Equal(new byte[] { 0, 0, 1, 0, 1, 0, 9, 0 }, bytes.Skip(images + 10).Take(8));
    }

    [Fact]
    public void GetLegacyFile_RawBackground_WrittenBeforeIndexTable()
    {
        var animation = CreateAnimation();
        animation.Data.ImageFormat = AnimationModel.ImageFormat.Raw;
        animation.Data.BackgroundType = AnimationModel.BackgroundType.ClearToImage;
        animation.Images[-1] = CreateImage(2, 1, new byte[] { 7, 8 });

        var bytes = _publisher.GetLegacyFile(animation);

        Assert.Equal(new byte[] { 0x01, 0, 0, 2, 0, 1, 0, 7, 8, 0, 0 }, bytes.Skip(0x23).Take(11));
    }

    #endregion

    #region Data section

    [Fact]
    public void GetLegacyFile_DataSection_IsParagraphAlignedWithSizePrefix()
    {
        var animation = CreateAnimation();

        var bytes = _publisher.GetLegacyFile(animation);

        var section = DataSection(bytes);
        Assert.Equal(new byte[] { 1, 0 }, bytes.Skip(HeaderLength + 2 + IndexTableLength).Take(2));
        Assert.Equal(16, section.Length);
        Assert.Equal(new byte[] { 0x05, 0x00, 0x01, 0x00, 0x02, 0x14 }, section.Take(6));
        Assert.All(section.Skip(6), x => Assert.Equal(0, x));
    }

    [Fact]
    public void GetLegacyFile_FoldedSetupSprite_WritesPointerAndParameters()
    {
        var animation = CreateAnimation();
        animation.Control.Instructions = new List<AnimationModel.AnimationInstruction>
        {
            new() { Opcode = Opcode.SetupSprite, StepLabel = "S", StackParameters = new short[] { 1, -1, 2, 3, 255, 0 } },
            new() { Opcode = Opcode.End }
        };
        animation.Control.Steps = new List<AnimationModel.AnimationStep>
        {
            new() { Type = StepType.DrawFrame, Data = new byte[] { 4 } },
            new() { Type = StepType.Pause }
        };
        animation.Control.StepLabels = new Dictionary<string, int> { ["S"] = 0 };

        var section = DataSection(_publisher.GetLegacyFile(animation));

        Assert.Equal(new byte[]
        {
            0x05, 0x00, 30, 0x00,
            0x05, 0x00, 1, 0, 0x05, 0x00, 0xFF, 0xFF, 0x05, 0x00, 2, 0, 0x05, 0x00, 3, 0, 0x05, 0x00, 0xFF, 0, 0x05, 0x00, 0, 0,
            0x00, 0x14,
            0x00, 4, 0x09
        }, section.Take(33));
    }

    [Fact]
    public void GetLegacyFile_BareSetupSpriteAndLabelledPush_WriteRawOpcodes()
    {
        var animation = CreateAnimation();
        animation.Control.Instructions = new List<AnimationModel.AnimationInstruction>
        {
            new() { Opcode = Opcode.PushToStack, StepLabel = "S" },
            new() { Opcode = Opcode.PushRegisterToStack, Data = new byte[] { 2, 0 } },
            new() { Opcode = Opcode.SetupSprite },
            new() { Opcode = Opcode.Comment, Comment = "ignored" },
            new() { Opcode = Opcode.Call, Label = "SUB" },
            new() { Opcode = Opcode.End },
            new() { Opcode = Opcode.CompareEqual },
            new() { Opcode = Opcode.Return }
        };
        animation.Control.InstructionLabels = new Dictionary<string, int> { ["SUB"] = 6 };
        animation.Control.Steps = new List<AnimationModel.AnimationStep> { new() { Type = StepType.Stop } };
        animation.Control.StepLabels = new Dictionary<string, int> { ["S"] = 0 };

        var section = DataSection(_publisher.GetLegacyFile(animation));

        Assert.Equal(new byte[]
        {
            0x05, 0x00, 15, 0x00,
            0x05, 0x01, 2, 0,
            0x00,
            0x17, 13, 0x00,
            0x14,
            0x08,
            0x16,
            0x0A
        }, section.Take(16));
    }

    [Fact]
    public void GetLegacyFile_SetupSpriteWithLabelButWrongParameterCount_Throws()
    {
        var animation = CreateAnimation();
        animation.Control.Instructions = new List<AnimationModel.AnimationInstruction>
        {
            new() { Opcode = Opcode.SetupSprite, StepLabel = "S", StackParameters = new short[] { 1 } },
            new() { Opcode = Opcode.End }
        };
        animation.Control.StepLabels = new Dictionary<string, int> { ["S"] = 0 };

        Assert.ThrowsAny<Exception>(() => _publisher.GetLegacyFile(animation));
    }

    #endregion

    #region Export flow

    [Fact]
    public void Export_WritesPanFileForIncludedAnimations()
    {
        var animation = CreateAnimation();
        var model = new PackageModel { Animations = new Dictionary<string, AnimationModel> { ["ANIM"] = animation } };
        model.Index.AnimationIncluded.Add("ANIM");

        _publisher.Start(_tempDir, model);
        while (!_publisher.RunStep()) { }

        Assert.True(File.Exists(Path.Combine(_tempDir, "ANIM.PAN")));
    }

    #endregion

    #region Helpers

    private static AnimationModel CreateAnimation()
    {
        return new AnimationModel
        {
            Key = "ANIM",
            Data = new AnimationModel.GlobalData
            {
                BoundingWidth = 99,
                BoundingHeight = 79,
                FrameDelay = 1,
                BackgroundType = AnimationModel.BackgroundType.ClearToColor,
                ClearColor = 5,
                Unknown2 = 3
            },
            Control = new AnimationModel.ControlData
            {
                Instructions = new List<AnimationModel.AnimationInstruction>
                {
                    new() { Opcode = Opcode.WaitForFrames, StackParameters = new short[] { 1 } },
                    new() { Opcode = Opcode.End }
                }
            }
        };
    }

    private static SharedImageModel CreateImage(int width, int height, byte[] pixels)
    {
        return new SharedImageModel
        {
            Data = new SharedImageModel.ImageData { Width = width, Height = height, CompressionDictionaryWidth = 11 },
            RawVgaImageData = pixels,
            VgaImageData = new byte[width * height * 4]
        };
    }

    private static byte[] DataSection(byte[] file)
    {
        var start = HeaderLength + 2 + IndexTableLength + 2;
        return file.Skip(start).ToArray();
    }

    #endregion
}
