using System;
using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Models;

namespace CovertActionTools.Core.Processors
{
    public class AnimationState
    {
        public const int MaxSprites = 50;
        public const int MaxRegisters = 51;

        /// <summary>
        /// A Sprite is a container that runs Animation Steps.
        /// </summary>
        public class Sprite
        {
            public int Index { get; set; }
            /// <summary>
            /// When set to a valid sprite, drawn position is an offset from that sprite's own position
            /// </summary>
            public int FollowIndex { get; set; }
            /// <summary>
            /// Loop counters, the active one is the last
            /// </summary>
            public List<short> CounterStack { get; set; } = new();
            public int Counter => CounterStack.Count > 0 ? CounterStack[CounterStack.Count - 1] : 0;

            public int ImageId { get; set; }
            public int OriginalPositionX { get; set; }
            public int PositionX { get; set; }
            public int OriginalPositionY { get; set; }
            public int PositionY { get; set; }

            public bool Active { get; set; }
            /// <summary>
            /// Set by StampSprite for the current frame
            /// </summary>
            public bool Stamped { get; set; }
            public int OriginalStepIndex { get; set; }
            public int StepIndex { get; set; }

            /// <summary>
            /// Accumulates Speed every frame, steps run once it passes 255
            /// </summary>
            public short Credit { get; set; }
            /// <summary>
            /// Added to Credit every frame, starts at Rate
            /// </summary>
            public short Speed { get; set; }
            public short Rate { get; set; }
            /// <summary>
            /// Non-zero draws the sprite without erasing it on later frames; only zero versus non-zero matters, which is exactly what the legacy game engine does
            /// Note: retail game versions only set it on static sprites, moving ones are supported by the legacy game engine
            /// </summary>
            public short Flags { get; set; }

            public List<int> LastFrameStepIndices { get; set; } = new();
        }

        public List<Sprite> Sprites { get; set; } = new();

        public int PageWidth { get; set; }
        public int PageHeight { get; set; }
        /// <summary>
        /// Background page, one byte per pixel, holds the background and any stamps
        /// </summary>
        public byte[] Background { get; set; } = Array.Empty<byte>();
        /// <summary>
        /// Draw page, one byte per pixel, holds the last frame shown
        /// </summary>
        public byte[] Frame { get; set; } = Array.Empty<byte>();
        /// <summary>
        /// Displayed colour for each of the 16 colour indices while the animation plays
        /// </summary>
        public byte[] Palette { get; set; } = Array.Empty<byte>();

        public Stack<short> Stack { get; set; } = new();
        public Dictionary<int, int> Registers { get; set; } = new();
        public int InstructionIndex { get; set; }
        public int FramesToWait { get; set; }
        public int CurrentFrame { get; set; }
        public bool Ended { get; set; }

        public List<int> LastFrameInstructionIndices { get; set; } = new();
        public List<int> TriggeredAudio { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public Sprite? GetSprite(int spriteIndex)
        {
            return Sprites.FirstOrDefault(x => x.Index == spriteIndex);
        }

        public (int x, int y)? GetSpritePosition(int spriteIndex)
        {
            var sprite = GetSprite(spriteIndex);
            if (sprite == null)
            {
                return null;
            }

            if (sprite.FollowIndex == -1)
            {
                return (sprite.PositionX, sprite.PositionY);
            }

            //following is a single level: the followed sprite's own offset plus this sprite's home and offset
            if (sprite.FollowIndex < 1 || sprite.FollowIndex > MaxSprites)
            {
                return null;
            }

            var followed = GetSprite(sprite.FollowIndex);
            if (followed == null || !followed.Active)
            {
                return null;
            }

            return (followed.PositionX + sprite.OriginalPositionX + sprite.PositionX,
                followed.PositionY + sprite.OriginalPositionY + sprite.PositionY);
        }
    }

    public interface IAnimationProcessor
    {
        AnimationState Process(AnimationModel animation, int frameIndex, Dictionary<int, (int value, int frameIndex)> inputRegisters, AnimationState? previousState = null);
    }

    internal class AnimationProcessor : IAnimationProcessor
    {
        private const int MaxInstructionsPerFrame = 100000;
        private const int MaxStepsPerFrame = 100000;

        public AnimationState Process(AnimationModel animation, int frameIndex, Dictionary<int, (int value, int frameIndex)> inputRegisters, AnimationState? previousState = null)
        {
            var engine = new Engine(animation, previousState);
            for (var frame = 0; frame <= frameIndex; frame++)
            {
                engine.ApplyRegisters(inputRegisters, frame);
                if (!engine.RunFrame(frame))
                {
                    break;
                }
            }

            return engine.State;
        }

