using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Models;
using CovertActionTools.Core.Processors;
using Xunit;

using CovertActionTools.UnitTests.Core.Processors.Data;

using Opcode = CovertActionTools.Core.Models.AnimationModel.AnimationInstruction.AnimationOpcode;
using StepType = CovertActionTools.Core.Models.AnimationModel.AnimationStep.StepType;
using static CovertActionTools.UnitTests.Core.Processors.Data.AnimationProcessorTestDataGenerator;

namespace CovertActionTools.UnitTests.Core.Processors;

public class AnimationProcessorTests
{
    private readonly AnimationProcessor _processor = new();

    #region Instruction flow

    [Fact]
    public void Process_WaitForFrames_ResumesAfterExactCount()
    {
        var animation = CreateAnimation(
            new[] { Wait(3), Wait(1), End() },
            new AnimationModel.AnimationStep[0]);

        Assert.Equal(new[] { 0 }, Run(animation, 0).LastFrameInstructionIndices);
        Assert.Empty(Run(animation, 2).LastFrameInstructionIndices);
        Assert.Equal(new[] { 1 }, Run(animation, 3).LastFrameInstructionIndices);
        Assert.Equal(new[] { 2 }, Run(animation, 4).LastFrameInstructionIndices);
    }

    [Fact]
    public void Process_WaitForFramesZero_DoesNotConsumeFrame()
    {
        var animation = CreateAnimation(
            new[] { Wait(0), Wait(1), End() },
            new AnimationModel.AnimationStep[0]);

        var state = Run(animation, 0);

        Assert.Equal(new[] { 0, 1 }, state.LastFrameInstructionIndices);
    }

    [Fact]
    public void Process_ConditionalJump_TakenOnNonZero()
    {
        var animation = CreateAnimation(
            new[] { Push(1), JumpTo(Opcode.ConditionalJump, "L"), Wait(5), Wait(1), End() },
            new AnimationModel.AnimationStep[0],
            new Dictionary<string, int> { ["L"] = 3 });

        var state = Run(animation, 0);

        Assert.Equal(new[] { 0, 1, 3 }, state.LastFrameInstructionIndices);
    }

    [Fact]
    public void Process_CallAndReturn_RunSubroutine()
    {
        var animation = CreateAnimation(
            new[]
            {
                JumpTo(Opcode.Call, "SUB"), Wait(1), End(),
                Push(9), PopRegister(0), Instruction(Opcode.Return)
            },
            new AnimationModel.AnimationStep[0],
            new Dictionary<string, int> { ["SUB"] = 3 });

        var state = Run(animation, 0);

        Assert.Equal(new[] { 0, 3, 4, 5, 1 }, state.LastFrameInstructionIndices);
        Assert.Equal(9, state.Registers[0]);
    }

    [Fact]
    public void Process_Comparisons_UseFirstPushedAsFirstOperand()
    {
        var animation = CreateAnimation(
            new[]
            {
                Push(5), Instruction(Opcode.CompareGreaterThan, 3), PopRegister(1),
                Push(2), Instruction(Opcode.Subtract, 5), PopRegister(2),
                Push(7), Instruction(Opcode.Divide, 0), PopRegister(3),
                End()
            },
            new AnimationModel.AnimationStep[0]);

        var state = Run(animation, 0);

        Assert.Equal(1, state.Registers[1]);
        Assert.Equal(-3, state.Registers[2]);
        Assert.Equal(0, state.Registers[3]);
        Assert.DoesNotContain(state.Warnings, x => x.Contains("Stack underflow"));
        Assert.Contains(state.Warnings, x => x.Contains("Division by zero"));
    }

