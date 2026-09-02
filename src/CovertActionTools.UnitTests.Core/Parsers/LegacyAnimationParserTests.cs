using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using CovertActionTools.UnitTests.Core.Parsers.Data;
using CovertActionTools.UnitTests.Core.Parsers.Stubs;

using Opcode = CovertActionTools.Core.Models.AnimationModel.AnimationInstruction.AnimationOpcode;
using StepType = CovertActionTools.Core.Models.AnimationModel.AnimationStep.StepType;

namespace CovertActionTools.UnitTests.Core.Parsers;

public class LegacyAnimationParserTests : IDisposable
{
    private readonly LegacyAnimationParser _parser;
    private readonly StubLzwDecompression _stubDecompression;
    private readonly string _tempDir;

    public LegacyAnimationParserTests()
    {
        _stubDecompression = new StubLzwDecompression();
        var imageParser = new SharedImageParser(NullLogger<SharedImageParser>.Instance, _stubDecompression);
        _parser = new LegacyAnimationParser(NullLogger<LegacyAnimationParser>.Instance, imageParser);
        _tempDir = Path.Combine(Path.GetTempPath(), $"LegacyAnimationParserTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region Header parsing

    [Fact]
    public void Parse_MinimalPanFile_ReadsBoundingDimensions()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile(boundingWidth: 159, boundingHeight: 99);
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(159, animations["TEST"].Data.BoundingWidth);
        Assert.Equal(99, animations["TEST"].Data.BoundingHeight);
    }

    [Fact]
    public void Parse_MinimalPanFile_ReadsFrameDelay()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile(frameDelay: 3);
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(3, animations["TEST"].Data.FrameDelay);
    }

