using System;
using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Models;

using Opcode = CovertActionTools.Core.Models.AnimationModel.AnimationInstruction.AnimationOpcode;
using StepType = CovertActionTools.Core.Models.AnimationModel.AnimationStep.StepType;

namespace CovertActionTools.UnitTests.Core.Processors.Data;

/// <summary>
/// Builds small in-memory animations for driving the processor directly.
/// </summary>
public static class AnimationProcessorTestDataGenerator
{
    public const int PageWidth = 20;
    public const int PageHeight = 10;

    /// <summary>
    /// A solid image of the given colour, registered under the given image ID.
    /// </summary>
    public static SharedImageModel SolidImage(int width, int height, byte color)
    {
        var pixels = new byte[width * height];
        Array.Fill(pixels, color);
        return new SharedImageModel
        {
            Data = new SharedImageModel.ImageData { Width = width, Height = height, CompressionDictionaryWidth = 11 },
            RawVgaImageData = pixels,
            VgaImageData = new byte[width * height * 4],
            CgaImageData = Array.Empty<byte>()
        };
    }

    /// <summary>
    /// A 2x2 image whose top-left pixel is transparent (colour 0), the rest the given colour.
    /// </summary>
    public static SharedImageModel CornerImage(byte color)
    {
        var image = SolidImage(2, 2, color);
        image.RawVgaImageData[0] = 0;
        return image;
    }

    public static AnimationModel CreateAnimation(
        IEnumerable<AnimationModel.AnimationInstruction> instructions,
        IEnumerable<AnimationModel.AnimationStep> steps,
        Dictionary<string, int>? instructionLabels = null,
        Dictionary<string, int>? stepLabels = null,
        Dictionary<int, SharedImageModel>? imagesById = null,
        AnimationModel.BackgroundType backgroundType = AnimationModel.BackgroundType.ClearToColor,
        byte clearColor = 0)
    {
        var model = new AnimationModel
        {
            Key = "TEST",
            Data = new AnimationModel.GlobalData
            {
                BoundingWidth = PageWidth - 1,
                BoundingHeight = PageHeight - 1,
                FrameDelay = 1,
                BackgroundType = backgroundType,
                ClearColor = clearColor
            },
            Control = new AnimationModel.ControlData
            {
                Instructions = instructions.ToList(),
                InstructionLabels = instructionLabels ?? new Dictionary<string, int>(),
                Steps = steps.ToList(),
                StepLabels = stepLabels ?? new Dictionary<string, int>()
            }
        };

        var index = 0;
        foreach (var pair in (imagesById ?? new Dictionary<int, SharedImageModel>()).OrderBy(x => x.Key))
        {
            if (pair.Key == -1)
            {
                model.Images[-1] = pair.Value;
                continue;
            }

            model.Data.ImageIdToIndex[pair.Key] = index;
            model.Data.ImageIndexToUnknownData[index] = 1;
            model.Images[index] = pair.Value;
            index++;
        }

        return model;
    }

    #region Instruction builders

    public static AnimationModel.AnimationInstruction Instruction(Opcode opcode, params short[] parameters)
    {
        return new AnimationModel.AnimationInstruction { Opcode = opcode, StackParameters = parameters };
    }

    public static AnimationModel.AnimationInstruction Push(short value)
    {
        return new AnimationModel.AnimationInstruction { Opcode = Opcode.PushToStack, Data = Bytes(value) };
    }

    public static AnimationModel.AnimationInstruction PushStepLabel(string stepLabel)
    {
        return new AnimationModel.AnimationInstruction { Opcode = Opcode.PushToStack, StepLabel = stepLabel };
    }

    public static AnimationModel.AnimationInstruction PushRegister(short register)
    {
        return new AnimationModel.AnimationInstruction { Opcode = Opcode.PushRegisterToStack, Data = Bytes(register) };
    }

    public static AnimationModel.AnimationInstruction PopRegister(short register)
    {
        return new AnimationModel.AnimationInstruction { Opcode = Opcode.PopStackToRegister, Data = Bytes(register) };
    }

    public static AnimationModel.AnimationInstruction JumpTo(Opcode opcode, string label)
    {
        return new AnimationModel.AnimationInstruction { Opcode = opcode, Label = label };
    }

    public static AnimationModel.AnimationInstruction SetupSprite(string stepLabel, short index, short follow, short x, short y, short rate, short flags)
    {
        return new AnimationModel.AnimationInstruction
        {
            Opcode = Opcode.SetupSprite,
            StepLabel = stepLabel,
            StackParameters = new[] { index, follow, x, y, rate, flags }
        };
    }

    public static AnimationModel.AnimationInstruction Wait(short frames)
    {
        return Instruction(Opcode.WaitForFrames, frames);
    }

    public static AnimationModel.AnimationInstruction End()
    {
        return Instruction(Opcode.End);
    }

    #endregion

    #region Step builders

    public static AnimationModel.AnimationStep Step(StepType type)
    {
        return new AnimationModel.AnimationStep { Type = type };
    }

    public static AnimationModel.AnimationStep DrawFrame(sbyte imageId)
    {
        return new AnimationModel.AnimationStep { Type = StepType.DrawFrame, Data = new[] { (byte)imageId } };
    }

    public static AnimationModel.AnimationStep Move(StepType type, short x, short y)
    {
        return new AnimationModel.AnimationStep { Type = type, Data = Bytes(x).Concat(Bytes(y)).ToArray() };
    }

    public static AnimationModel.AnimationStep StepValue(StepType type, short value)
    {
        return new AnimationModel.AnimationStep { Type = type, Data = Bytes(value) };
    }

    public static AnimationModel.AnimationStep JumpIfCounter(string stepLabel)
    {
        return new AnimationModel.AnimationStep { Type = StepType.JumpIfCounter, StepLabel = stepLabel };
    }

    #endregion

    public static byte[] Bytes(short value)
    {
        return new[] { (byte)(value & 0xFF), (byte)((value >> 8) & 0xFF) };
    }
}