    [Fact]
    public void Process_Registers_InputsReadAndOutOfRangeWritesDiscarded()
    {
        var animation = CreateAnimation(
            new[] { PushRegister(3), PopRegister(60), PushRegister(3), PopRegister(4), PushRegister(51), PopRegister(5), End() },
            new AnimationModel.AnimationStep[0]);

        var state = Run(animation, 0, new Dictionary<int, (int value, int frameIndex)> { [3] = (42, 0) });

        Assert.Equal(42, state.Registers[4]);
        Assert.False(state.Registers.ContainsKey(60));
        Assert.Equal(0, state.Registers[5]);
        Assert.Contains(state.Warnings, x => x.Contains("Register 51"));
    }

    [Fact]
    public void Process_InputRegister_AppliedAtRequestedFrame()
    {
        var animation = CreateAnimation(
            new[] { Wait(2), PushRegister(0), PopRegister(1), End() },
            new AnimationModel.AnimationStep[0]);

        var state = Run(animation, 3, new Dictionary<int, (int value, int frameIndex)> { [0] = (7, 2) });

        Assert.Equal(7, state.Registers[1]);
    }

    [Fact]
    public void Process_StackUnderflow_WarnsAndUsesZero()
    {
        var animation = CreateAnimation(
            new[] { Instruction(Opcode.Add), PopRegister(0), End() },
            new AnimationModel.AnimationStep[0]);

        var state = Run(animation, 0);

        Assert.Equal(0, state.Registers[0]);
        Assert.Contains(state.Warnings, x => x.Contains("Stack underflow"));
    }