        /// <summary>
        /// Runs the animation one frame at a time the way the game does: instructions until a wait,
        /// then sprite steps, stamps into the background page, background restore and sprite drawing.
        /// </summary>
        private class Engine
        {
            private readonly AnimationModel _animation;
            private readonly int _width;
            private readonly int _height;
            private readonly int[] _rowMin;
            private readonly int[] _rowMax;
            private int _dirtyMinY = int.MaxValue;
            private int _dirtyMaxY = -1;
            private bool _waiting;
            private short _waitCount;
            private readonly List<short> _audioQueue = new();
            private readonly HashSet<string> _warned = new();

            public AnimationState State { get; } = new();

            public Engine(AnimationModel animation, AnimationState? previousState)
            {
                _animation = animation;
                _width = animation.Data.BoundingWidth + 1;
                _height = animation.Data.BoundingHeight + 1;
                _rowMin = new int[_height];
                _rowMax = new int[_height];
                ResetDirty();

                State.PageWidth = _width;
                State.PageHeight = _height;
                State.Palette = BuildPalette(animation.Data, previousState);
                State.Background = new byte[_width * _height];
                switch (animation.Data.BackgroundType)
                {
                    case AnimationModel.BackgroundType.ClearToImage:
                        if (animation.Images.TryGetValue(-1, out var background))
                        {
                            Draw(State.Background, background, 0, 0, false);
                        }
                        break;
                    case AnimationModel.BackgroundType.ClearToColor:
                        for (var i = 0; i < State.Background.Length; i++)
                        {
                            State.Background[i] = animation.Data.ClearColor;
                        }
                        break;
                    default:
                        if (previousState != null)
                        {
                            CopyPage(previousState.Background, previousState.PageWidth, previousState.PageHeight, State.Background);
                        }
                        break;
                }

                State.Frame = State.Background.ToArray();
            }

            /// <summary>
            /// The colour block is loaded into the palette with entry 0 overwritten with 0, as the game does
            /// </summary>
            private static byte[] BuildPalette(AnimationModel.GlobalData data, AnimationState? previousState)
            {
                var palette = new byte[16];
                for (var i = 0; i < palette.Length; i++)
                {
                    palette[i] = (byte)i;
                }

                switch (data.ColorMappingType)
                {
                    case AnimationModel.ColorMappingType.Embedded:
                        foreach (var pair in data.ColorMapping)
                        {
                            if (pair.Key < palette.Length)
                            {
                                palette[pair.Key] = (byte)(pair.Value & 0x0F);
                            }
                        }
                        break;
                    case AnimationModel.ColorMappingType.Palette:
                        for (var i = 0; i < palette.Length && i < data.PaletteData.Length; i++)
                        {
                            palette[i] = (byte)(data.PaletteData[i] & 0x0F);
                        }
                        break;
                    case AnimationModel.ColorMappingType.Previous:
                        if (previousState != null && previousState.Palette.Length == palette.Length)
                        {
                            palette = previousState.Palette.ToArray();
                        }
                        break;
                }

                palette[0] = 0;
                return palette;
            }

            public void ApplyRegisters(Dictionary<int, (int value, int frameIndex)> inputRegisters, int frame)
            {
                foreach (var pair in inputRegisters.Where(x => x.Value.frameIndex == frame))
                {
                    State.Registers[pair.Key] = pair.Value.value;
                }
            }

            public bool RunFrame(int frame)
            {
                State.CurrentFrame = frame;
                State.LastFrameInstructionIndices.Clear();
                State.TriggeredAudio.Clear();
                foreach (var sprite in State.Sprites)
                {
                    if (sprite.Active)
                    {
                        sprite.Stamped = false;
                    }
                }

                if (!RunInstructions())
                {
                    State.Ended = true;
                    return false;
                }

                StepSprites();
                DrawStamps();
                Restore();
                DrawSprites();
                FlushAudio();
                return true;
            }

            #region Instructions