    [Fact]
    public void Parse_MinimalPanFile_ReadsBackgroundType()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(AnimationModel.BackgroundType.ClearToColor, animations["TEST"].Data.BackgroundType);
    }

    [Fact]
    public void Parse_MinimalPanFile_ReadsClearColor()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile(clearColor: 7);
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(7, animations["TEST"].Data.ClearColor);
    }

    [Fact]
    public void Parse_MinimalPanFile_ReadsColorMapping()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(16, animations["TEST"].Data.ColorMapping.Count);
        Assert.Equal(3, animations["TEST"].Data.ColorMapping[0]);
        for (byte i = 1; i <= 15; i++)
        {
            Assert.Equal(i, animations["TEST"].Data.ColorMapping[i]);
        }
    }

    [Fact]
    public void Parse_MinimalPanFile_DefaultsToCompressedEmbeddedHeader()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(AnimationModel.ImageFormat.Compressed, animations["TEST"].Data.ImageFormat);
        Assert.Equal(AnimationModel.ColorMappingType.Embedded, animations["TEST"].Data.ColorMappingType);
        Assert.Equal(3, animations["TEST"].Data.ColorMapping[0]);
        Assert.Equal(0, animations["TEST"].Data.BorderColor);
        Assert.Equal(0, animations["TEST"].Data.PositionX);
        Assert.Equal(0, animations["TEST"].Data.PositionY);
    }

    [Fact]
    public void Parse_UnsupportedVersion_Throws()
    {
        var data = AnimationTestDataGenerator.BuildPanFile(version: 0x04);
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        Assert.ThrowsAny<Exception>(() => RunParser());
    }

    [Fact]
    public void Parse_EmbeddedColorBlock_ReadsEntryZeroAndBorderBytes()
    {
        var block = AnimationTestDataGenerator.IdentityColorBlock();
        block[0] = 0x09;
        block[5] = 0x00;
        block[16] = 0x04;
        var data = AnimationTestDataGenerator.BuildPanFile(colorBlock: block);
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(9, animations["TEST"].Data.ColorMapping[0]);
        Assert.Equal(0, animations["TEST"].Data.ColorMapping[5]);
        Assert.Equal(4, animations["TEST"].Data.BorderColor);
    }

    [Fact]
    public void Parse_NoColorBlock_ReadsRestOfHeader()
    {
        var data = AnimationTestDataGenerator.BuildPanFile(hasColorBlock: false, boundingWidth: 63, frameDelay: 2);
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(AnimationModel.ColorMappingType.None, animations["TEST"].Data.ColorMappingType);
        Assert.Empty(animations["TEST"].Data.ColorMapping);
        Assert.Equal(63, animations["TEST"].Data.BoundingWidth);
        Assert.Equal(2, animations["TEST"].Data.FrameDelay);
    }

    [Fact]
    public void Parse_PreviousColorBlock_HasNoBlockBytes()
    {
        var data = AnimationTestDataGenerator.BuildPanFile(colorBlockKind: 0x01, boundingWidth: 63);
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(AnimationModel.ColorMappingType.Previous, animations["TEST"].Data.ColorMappingType);
        Assert.Equal(63, animations["TEST"].Data.BoundingWidth);
    }

    [Fact]
    public void Parse_PaletteBlock_ReadsAllBytes()
    {
        var palette = Enumerable.Range(0, 774).Select(x => (byte)(x % 251)).ToArray();
        var data = AnimationTestDataGenerator.BuildPanFile(colorBlockKind: 0x02, colorBlock: palette, boundingWidth: 63);
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(AnimationModel.ColorMappingType.Palette, animations["TEST"].Data.ColorMappingType);
        Assert.Equal(palette, animations["TEST"].Data.PaletteData);
        Assert.Equal(63, animations["TEST"].Data.BoundingWidth);
    }

    [Fact]
    public void Parse_ReadsPosition()
    {
        var data = AnimationTestDataGenerator.BuildPanFile(positionX: 10, positionY: 20);
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(10, animations["TEST"].Data.PositionX);
        Assert.Equal(20, animations["TEST"].Data.PositionY);
    }

    #endregion

    #region Images

    [Fact]
    public void Parse_MinimalPanFile_HasNoImages()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Empty(animations["TEST"].Images);
    }

    [Fact]
    public void Parse_MinimalPanFile_HasEmptyImageIdMapping()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Empty(animations["TEST"].Data.ImageIdToIndex);
    }

    [Fact]
    public void Parse_RawImages_ReadsPixels()
    {
        var pixels = new byte[] { 1, 2, 3, 4, 5, 6 };
        var data = AnimationTestDataGenerator.BuildPanFile(
            imageFormat: 0x00,
            images: new[] { AnimationTestDataGenerator.RawImage(3, 2, pixels), AnimationTestDataGenerator.RawImage(1, 1, new byte[] { 9 }) },
            imageIds: new[] { 0, 4 });
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        var animation = animations["TEST"];
        Assert.Equal(AnimationModel.ImageFormat.Raw, animation.Data.ImageFormat);
        Assert.Equal(new Dictionary<int, int> { [0] = 0, [4] = 1 }, animation.Data.ImageIdToIndex);
        Assert.Equal(3, animation.Images[0].Data.Width);
        Assert.Equal(2, animation.Images[0].Data.Height);
        Assert.Equal(pixels, animation.Images[0].RawVgaImageData);
        Assert.Equal(new byte[] { 9 }, animation.Images[1].RawVgaImageData);
    }

    [Fact]
    public void Parse_RawBackgroundImage_IsImageMinusOne()
    {
        var data = AnimationTestDataGenerator.BuildPanFile(
            imageFormat: 0x00,
            backgroundType: 0x01,
            backgroundImage: AnimationTestDataGenerator.RawImage(2, 1, new byte[] { 7, 8 }));
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(new byte[] { 7, 8 }, animations["TEST"].Images[-1].RawVgaImageData);
        Assert.Equal(AnimationModel.BackgroundType.ClearToImage, animations["TEST"].Data.BackgroundType);
    }

    [Fact]
    public void Parse_CompressedImages_UsesDecompressor()
    {
        _stubDecompression.SetResult(new byte[] { 1, 2, 3, 4 });
        var data = AnimationTestDataGenerator.BuildPanFile(
            images: new[] { AnimationTestDataGenerator.StubCompressedImage(2, 2) });
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        Assert.Equal(2, _stubDecompression.LastWidth);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, animations["TEST"].Images[0].RawVgaImageData);
        Assert.Single(animations["TEST"].Control.Instructions.Where(x => x.Opcode == Opcode.End));
    }

    #endregion

    #region Instructions

    [Fact]
    public void Parse_MinimalPanFile_FoldsPushIntoWait()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        var instructions = animations["TEST"].Control.Instructions;
        Assert.Equal(2, instructions.Count);
        Assert.Equal(Opcode.WaitForFrames, instructions[0].Opcode);
        Assert.Equal(new short[] { 1 }, instructions[0].StackParameters);
        Assert.Equal(Opcode.End, instructions[1].Opcode);
    }

    [Fact]
    public void Parse_SetupSprite_FoldsPointerAndParameters()
    {
        var section = Bytes(
            Push(30), Push(1), Push(-1), Push(2), Push(3), Push(255), Push(0), new byte[] { 0x00 },
            new byte[] { 0x14 },
            new byte[] { 0x00, 0x05, 0x09 });
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", AnimationTestDataGenerator.BuildPanFile(dataSection: section));

        var animations = RunParser();

        var control = animations["TEST"].Control;
        var setup = control.Instructions[0];
        Assert.Equal(Opcode.SetupSprite, setup.Opcode);
        Assert.Equal("START_1", setup.StepLabel);
        Assert.Equal(new short[] { 1, -1, 2, 3, 255, 0 }, setup.StackParameters);
        Assert.Equal(0, control.StepLabels["START_1"]);
        Assert.Equal(StepType.DrawFrame, control.Steps[0].Type);
        Assert.Equal(new byte[] { 5 }, control.Steps[0].Data);
        Assert.Equal("Sprites start: 1", control.Steps[0].Comment);
        Assert.Equal(StepType.Pause, control.Steps[1].Type);
    }

    [Fact]
    public void Parse_Jump_CreatesLabelAndDropsUnreachableBytes()
    {
        var section = Bytes(new byte[] { 0x13, 0x04, 0x00 }, new byte[] { 0x14 }, new byte[] { 0x14 });
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", AnimationTestDataGenerator.BuildPanFile(dataSection: section));

        var animations = RunParser();

        var control = animations["TEST"].Control;
        Assert.Equal(2, control.Instructions.Count);
        Assert.Equal(Opcode.Jump, control.Instructions[0].Opcode);
        Assert.Equal("LABEL_1", control.Instructions[0].Label);
        Assert.Equal(1, control.InstructionLabels["LABEL_1"]);
    }

    [Fact]
    public void Parse_CallAndReturn_ReachInstructionsAfterEnd()
    {
        var section = Bytes(new byte[] { 0x17, 0x04, 0x00 }, new byte[] { 0x14 }, new byte[] { 0x16 });
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", AnimationTestDataGenerator.BuildPanFile(dataSection: section));

        var animations = RunParser();

        var control = animations["TEST"].Control;
        Assert.Equal(new[] { Opcode.Call, Opcode.End, Opcode.Return }, control.Instructions.Select(x => x.Opcode));
        Assert.Equal(2, control.InstructionLabels[control.Instructions[0].Label]);
    }

    [Fact]
    public void Parse_RegisterPushBeforeSetupSprite_KeepsPushesAndLabelsPointer()
    {
        var section = Bytes(
            Push(30), new byte[] { 0x05, 0x01, 0x00, 0x00 }, Push(-1), Push(2), Push(3), Push(255), Push(0), new byte[] { 0x00 },
            new byte[] { 0x14 },
            new byte[] { 0x00, 0x05, 0x09 });
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", AnimationTestDataGenerator.BuildPanFile(dataSection: section));

        var animations = RunParser();

        var control = animations["TEST"].Control;
        Assert.Equal(Opcode.PushToStack, control.Instructions[0].Opcode);
        Assert.Equal("START_1", control.Instructions[0].StepLabel);
        Assert.Empty(control.Instructions[0].Data);
        Assert.Equal(Opcode.PushRegisterToStack, control.Instructions[1].Opcode);
        Assert.Equal(Opcode.SetupSprite, control.Instructions[7].Opcode);
        Assert.Empty(control.Instructions[7].StackParameters);
        Assert.Empty(control.Instructions[7].StepLabel);
        Assert.Equal(0, control.StepLabels["START_1"]);
    }

    [Fact]
    public void Parse_JumpIntoPushGroup_PreventsFolding()
    {
        var section = Bytes(
            new byte[] { 0x12, 0x07, 0x00 },
            Push(1), Push(2), new byte[] { 0x0E },
            new byte[] { 0x14 });
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", AnimationTestDataGenerator.BuildPanFile(dataSection: section));

        var animations = RunParser();

        var control = animations["TEST"].Control;
        Assert.Equal(new[] { Opcode.ConditionalJump, Opcode.PushToStack, Opcode.Add, Opcode.End }, control.Instructions.Select(x => x.Opcode));
        Assert.Equal(new short[] { 2 }, control.Instructions[2].StackParameters);
        Assert.Equal(2, control.InstructionLabels["LABEL_1"]);
    }

    [Fact]
    public void Parse_NonStandardRegisterPushByte_IsRegisterPush()
    {
        var section = Bytes(new byte[] { 0x05, 0x4B, 0x03, 0x00 }, new byte[] { 0x06, 0xFF, 0xFF }, new byte[] { 0x14 });
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", AnimationTestDataGenerator.BuildPanFile(dataSection: section));

        var animations = RunParser();

        var control = animations["TEST"].Control;
        Assert.Equal(Opcode.PushRegisterToStack, control.Instructions[0].Opcode);
        Assert.Equal(new byte[] { 3, 0 }, control.Instructions[0].Data);
        Assert.Equal(Opcode.PopStackToRegister, control.Instructions[1].Opcode);
    }

    [Fact]
    public void Parse_UnknownInstruction_Throws()
    {
        var section = new byte[] { 0x18, 0x14 };
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", AnimationTestDataGenerator.BuildPanFile(dataSection: section));

        Assert.ThrowsAny<Exception>(() => RunParser());
    }

    [Fact]
    public void Parse_StepsAfterEnd_IgnoresUnreferencedBytes()
    {
        var section = Bytes(
            Push(30), Push(1), Push(-1), Push(0), Push(0), Push(255), Push(0), new byte[] { 0x00 },
            new byte[] { 0x14 },
            new byte[] { 0x05, 0x02, 0x00 },
            new byte[] { 0x00, 0x01 },
            new byte[] { 0x06, 0x21, 0x00 },
            new byte[] { 0x0A },
            new byte[] { 0xFE, 0xFD });
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", AnimationTestDataGenerator.BuildPanFile(dataSection: section));

        var animations = RunParser();

        var control = animations["TEST"].Control;
        Assert.Equal(new[] { StepType.PushCounter, StepType.DrawFrame, StepType.JumpIfCounter, StepType.Stop }, control.Steps.Select(x => x.Type));
        Assert.Equal("DATA_2", control.Steps[2].StepLabel);
        Assert.Equal(1, control.StepLabels["DATA_2"]);
    }

    [Fact]
    public void Parse_DeclaredDataLength_LimitsWhatIsRead()
    {
        var section = Bytes(new byte[] { 0x14 }, new byte[15], new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", AnimationTestDataGenerator.BuildPanFile(dataSection: section, declaredParagraphs: 1));

        var animations = RunParser();

        Assert.Single(animations["TEST"].Control.Instructions);
    }

    [Fact]
    public void Parse_MinimalPanFile_EndsWithEndInstruction()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST", data);

        var animations = RunParser();

        var instructions = animations["TEST"].Control.Instructions;
        var lastInstruction = instructions[instructions.Count - 1];
        Assert.Equal(Opcode.End, lastInstruction.Opcode);
    }

    #endregion

    #region Metadata

    [Fact]
    public void Parse_SetsKey()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "MYANIM", data);

        var animations = RunParser();

        Assert.Equal("MYANIM", animations["MYANIM"].Key);
    }

    [Fact]
    public void Parse_SetsMetadata()
    {
        var data = AnimationTestDataGenerator.BuildMinimalPanFile();
        AnimationTestDataGenerator.WritePanFile(_tempDir, "MYANIM", data);

        var animations = RunParser();

        Assert.Equal("MYANIM", animations["MYANIM"].Metadata.Name);
        Assert.Equal("Legacy import", animations["MYANIM"].Metadata.Comment);
    }

    #endregion

    #region Multiple files

    [Fact]
    public void Parse_MultiplePanFiles_ParsesAll()
    {
        AnimationTestDataGenerator.WritePanFile(_tempDir, "ANIM1",
            AnimationTestDataGenerator.BuildMinimalPanFile());
        AnimationTestDataGenerator.WritePanFile(_tempDir, "ANIM2",
            AnimationTestDataGenerator.BuildMinimalPanFile());

        var animations = RunParser();

        Assert.Equal(2, animations.Count);
        Assert.True(animations.ContainsKey("ANIM1"));
        Assert.True(animations.ContainsKey("ANIM2"));
    }

    #endregion

    #region SetResult

    [Fact]
    public void SetResult_PopulatesAnimationsOnPackageModel()
    {
        AnimationTestDataGenerator.WritePanFile(_tempDir, "TEST",
            AnimationTestDataGenerator.BuildMinimalPanFile());

        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);

        Assert.NotEmpty(model.Animations);
        Assert.True(model.Animations.ContainsKey("TEST"));
    }

    #endregion

    #region Helpers

    private Dictionary<string, AnimationModel> RunParser()
    {
        var model = new PackageModel();
        _parser.Start(_tempDir);
        while (!_parser.RunStep()) { }
        _parser.SetResult(model);
        return model.Animations;
    }

    private static byte[] Push(short value)
    {
        return new[] { (byte)0x05, (byte)0x00, (byte)(value & 0xFF), (byte)((value >> 8) & 0xFF) };
    }

    private static byte[] Bytes(params byte[][] parts)
    {
        return parts.SelectMany(x => x).ToArray();
    }

    #endregion
}