    [Fact]
    public void Process_EndImmediate_FreezesEverything()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 2, 3, 255, 0), Wait(1), Instruction(Opcode.EndImmediate) },
            new[] { Move(StepType.MoveRelative, 1, 0), DrawFrame(0), Step(StepType.Loop) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 },
            imagesById: new Dictionary<int, SharedImageModel> { [0] = SolidImage(2, 2, 7) });

        var first = Run(animation, 0);
        var later = Run(animation, 5);

        Assert.False(first.Ended);
        Assert.True(later.Ended);
        Assert.Equal(first.Frame, later.Frame);
        Assert.Equal(first.GetSpritePosition(1), later.GetSpritePosition(1));
    }

    [Fact]
    public void Process_End_KeepsSteppingSprites()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 2, 3, 255, 0), End() },
            new[] { Move(StepType.MoveRelative, 1, 0), DrawFrame(0), Step(StepType.Loop) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 },
            imagesById: new Dictionary<int, SharedImageModel> { [0] = SolidImage(2, 2, 7) });

        var state = Run(animation, 3);

        Assert.Equal((6, 3), state.GetSpritePosition(1));
        Assert.Equal(7, Pixel(state, 6, 3));
    }

    [Fact]
    public void Process_TriggerAudio_FlushedMostRecentFirst()
    {
        var animation = CreateAnimation(
            new[] { Instruction(Opcode.TriggerAudio, 3), Instruction(Opcode.TriggerAudio, 7), Wait(1), End() },
            new AnimationModel.AnimationStep[0]);

        Assert.Equal(new[] { 7, 3 }, Run(animation, 0).TriggeredAudio);
        Assert.Empty(Run(animation, 1).TriggeredAudio);
    }

    #endregion

    #region Sprite setup

    [Fact]
    public void Process_SetupSprite_DrawsOnSameFrame()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 2, 3, 255, 0), End() },
            new[] { DrawFrame(0), Step(StepType.Pause) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 },
            imagesById: new Dictionary<int, SharedImageModel> { [0] = SolidImage(2, 2, 7) });

        var state = Run(animation, 0);

        Assert.Equal(7, Pixel(state, 2, 3));
        Assert.Equal(7, Pixel(state, 3, 4));
        Assert.Equal(0, Pixel(state, 1, 3));
        Assert.Equal(0, Pixel(state, 4, 3));
    }

    [Fact]
    public void Process_SetupSpriteMinusOne_TakesFirstFreeSlot()
    {
        var animation = CreateAnimation(
            new[]
            {
                SetupSprite("S", -1, -1, 0, 0, 255, 0),
                SetupSprite("S", -1, -1, 0, 0, 255, 0),
                SetupSprite("S", 51, -1, 0, 0, 255, 0),
                End()
            },
            new[] { DrawFrame(0), Step(StepType.Pause) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 });

        var state = Run(animation, 0);

        Assert.Equal(new[] { 1, 2 }, state.Sprites.Select(x => x.Index));
    }

    [Fact]
    public void Process_BareSetupSprite_PopsAllParameters()
    {
        var animation = CreateAnimation(
            new[]
            {
                PushStepLabel("S"), Push(3), Push(-1), Push(2), Push(3), Push(255), Push(0),
                Instruction(Opcode.SetupSprite), End()
            },
            new[] { DrawFrame(0), Step(StepType.Pause) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 },
            imagesById: new Dictionary<int, SharedImageModel> { [0] = SolidImage(2, 2, 7) });

        var state = Run(animation, 0);

        Assert.Equal((2, 3), state.GetSpritePosition(3));
        Assert.Equal(7, Pixel(state, 2, 3));
    }

    [Fact]
    public void Process_RemoveSprite_StopsDrawingAndRestoresBackground()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 2, 3, 255, 0), Wait(1), Instruction(Opcode.RemoveSprite, 1), End() },
            new[] { DrawFrame(0), Step(StepType.Pause) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 },
            imagesById: new Dictionary<int, SharedImageModel> { [0] = SolidImage(2, 2, 7) },
            clearColor: 4);

        var state = Run(animation, 1);

        Assert.False(state.GetSprite(1)!.Active);
        Assert.Equal(4, Pixel(state, 2, 3));
    }

    #endregion

    #region Sprite stepping

    [Theory]
    [InlineData(255, 6)]
    [InlineData(128, 4)]
    [InlineData(0, 0)]
    public void Process_Rate_ControlsHowOftenStepsRun(short rate, int expectedX)
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 0, 0, rate, 0), End() },
            new[] { Move(StepType.MoveRelative, 1, 0), DrawFrame(0), Step(StepType.Loop) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 });

        var state = Run(animation, 5);

        Assert.Equal((expectedX, 0), state.GetSpritePosition(1));
    }

    [Fact]
    public void Process_MovedSprite_OldPositionIsRestored()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 2, 3, 255, 0), End() },
            new[] { DrawFrame(0), Move(StepType.MoveRelative, 5, 0), DrawFrame(0), Step(StepType.Pause) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 },
            imagesById: new Dictionary<int, SharedImageModel> { [0] = SolidImage(2, 2, 7) });

        var state = Run(animation, 1);

        Assert.Equal(0, Pixel(state, 2, 3));
        Assert.Equal(7, Pixel(state, 7, 3));
    }

    [Fact]
    public void Process_OverlaySprite_IsNeverErased()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 2, 3, 255, 1), End() },
            new[] { DrawFrame(0), Move(StepType.MoveRelative, 5, 0), DrawFrame(0), Step(StepType.Pause) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 },
            imagesById: new Dictionary<int, SharedImageModel> { [0] = SolidImage(2, 2, 7) });

        var state = Run(animation, 1);

        Assert.Equal(7, Pixel(state, 2, 3));
        Assert.Equal(7, Pixel(state, 7, 3));
    }

    [Fact]
    public void Process_TransparentPixels_AreNotDrawn()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 2, 3, 255, 0), End() },
            new[] { DrawFrame(0), Step(StepType.Pause) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 },
            imagesById: new Dictionary<int, SharedImageModel> { [0] = CornerImage(7) },
            clearColor: 4);

        var state = Run(animation, 0);

        Assert.Equal(4, Pixel(state, 2, 3));
        Assert.Equal(7, Pixel(state, 3, 3));
    }

    [Fact]
    public void Process_DrawFrameMinusOne_HidesSprite()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 2, 3, 255, 0), End() },
            new[] { DrawFrame(0), DrawFrame(-1), Step(StepType.Pause) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 },
            imagesById: new Dictionary<int, SharedImageModel> { [0] = SolidImage(2, 2, 7) });

        Assert.Equal(7, Pixel(Run(animation, 0), 2, 3));
        var state = Run(animation, 1);
        Assert.Equal(-1, state.GetSprite(1)!.ImageId);
        Assert.Equal(0, Pixel(state, 2, 3));
    }

    [Fact]
    public void Process_JumpIfCounter_RunsBodyCounterTimes()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 2, 3, 255, 0), End() },
            new[]
            {
                StepValue(StepType.PushCounter, 3),
                Move(StepType.MoveRelative, 1, 0), JumpIfCounter("L"),
                DrawFrame(0), Step(StepType.Pause)
            },
            stepLabels: new Dictionary<string, int> { ["S"] = 0, ["L"] = 1 });

        var state = Run(animation, 0);

        Assert.Equal((5, 3), state.GetSpritePosition(1));
        Assert.Empty(state.GetSprite(1)!.CounterStack);
    }

    [Fact]
    public void Process_NestedCounters_RunInnerBodyEachOuterPass()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 0, 0, 255, 0), End() },
            new[]
            {
                StepValue(StepType.PushCounter, 2),
                StepValue(StepType.PushCounter, 3),
                Move(StepType.MoveRelative, 1, 0), JumpIfCounter("INNER"),
                Move(StepType.MoveRelative, 0, 1), JumpIfCounter("OUTER"),
                DrawFrame(0), Step(StepType.Pause)
            },
            stepLabels: new Dictionary<string, int> { ["S"] = 0, ["OUTER"] = 1, ["INNER"] = 2 });

        var state = Run(animation, 0);

        Assert.Equal((6, 2), state.GetSpritePosition(1));
    }

    [Fact]
    public void Process_Restart_ResetsPositionAndSpeed()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 2, 3, 255, 0), End() },
            new[]
            {
                DrawFrame(0), Move(StepType.MoveRelative, 1, 0),
                StepValue(StepType.SetSpeed, 100), Step(StepType.Restart)
            },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 });

        var state = Run(animation, 2);
        var sprite = state.GetSprite(1)!;

        Assert.Equal((2, 3), state.GetSpritePosition(1));
        Assert.Equal(255, sprite.Speed);
        Assert.Equal(255, sprite.Credit);
    }

    [Fact]
    public void Process_Loop_KeepsPosition()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 2, 3, 255, 0), End() },
            new[] { DrawFrame(0), Move(StepType.MoveRelative, 1, 0), Step(StepType.Loop) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 });

        var state = Run(animation, 2);

        Assert.Equal((4, 3), state.GetSpritePosition(1));
    }

    [Fact]
    public void Process_AddSpeed_AddsToSpeed()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 0, 0, 255, 0), End() },
            new[]
            {
                StepValue(StepType.SetSpeed, 100), StepValue(StepType.AddSpeed, 55),
                DrawFrame(0), Step(StepType.Pause)
            },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 });

        var state = Run(animation, 0);

        Assert.Equal(155, state.GetSprite(1)!.Speed);
    }

    [Fact]
    public void Process_Stop_DeactivatesAndErasesSprite()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 2, 3, 255, 0), End() },
            new[] { DrawFrame(0), Step(StepType.Stop) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 },
            imagesById: new Dictionary<int, SharedImageModel> { [0] = SolidImage(2, 2, 7) });

        Assert.Equal(7, Pixel(Run(animation, 0), 2, 3));
        var state = Run(animation, 1);
        Assert.False(state.GetSprite(1)!.Active);
        Assert.Equal(0, Pixel(state, 2, 3));
    }

    [Fact]
    public void Process_Pause_KeepsDrawingLastImage()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 2, 3, 255, 0), End() },
            new[] { DrawFrame(0), Step(StepType.Pause) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 },
            imagesById: new Dictionary<int, SharedImageModel> { [0] = SolidImage(2, 2, 7) });

        var state = Run(animation, 4);

        Assert.True(state.GetSprite(1)!.Active);
        Assert.Equal(1, state.GetSprite(1)!.StepIndex);
        Assert.Equal(7, Pixel(state, 2, 3));
    }

    [Fact]
    public void Process_StepLoopWithoutFrame_StopsSpriteWithWarning()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 0, 0, 255, 0), End() },
            new[] { Move(StepType.MoveRelative, 1, 0), Step(StepType.Loop) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 });

        var state = Run(animation, 0);

        Assert.False(state.GetSprite(1)!.Active);
        Assert.NotEmpty(state.Warnings);
    }

    #endregion

    #region Following

    [Fact]
    public void Process_FollowingSprite_AddsFollowedOffsetAndHome()
    {
        var animation = CreateAnimation(
            new[]
            {
                SetupSprite("S", 1, -1, 2, 3, 255, 0),
                SetupSprite("S", 2, 1, 1, 1, 255, 0),
                End()
            },
            new[] { DrawFrame(0), Move(StepType.MoveRelative, 2, 0), DrawFrame(0), Step(StepType.Pause) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 });

        var first = Run(animation, 0);
        var second = Run(animation, 1);

        Assert.Equal((3, 4), first.GetSpritePosition(2));
        Assert.Equal((7, 4), second.GetSpritePosition(2));
    }

    [Fact]
    public void Process_FollowingSprite_MoveAbsoluteStaysRelative()
    {
        var animation = CreateAnimation(
            new[]
            {
                SetupSprite("A", 1, -1, 2, 3, 255, 0),
                SetupSprite("B", 2, 1, 1, 1, 255, 0),
                End()
            },
            new[]
            {
                DrawFrame(0), Step(StepType.Pause),
                Move(StepType.MoveAbsolute, 4, 0), DrawFrame(0), Step(StepType.Pause)
            },
            stepLabels: new Dictionary<string, int> { ["A"] = 0, ["B"] = 2 });

        var state = Run(animation, 0);

        Assert.Equal((7, 4), state.GetSpritePosition(2));
    }

    [Fact]
    public void Process_FollowingSprite_NotDrawnWhenFollowedInactive()
    {
        var animation = CreateAnimation(
            new[]
            {
                SetupSprite("S", 1, -1, 2, 3, 255, 0),
                SetupSprite("S", 2, 1, 1, 1, 255, 0),
                Wait(1),
                Instruction(Opcode.RemoveSprite, 1),
                End()
            },
            new[] { DrawFrame(0), Step(StepType.Pause) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 },
            imagesById: new Dictionary<int, SharedImageModel> { [0] = SolidImage(1, 1, 7) });

        var state = Run(animation, 1);

        Assert.Null(state.GetSpritePosition(2));
        Assert.Equal(0, Pixel(state, 3, 4));
    }

    #endregion

    #region Stamps and backgrounds

    [Fact]
    public void Process_StampSprite_DrawsIntoBackgroundAndPersists()
    {
        var animation = CreateAnimation(
            new[]
            {
                SetupSprite("S", 1, -1, 2, 3, 255, 0),
                Instruction(Opcode.StampSprite, 1),
                Wait(1),
                Instruction(Opcode.RemoveSprite, 1),
                End()
            },
            new[] { DrawFrame(0), Step(StepType.Pause) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 },
            imagesById: new Dictionary<int, SharedImageModel> { [0] = SolidImage(2, 2, 7) });

        var first = Run(animation, 0);
        var later = Run(animation, 3);

        Assert.Equal(7, first.Background[3 * PageWidth + 2]);
        Assert.Equal(7, Pixel(first, 2, 3));
        Assert.False(later.GetSprite(1)!.Active);
        Assert.Equal(7, Pixel(later, 2, 3));
    }

    [Fact]
    public void Process_ClearToImage_DrawsBackgroundImage()
    {
        var animation = CreateAnimation(
            new[] { End() },
            new AnimationModel.AnimationStep[0],
            imagesById: new Dictionary<int, SharedImageModel> { [-1] = SolidImage(PageWidth, PageHeight, 4) },
            backgroundType: AnimationModel.BackgroundType.ClearToImage);

        var state = Run(animation, 0);

        Assert.All(state.Background, x => Assert.Equal(4, x));
        Assert.All(state.Frame, x => Assert.Equal(4, x));
    }

    [Fact]
    public void Process_ClearToColor_FillsBackground()
    {
        var animation = CreateAnimation(new[] { End() }, new AnimationModel.AnimationStep[0], clearColor: 6);

        var state = Run(animation, 0);

        Assert.All(state.Frame, x => Assert.Equal(6, x));
    }

    [Fact]
    public void Process_PreviousAnimation_KeepsPreviousBackgroundPage()
    {
        var previous = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, 2, 3, 255, 0), Instruction(Opcode.StampSprite, 1), End() },
            new[] { DrawFrame(0), Step(StepType.Pause) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 },
            imagesById: new Dictionary<int, SharedImageModel> { [0] = SolidImage(2, 2, 7) },
            clearColor: 6);
        var previousState = Run(previous, 0);
        var animation = CreateAnimation(new[] { End() }, new AnimationModel.AnimationStep[0],
            backgroundType: AnimationModel.BackgroundType.PreviousAnimation);

        var state = _processor.Process(animation, 0, new(), previousState);

        Assert.Equal(previousState.Background, state.Background);
        Assert.Equal(7, Pixel(state, 2, 3));
        Assert.Equal(6, Pixel(state, 0, 0));
    }

    [Fact]
    public void Process_Palette_LoadsColorMappingWithEntryZeroForcedToZero()
    {
        var animation = CreateAnimation(new[] { End() }, new AnimationModel.AnimationStep[0]);
        animation.Data.ColorMapping = new Dictionary<byte, byte> { [0] = 3, [5] = 0, [15] = 12 };

        var state = Run(animation, 0);

        Assert.Equal(0, state.Palette[0]);
        Assert.Equal(0, state.Palette[5]);
        Assert.Equal(12, state.Palette[15]);
        Assert.Equal(7, state.Palette[7]);
    }

    [Fact]
    public void Process_PreviousColorMapping_KeepsPreviousPalette()
    {
        var previous = CreateAnimation(new[] { End() }, new AnimationModel.AnimationStep[0]);
        previous.Data.ColorMapping = new Dictionary<byte, byte> { [15] = 12 };
        var previousState = Run(previous, 0);
        var animation = CreateAnimation(new[] { End() }, new AnimationModel.AnimationStep[0]);
        animation.Data.ColorMappingType = AnimationModel.ColorMappingType.Previous;

        var state = _processor.Process(animation, 0, new(), previousState);

        Assert.Equal(12, state.Palette[15]);
    }

    [Fact]
    public void Process_SpriteOffPage_IsClipped()
    {
        var animation = CreateAnimation(
            new[] { SetupSprite("S", 1, -1, -1, -1, 255, 0), End() },
            new[] { DrawFrame(0), Step(StepType.Pause) },
            stepLabels: new Dictionary<string, int> { ["S"] = 0 },
            imagesById: new Dictionary<int, SharedImageModel> { [0] = SolidImage(2, 2, 7) });

        var state = Run(animation, 0);

        Assert.Equal(7, Pixel(state, 0, 0));
        Assert.Equal(0, Pixel(state, 1, 1));
    }

    #endregion

    #region Helpers

    private AnimationState Run(AnimationModel animation, int frame, Dictionary<int, (int value, int frameIndex)>? registers = null)
    {
        return _processor.Process(animation, frame, registers ?? new Dictionary<int, (int value, int frameIndex)>());
    }

    private static byte Pixel(AnimationState state, int x, int y)
    {
        return state.Frame[y * state.PageWidth + x];
    }

    #endregion
}