            private bool RunInstructions()
            {
                var instructions = _animation.Control.Instructions;
                var executed = 0;
                while (true)
                {
                    if (_waiting)
                    {
                        //the counter is tested before it is decremented, so a wait of N lasts exactly N frames
                        var remaining = _waitCount--;
                        if (remaining != 0)
                        {
                            State.FramesToWait = _waitCount;
                            return true;
                        }

                        _waiting = false;
                        State.FramesToWait = 0;
                    }

                    if (State.InstructionIndex < 0 || State.InstructionIndex >= instructions.Count)
                    {
                        Warn($"Instruction {State.InstructionIndex} is out of range");
                        return true;
                    }

                    if (++executed > MaxInstructionsPerFrame)
                    {
                        Warn("Instructions loop without waiting for a frame");
                        return true;
                    }

                    var instruction = instructions[State.InstructionIndex];
                    State.LastFrameInstructionIndices.Add(State.InstructionIndex);
                    foreach (var parameter in instruction.StackParameters)
                    {
                        Push(parameter);
                    }

                    var next = State.InstructionIndex + 1;
                    switch (instruction.Opcode)
                    {
                        case AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite:
                            SetupSprite(instruction);
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.RemoveSprite:
                        {
                            var sprite = State.GetSprite(Pop());
                            if (sprite != null)
                            {
                                sprite.Active = false;
                            }
                            break;
                        }
                        case AnimationModel.AnimationInstruction.AnimationOpcode.WaitForFrames:
                            _waitCount = Pop();
                            _waiting = true;
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.TriggerAudio:
                            _audioQueue.Add(Pop());
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.StampSprite:
                        {
                            var sprite = State.GetSprite(Pop());
                            if (sprite != null)
                            {
                                sprite.Stamped = true;
                            }
                            break;
                        }
                        case AnimationModel.AnimationInstruction.AnimationOpcode.PushToStack:
                            if (!string.IsNullOrEmpty(instruction.StepLabel))
                            {
                                Push((short)ResolveStepLabel(instruction.StepLabel));
                            }
                            else
                            {
                                Push(ReadShort(instruction.Data));
                            }
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.PushRegisterToStack:
                            Push((short)GetRegister(ReadShort(instruction.Data)));
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.PopStackToRegister:
                        {
                            var value = Pop();
                            var register = ReadShort(instruction.Data);
                            if (register >= 0 && register < AnimationState.MaxRegisters)
                            {
                                State.Registers[register] = value;
                            }
                            break;
                        }
                        case AnimationModel.AnimationInstruction.AnimationOpcode.PushCopyOfStackValue:
                        {
                            var value = Pop();
                            Push(value);
                            Push(value);
                            break;
                        }
                        case AnimationModel.AnimationInstruction.AnimationOpcode.CompareEqual:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.CompareNotEqual:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.CompareGreaterThan:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.CompareLessThan:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.CompareGreaterOrEqual:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.CompareLessOrEqual:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.Add:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.Subtract:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.Multiply:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.Divide:
                        {
                            var second = Pop();
                            var first = Pop();
                            if (instruction.Opcode == AnimationModel.AnimationInstruction.AnimationOpcode.Divide && second == 0)
                            {
                                Warn("Division by zero");
                            }
                            Push(Compute(instruction.Opcode, first, second));
                            break;
                        }
                        case AnimationModel.AnimationInstruction.AnimationOpcode.ConditionalJump:
                            if (Pop() != 0)
                            {
                                next = ResolveInstructionLabel(instruction.Label, next);
                            }
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.Jump:
                            next = ResolveInstructionLabel(instruction.Label, next);
                            break;
                        //Note: Call and Return are not used in retail game versions but supported by the legacy game engine
                        case AnimationModel.AnimationInstruction.AnimationOpcode.Call:
                            Push((short)next);
                            next = ResolveInstructionLabel(instruction.Label, next);
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.Return:
                            next = Pop();
                            break;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.End:
                            //the instruction pointer stays here, so every later frame only steps and draws sprites
                            return true;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.EndImmediate:
                            return false;
                        case AnimationModel.AnimationInstruction.AnimationOpcode.Comment:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.RawByte:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.RawShort:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.RawLabel:
                        case AnimationModel.AnimationInstruction.AnimationOpcode.RawDataLabel:
                            break;
                        default:
                            Warn($"Unknown instruction {instruction.Opcode} at {State.InstructionIndex}");
                            return true;
                    }

                    State.InstructionIndex = next;
                }
            }

