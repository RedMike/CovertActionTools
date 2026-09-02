using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CovertActionTools.Core.Models;

using Opcode = CovertActionTools.Core.Models.AnimationModel.AnimationInstruction.AnimationOpcode;
using StepType = CovertActionTools.Core.Models.AnimationModel.AnimationStep.StepType;

namespace CovertActionTools.IntegrationTests.Core.Parsers.Data;

public static class AnimationIntegrationTestDataGenerator
{
    /// <summary>
    /// Builds a minimal valid PAN file with BackgroundType.ClearToColor, zero images,
    /// and a minimal data section. Uses no image compression since there are no images.
    /// </summary>
    public static byte[] BuildMinimalPanFile(
        ushort boundingWidth = 99,
        ushort boundingHeight = 79,
        ushort frameDelay = 1,
        byte clearColor = 0,
        byte unknown2 = 0)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // 4-byte prefix "PANI"
        writer.Write((byte)0x50);
        writer.Write((byte)0x41);
        writer.Write((byte)0x4E);
        writer.Write((byte)0x49);

        // version, compressed images, colour block present, embedded kind
        writer.Write((byte)0x03);
        writer.Write((byte)0x01);
        writer.Write((byte)0x01);
        writer.Write((byte)0x00);

        // colour block: ignored byte, identity colours 1-15, border colour
        writer.Write((byte)0x03);
        for (byte i = 1; i <= 15; i++)
        {
            writer.Write(i);
        }
        writer.Write((byte)0x00);

        // position, bounding width/height, frameDelay
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write(boundingWidth);
        writer.Write(boundingHeight);
        writer.Write(frameDelay);

        // backgroundType = ClearToColor (0x02)
        writer.Write((byte)0x02);

        // ClearToColor bytes
        writer.Write(clearColor);
        writer.Write(unknown2);

        // 500-byte index table (250 ushort pairs, all zero = no images)
        for (var i = 0; i < 250; i++)
        {
            writer.Write((ushort)0);
        }

        // Data section: 1 * 16 = 16 bytes
        writer.Write((ushort)1);

        // PushToStack + WaitForFrames + End
        writer.Write((byte)0x05); // PushToStack
        writer.Write((byte)0x00);
        writer.Write((short)1);
        writer.Write((byte)0x02); // WaitForFrames
        writer.Write((byte)0x14); // End

        // Pad remaining bytes
        var bytesWritten = 6;
        writer.Write(new byte[16 - bytesWritten]);