            private void SetupSprite(AnimationModel.AnimationInstruction instruction)
            {
                var flags = Pop();
                var rate = Pop();
                var positionY = Pop();
                var positionX = Pop();
                var followIndex = Pop();
                int spriteIndex = Pop();
                var stepIndex = !string.IsNullOrEmpty(instruction.StepLabel)
                    ? ResolveStepLabel(instruction.StepLabel)
                    : Pop();

                //-1 takes the first free slot, and falls back to the last slot when none is free
                //Note: not used in retail game versions but supported by the legacy game engine
                if (spriteIndex == -1)
                {
                    spriteIndex = AnimationState.MaxSprites;
                    for (var i = 1; i <= AnimationState.MaxSprites; i++)
                    {
                        var existing = State.GetSprite(i);
                        if (existing == null || !existing.Active)
                        {
                            spriteIndex = i;
                            break;
                        }
                    }
                }

                if (spriteIndex < 1 || spriteIndex > AnimationState.MaxSprites)
                {
                    return;
                }

                var sprite = State.GetSprite(spriteIndex);
                if (sprite == null)
                {
                    sprite = new AnimationState.Sprite() { Index = spriteIndex };
                    var insertAt = State.Sprites.FindIndex(x => x.Index > spriteIndex);
                    State.Sprites.Insert(insertAt < 0 ? State.Sprites.Count : insertAt, sprite);
                }

                sprite.Active = true;
                sprite.Stamped = false;
                sprite.FollowIndex = followIndex;
                sprite.OriginalPositionX = positionX;
                sprite.OriginalPositionY = positionY;
                sprite.PositionX = followIndex == -1 ? positionX : 0;
                sprite.PositionY = followIndex == -1 ? positionY : 0;
                sprite.Rate = rate;
                sprite.Speed = rate;
                sprite.Credit = 255;
                sprite.CounterStack.Clear();
                sprite.OriginalStepIndex = stepIndex;
                sprite.StepIndex = stepIndex;
                sprite.Flags = flags;
                sprite.LastFrameStepIndices.Clear();
            }

            private static short Compute(AnimationModel.AnimationInstruction.AnimationOpcode opcode, short first, short second)
            {
                switch (opcode)
                {
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareEqual:
                        return (short)(first == second ? 1 : 0);
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareNotEqual:
                        return (short)(first != second ? 1 : 0);
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareGreaterThan:
                        return (short)(first > second ? 1 : 0);
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareLessThan:
                        return (short)(first < second ? 1 : 0);
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareGreaterOrEqual:
                        return (short)(first >= second ? 1 : 0);
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareLessOrEqual:
                        return (short)(first <= second ? 1 : 0);
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Add:
                        return (short)(first + second);
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Subtract:
                        return (short)(first - second);
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Multiply:
                        return (short)(first * second);
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Divide:
                        return second == 0 ? (short)0 : (short)(first / second);
                    default:
                        return 0;
                }
            }

            private void Push(short value)
            {
                State.Stack.Push(value);
            }

            private short Pop()
            {
                if (State.Stack.Count == 0)
                {
                    Warn("Stack underflow");
                    return 0;
                }

                return State.Stack.Pop();
            }

            private int GetRegister(int register)
            {
                if (register < 0 || register >= AnimationState.MaxRegisters)
                {
                    Warn($"Register {register} is out of range");
                    return 0;
                }

                return State.Registers.TryGetValue(register, out var value) ? value : 0;
            }

            private int ResolveInstructionLabel(string label, int fallback)
            {
                if (_animation.Control.InstructionLabels.TryGetValue(label, out var index))
                {
                    return index;
                }

                Warn($"Missing label {label}");
                return fallback;
            }

            private int ResolveStepLabel(string label)
            {
                if (_animation.Control.StepLabels.TryGetValue(label, out var index))
                {
                    return index;
                }

                Warn($"Missing data label {label}");
                return _animation.Control.Steps.Count;
            }

            private static short ReadShort(byte[] data)
            {
                return (short)(data[0] | (data[1] << 8));
            }

            #endregion

            #region Steps

            private void StepSprites()
            {
                foreach (var sprite in State.Sprites)
                {
                    if (!sprite.Active)
                    {
                        continue;
                    }

                    sprite.LastFrameStepIndices.Clear();
                    sprite.Credit = (short)(sprite.Credit + sprite.Speed);
                    if (sprite.Credit <= 255)
                    {
                        continue;
                    }

                    sprite.Credit -= 255;
                    RunSteps(sprite);
                }
            }

            private void RunSteps(AnimationState.Sprite sprite)
            {
                var steps = _animation.Control.Steps;
                var executed = 0;
                while (true)
                {
                    if (sprite.StepIndex < 0 || sprite.StepIndex >= steps.Count)
                    {
                        Warn($"Sprite {sprite.Index} step {sprite.StepIndex} is out of range");
                        sprite.Active = false;
                        return;
                    }

                    if (++executed > MaxStepsPerFrame)
                    {
                        Warn($"Sprite {sprite.Index} steps loop without drawing a frame");
                        sprite.Active = false;
                        return;
                    }

                    var step = steps[sprite.StepIndex];
                    sprite.LastFrameStepIndices.Add(sprite.StepIndex);
                    var next = sprite.StepIndex + 1;
                    switch (step.Type)
                    {
                        case AnimationModel.AnimationStep.StepType.DrawFrame:
                            sprite.ImageId = step.Data[0] == 0xFF ? -1 : step.Data[0];
                            sprite.StepIndex = next;
                            return;
                        case AnimationModel.AnimationStep.StepType.MoveAbsolute:
                            sprite.PositionX = ReadShort(step.Data);
                            sprite.PositionY = (short)(step.Data[2] | (step.Data[3] << 8));
                            break;
                        case AnimationModel.AnimationStep.StepType.MoveRelative:
                            sprite.PositionX = (short)(sprite.PositionX + ReadShort(step.Data));
                            sprite.PositionY = (short)(sprite.PositionY + (short)(step.Data[2] | (step.Data[3] << 8)));
                            break;
                        //Note: SetSpeed and AddSpeed are not used in retail game versions but supported by the legacy game engine
                        case AnimationModel.AnimationStep.StepType.SetSpeed:
                            sprite.Speed = ReadShort(step.Data);
                            break;
                        case AnimationModel.AnimationStep.StepType.AddSpeed:
                            sprite.Speed = (short)(sprite.Speed + ReadShort(step.Data));
                            break;
                        case AnimationModel.AnimationStep.StepType.PushCounter:
                            sprite.CounterStack.Add(ReadShort(step.Data));
                            break;
                        case AnimationModel.AnimationStep.StepType.JumpIfCounter:
                            //the counter is decremented first, so a counter of N runs the loop body N times
                            if (sprite.CounterStack.Count == 0)
                            {
                                Warn($"Sprite {sprite.Index} jumps on a counter without one");
                                break;
                            }

                            var last = sprite.CounterStack.Count - 1;
                            sprite.CounterStack[last] = (short)(sprite.CounterStack[last] - 1);
                            if (sprite.CounterStack[last] != 0)
                            {
                                next = ResolveStepLabel(step.StepLabel);
                            }
                            else
                            {
                                sprite.CounterStack.RemoveAt(last);
                            }
                            break;
                        case AnimationModel.AnimationStep.StepType.Restart:
                            sprite.PositionX = sprite.FollowIndex == -1 ? sprite.OriginalPositionX : 0;
                            sprite.PositionY = sprite.FollowIndex == -1 ? sprite.OriginalPositionY : 0;
                            sprite.Speed = sprite.Rate;
                            sprite.Credit = 255;
                            sprite.CounterStack.Clear();
                            next = sprite.OriginalStepIndex;
                            break;
                        case AnimationModel.AnimationStep.StepType.Loop:
                            sprite.CounterStack.Clear();
                            next = sprite.OriginalStepIndex;
                            break;
                        case AnimationModel.AnimationStep.StepType.Pause:
                            return;
                        case AnimationModel.AnimationStep.StepType.Stop:
                            sprite.Active = false;
                            return;
                        case AnimationModel.AnimationStep.StepType.Comment:
                        case AnimationModel.AnimationStep.StepType.RawByte:
                        case AnimationModel.AnimationStep.StepType.RawShort:
                        case AnimationModel.AnimationStep.StepType.RawLabel:
                            break;
                        default:
                            Warn($"Sprite {sprite.Index} hit unknown step {step.Type}");
                            sprite.Active = false;
                            return;
                    }

                    sprite.StepIndex = next;
                }
            }

            #endregion

            #region Drawing

            private void DrawStamps()
            {
                foreach (var sprite in State.Sprites)
                {
                    if (!sprite.Active || !sprite.Stamped)
                    {
                        continue;
                    }

                    var position = State.GetSpritePosition(sprite.Index);
                    var image = GetImage(sprite);
                    if (position == null || image == null)
                    {
                        continue;
                    }

                    Draw(State.Background, image, position.Value.x, position.Value.y, true);
                    MarkDirty(position.Value.x, position.Value.y, image.Data.Width, image.Data.Height);
                }
            }