        return ms.ToArray();
    }

    /// <summary>
    /// Writes a PAN file to the given directory.
    /// </summary>
    public static void WritePanFile(string directory, string key, byte[] data)
    {
        File.WriteAllBytes(Path.Combine(directory, $"{key}.PAN"), data);
    }

    public static SharedImageModel CreateImage(int width, int height, int seed)
    {
        var pixels = SharedImageTestDataGenerator.GenerateVariedPixels(width, height, seed);
        return new SharedImageModel
        {
            Data = new SharedImageModel.ImageData { Width = width, Height = height, CompressionDictionaryWidth = 11 },
            RawVgaImageData = pixels,
            VgaImageData = new byte[width * height * 4],
            CgaImageData = Array.Empty<byte>()
        };
    }

    /// <summary>
    /// Two images (IDs 0 and 3), two sprites with counter loops, a stamp and a removal.
    /// </summary>
    public static AnimationModel CreateSpriteAnimation()
    {
        var animation = CreateBaseAnimation();
        animation.Control.Instructions = new List<AnimationModel.AnimationInstruction>
        {
            Setup("START_1", 1, -1, 2, 3, 255, 0),
            Setup("START_2", 2, 1, 1, 1, 255, 1),
            Instruction(Opcode.WaitForFrames, 5),
            Instruction(Opcode.StampSprite, 1),
            Instruction(Opcode.WaitForFrames, 1),
            Instruction(Opcode.RemoveSprite, 2),
            Instruction(Opcode.End)
        };
        animation.Control.Steps = new List<AnimationModel.AnimationStep>
        {
            StepValue(StepType.PushCounter, 3),
            DrawFrame(0),
            Move(StepType.MoveRelative, 1, 0),
            JumpIfCounter("DATA_3"),
            DrawFrame(3),
            Step(StepType.Pause),
            DrawFrame(3),
            Move(StepType.MoveAbsolute, 4, 0),
            Step(StepType.Loop)
        };
        animation.Control.StepLabels = new Dictionary<string, int> { ["START_1"] = 0, ["DATA_3"] = 1, ["START_2"] = 6 };
        return animation;
    }

    /// <summary>
    /// Every instruction opcode and step type at least once, in the form the parser produces.
    /// </summary>
    public static AnimationModel CreateAllOpcodesAnimation()
    {
        var animation = CreateBaseAnimation();
        animation.Control.Instructions = new List<AnimationModel.AnimationInstruction>
        {
            Push(7),                                                     // 0
            new() { Opcode = Opcode.PushRegisterToStack, Data = Bytes(0) },  // 1
            Instruction(Opcode.Add),                                     // 2
            new() { Opcode = Opcode.PopStackToRegister, Data = Bytes(1) },   // 3
            Push(1),                                                     // 4
            Instruction(Opcode.CompareEqual, 1),                         // 5
            new() { Opcode = Opcode.ConditionalJump, Label = "SKIP" },   // 6
            Instruction(Opcode.TriggerAudio, 4),                         // 7
            Push(2),                                                     // 8 SKIP
            Instruction(Opcode.PushCopyOfStackValue),                    // 9
            Instruction(Opcode.Multiply),                                // 10
            new() { Opcode = Opcode.PopStackToRegister, Data = Bytes(-1) },  // 11
            new() { Opcode = Opcode.Call, Label = "SUB" },               // 12
            new() { Opcode = Opcode.PushToStack, StepLabel = "START_1" },    // 13
            new() { Opcode = Opcode.PushRegisterToStack, Data = Bytes(1) },  // 14
            Push(-1),                                                    // 15
            Push(0),                                                     // 16
            Push(0),                                                     // 17
            Push(255),                                                   // 18
            Push(0),                                                     // 19
            Instruction(Opcode.SetupSprite),                             // 20
            Setup("START_2", 2, -1, 3, 3, 255, 1),                       // 21
            Setup("START_3", 3, -1, 0, 0, 255, 0),                       // 22
            Setup("START_4", 4, 2, 0, 0, 128, 0),                        // 23
            Instruction(Opcode.WaitForFrames, 2),                        // 24
            Instruction(Opcode.StampSprite, 2),                          // 25
            new() { Opcode = Opcode.ConditionalJump, Label = "MORE" },   // 26
            Instruction(Opcode.EndImmediate),                            // 27
            Instruction(Opcode.Subtract, 1),                             // 28 MORE
            Instruction(Opcode.CompareNotEqual, 0),                      // 29
            Instruction(Opcode.CompareGreaterThan, 1),                   // 30
            Instruction(Opcode.CompareLessThan, 1),                      // 31
            Instruction(Opcode.CompareGreaterOrEqual, 1),                // 32
            Instruction(Opcode.CompareLessOrEqual, 1),                   // 33
            Instruction(Opcode.Divide, 1),                               // 34
            Instruction(Opcode.RemoveSprite, 2),                         // 35
            new() { Opcode = Opcode.Jump, Label = "LOOP" },              // 36
            Instruction(Opcode.End),                                     // 37 LOOP
            Instruction(Opcode.WaitForFrames, 0),                        // 38 SUB
            Instruction(Opcode.Return)                                   // 39
        };
        animation.Control.InstructionLabels = new Dictionary<string, int>
        {
            ["SKIP"] = 8, ["MORE"] = 28, ["LOOP"] = 37, ["SUB"] = 38
        };
        animation.Control.Steps = new List<AnimationModel.AnimationStep>
        {
            StepValue(StepType.PushCounter, 2),        // 0 START_1
            DrawFrame(0),                              // 1 BODY
            Move(StepType.MoveRelative, 1, 1),         // 2
            StepValue(StepType.SetSpeed, 200),     // 3
            StepValue(StepType.AddSpeed, 55),// 4
            JumpIfCounter("BODY"),                     // 5
            DrawFrame(-1),                             // 6
            Step(StepType.Restart),                    // 7
            StepValue(StepType.PushCounter, 1),        // 8 START_2
            DrawFrame(3),                              // 9 AGAIN
            JumpIfCounter("AGAIN"),                    // 10
            Step(StepType.Loop),                       // 11
            Move(StepType.MoveAbsolute, 5, 6),         // 12 START_3
            DrawFrame(3),                              // 13
            Step(StepType.Stop),                       // 14
            DrawFrame(0),                              // 15 START_4
            Step(StepType.Pause)                       // 16
        };
        animation.Control.StepLabels = new Dictionary<string, int>
        {
            ["START_1"] = 0, ["BODY"] = 1, ["START_2"] = 8, ["AGAIN"] = 9, ["START_3"] = 12, ["START_4"] = 15
        };
        return animation;
    }

    #region Builders

    private static AnimationModel CreateBaseAnimation()
    {
        var animation = new AnimationModel
        {
            Key = "ANIM",
            Metadata = new SharedMetadata { Name = "Animation", Comment = "Round trip" },
            Data = new AnimationModel.GlobalData
            {
                BoundingWidth = 39,
                BoundingHeight = 29,
                FrameDelay = 1,
                BackgroundType = AnimationModel.BackgroundType.ClearToColor,
                ClearColor = 5,
                Unknown2 = 0,
                ImageIdToIndex = new Dictionary<int, int> { [0] = 0, [3] = 1 },
                ImageIndexToUnknownData = new Dictionary<int, int> { [0] = 17, [1] = 18 }
            }
        };
        animation.Images[0] = CreateImage(5, 4, seed: 1);
        animation.Images[1] = CreateImage(6, 3, seed: 2);
        return animation;
    }

    private static AnimationModel.AnimationInstruction Instruction(Opcode opcode, params short[] parameters)
    {
        return new AnimationModel.AnimationInstruction { Opcode = opcode, StackParameters = parameters };
    }

    private static AnimationModel.AnimationInstruction Push(short value)
    {
        return new AnimationModel.AnimationInstruction { Opcode = Opcode.PushToStack, Data = Bytes(value) };
    }

    private static AnimationModel.AnimationInstruction Setup(string stepLabel, short index, short follow, short x, short y, short rate, short flags)
    {
        return new AnimationModel.AnimationInstruction
        {
            Opcode = Opcode.SetupSprite,
            StepLabel = stepLabel,
            StackParameters = new[] { index, follow, x, y, rate, flags }
        };
    }

    private static AnimationModel.AnimationStep Step(StepType type)
    {
        return new AnimationModel.AnimationStep { Type = type };
    }

    private static AnimationModel.AnimationStep DrawFrame(sbyte imageId)
    {
        return new AnimationModel.AnimationStep { Type = StepType.DrawFrame, Data = new[] { (byte)imageId } };
    }

    private static AnimationModel.AnimationStep Move(StepType type, short x, short y)
    {
        return new AnimationModel.AnimationStep { Type = type, Data = Bytes(x).Concat(Bytes(y)).ToArray() };
    }

    private static AnimationModel.AnimationStep StepValue(StepType type, short value)
    {
        return new AnimationModel.AnimationStep { Type = type, Data = Bytes(value) };
    }

    private static AnimationModel.AnimationStep JumpIfCounter(string stepLabel)
    {
        return new AnimationModel.AnimationStep { Type = StepType.JumpIfCounter, StepLabel = stepLabel };
    }

    private static byte[] Bytes(short value)
    {
        return new[] { (byte)(value & 0xFF), (byte)((value >> 8) & 0xFF) };
    }

    #endregion
}