            private void DrawSprites()
            {
                foreach (var sprite in State.Sprites)
                {
                    if (!sprite.Active || sprite.Stamped || sprite.ImageId == -1)
                    {
                        continue;
                    }

                    var position = State.GetSpritePosition(sprite.Index);
                    var image = GetImage(sprite);
                    if (position == null || image == null)
                    {
                        continue;
                    }

                    Draw(State.Frame, image, position.Value.x, position.Value.y, true);
                    if (sprite.Flags == 0)
                    {
                        MarkDirty(position.Value.x, position.Value.y, image.Data.Width, image.Data.Height);
                    }
                }
            }

            private SharedImageModel? GetImage(AnimationState.Sprite sprite)
            {
                if (sprite.ImageId == -1)
                {
                    Warn($"Sprite {sprite.Index} is stamped without an image");
                    return null;
                }

                if (!_animation.Data.ImageIdToIndex.TryGetValue(sprite.ImageId, out var index) ||
                    !_animation.Images.TryGetValue(index, out var image))
                {
                    Warn($"Sprite {sprite.Index} uses missing image {sprite.ImageId}");
                    return null;
                }

                return image;
            }

            private void Draw(byte[] page, SharedImageModel image, int x, int y, bool transparent)
            {
                var width = image.Data.Width;
                var height = image.Data.Height;
                var pixels = image.RawVgaImageData;
                for (var py = 0; py < height; py++)
                {
                    var targetY = y + py;
                    if (targetY < 0 || targetY >= _height)
                    {
                        continue;
                    }

                    for (var px = 0; px < width; px++)
                    {
                        var targetX = x + px;
                        if (targetX < 0 || targetX >= _width)
                        {
                            continue;
                        }

                        var color = pixels[py * width + px];
                        if (transparent && color == 0)
                        {
                            continue;
                        }

                        page[targetY * _width + targetX] = color;
                    }
                }
            }

            private void CopyPage(byte[] source, int sourceWidth, int sourceHeight, byte[] target)
            {
                if (source.Length < sourceWidth * sourceHeight)
                {
                    return;
                }

                var width = Math.Min(sourceWidth, _width);
                var height = Math.Min(sourceHeight, _height);
                for (var y = 0; y < height; y++)
                {
                    Array.Copy(source, y * sourceWidth, target, y * _width, width);
                }
            }

            /// <summary>
            /// Records the rows and horizontal span a draw touched, so the background is put back there next frame
            /// </summary>
            private void MarkDirty(int x, int y, int width, int height)
            {
                var firstRow = Math.Max(y, 0);
                var lastRow = Math.Min(y + height - 1, _height - 1);
                if (firstRow > lastRow || width <= 0)
                {
                    return;
                }

                _dirtyMinY = Math.Min(_dirtyMinY, firstRow);
                _dirtyMaxY = Math.Max(_dirtyMaxY, lastRow);
                for (var row = firstRow; row <= lastRow; row++)
                {
                    _rowMin[row] = Math.Min(_rowMin[row], x);
                    _rowMax[row] = Math.Max(_rowMax[row], x + width - 1);
                }
            }

            private void Restore()
            {
                if (_dirtyMaxY < 0)
                {
                    return;
                }

                for (var row = _dirtyMinY; row <= _dirtyMaxY; row++)
                {
                    var first = Math.Max(_rowMin[row], 0);
                    var last = Math.Min(_rowMax[row], _width - 1);
                    if (first <= last)
                    {
                        Array.Copy(State.Background, row * _width + first, State.Frame, row * _width + first, last - first + 1);
                    }
                }

                ResetDirty();
            }

            private void ResetDirty()
            {
                _dirtyMinY = int.MaxValue;
                _dirtyMaxY = -1;
                for (var row = 0; row < _height; row++)
                {
                    _rowMin[row] = int.MaxValue;
                    _rowMax[row] = -1;
                }
            }

            private void FlushAudio()
            {
                //the queue is emptied from the most recent entry
                for (var i = _audioQueue.Count - 1; i >= 0; i--)
                {
                    State.TriggeredAudio.Add(_audioQueue[i]);
                }

                _audioQueue.Clear();
            }

            #endregion

            private void Warn(string message)
            {
                if (_warned.Add(message))
                {
                    State.Warnings.Add($"Frame {State.CurrentFrame}: {message}");
                }
            }
        }
    }
}
