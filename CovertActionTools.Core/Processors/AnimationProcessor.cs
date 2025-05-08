using System;
using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Models;

namespace CovertActionTools.Core.Processors
{
    public class AnimationState
    {
        /// <summary>
        /// A Sprite is a container that runs Animation Steps.
        /// </summary>
        public class Sprite
        {
            public int Index { get; set; }
            public int Counter { get; set; }
            public List<int> CounterStack { get; set; } = new();

            public int ImageId { get; set; } = -1;
            public int OriginalPositionX { get; set; }
            public int PositionX { get; set; }
            public int OriginalPositionY { get; set; }
            public int PositionY { get; set; }
            
            public bool Active { get; set; }
            public int OriginalStepIndex { get; set; }
            public int StepIndex { get; set; }

            public List<int> LastFrameStepIndices { get; set; } = new();
        }

        /// <summary>
        /// A Sprite may emit a DrawnImage multiple times.
        /// These persist across frames.
        /// </summary>
        public class DrawnImage
        {
            public int SpriteIndex { get; set; }
            public int ImageId { get; set; }
            public int PositionX { get; set; }
            public int PositionY { get; set; }
        }

        public List<Sprite> Sprites { get; set; } = new();
        public List<DrawnImage> DrawnImages { get; set; } = new();

        public List<short> Stack { get; set; } = new();
        public Dictionary<int, int> Registers { get; set; } = new();
        public bool CompareFlag { get; set; }
        public int InstructionIndex { get; set; }
        public int FramesToWait { get; set; }
        public int CurrentFrame { get; set; }
        public bool Ended { get; set; }

        public List<int> LastFrameInstructionIndices { get; set; } = new();
    }
    
    public interface IAnimationProcessor
    {
        AnimationState Process(AnimationModel animation, int frameIndex, Dictionary<int, int> inputRegisters);
    }
    
    internal class AnimationProcessor : IAnimationProcessor
    {
        public AnimationState Process(AnimationModel animation, int frameIndex, Dictionary<int, int> inputRegisters)
        {
            var state = new AnimationState();
            state.Registers = inputRegisters.ToDictionary(x => x.Key, x => x.Value);
            while (state.CurrentFrame <= frameIndex)
            {
                var firstFrame = true;
                while (state.FramesToWait > 0 && state.CurrentFrame <= frameIndex)
                {
                    foreach (var sprite in state.Sprites)
                    {
                        if (!sprite.Active)
                        {
                            continue;
                        }

                        sprite.LastFrameStepIndices.Clear();
                        var frameDone = false;
                        while (!frameDone)
                        {
                            sprite.LastFrameStepIndices.Add(sprite.StepIndex);
                            var step = animation.ExtraData.Steps[sprite.StepIndex];
                            if (step.Type == AnimationModel.AnimationStep.StepType.End)
                            {
                                sprite.Active = false;
                                frameDone = true;
                                continue;
                            }

                            if (step.Type == AnimationModel.AnimationStep.StepType.End9)
                            {
                                sprite.Active = false;
                                frameDone = true;
                                //but keep drawing it
                                state.DrawnImages.Add(new AnimationState.DrawnImage()
                                {
                                    SpriteIndex = sprite.Index,
                                    ImageId = sprite.ImageId,
                                    PositionX = sprite.PositionX,
                                    PositionY = sprite.PositionY
                                });
                                continue;
                            }

                            var nextIndex = sprite.StepIndex + 1;
                            switch (step.Type)
                            {
                                case AnimationModel.AnimationStep.StepType.End7:
                                    nextIndex = sprite.OriginalStepIndex;
                                    //reset the position to the original
                                    sprite.PositionX = sprite.OriginalPositionX;
                                    sprite.PositionY = sprite.OriginalPositionY;
                                    //TODO: should counter clear here?
                                    sprite.Counter = 0;
                                    sprite.CounterStack.Clear();
                                    break;
                                case AnimationModel.AnimationStep.StepType.Restart:
                                    nextIndex = sprite.OriginalStepIndex;
                                    //TODO: should counter clear here?
                                    sprite.Counter = 0;
                                    sprite.CounterStack.Clear();
                                    break;
                                case AnimationModel.AnimationStep.StepType.Jump:
                                    if (sprite.Counter > 0)
                                    {
                                        sprite.Counter--;
                                        if (sprite.Counter > 0)
                                        {
                                            nextIndex = animation.ExtraData.DataLabels[step.Label];
                                        }
                                        if (sprite.Counter == 0 && sprite.CounterStack.Count > 0)
                                        {
                                            sprite.Counter = sprite.CounterStack[sprite.CounterStack.Count - 1];
                                            sprite.CounterStack.RemoveAt(sprite.CounterStack.Count - 1);
                                        }
                                    }
                                    break;
                                case AnimationModel.AnimationStep.StepType.Move:
                                    var dx = (short)(step.Data[0] | (step.Data[1] << 8));
                                    var dy = (short)(step.Data[2] | (step.Data[3] << 8));
                                    sprite.PositionX += dx;
                                    sprite.PositionY += dy;
                                    break;
                                case AnimationModel.AnimationStep.StepType.SetImage:
                                    sprite.ImageId = (sbyte)step.Data[0];
                                    frameDone = true;
                                    break;
                                case AnimationModel.AnimationStep.StepType.SetCounter:
                                    var count = (short)(step.Data[0] | (step.Data[1] << 8));
                                    if (sprite.Counter > 0)
                                    {
                                        //if we already had one, we push the current one to the stack and add a new one
                                        sprite.CounterStack.Add(sprite.Counter);
                                    }
                                    sprite.Counter = count;
                                    break;
                                default:
                                    break;
                            }

                            sprite.StepIndex = nextIndex;
                        }
                    }

                    state.CurrentFrame++;
                    state.FramesToWait--;

                    if (!firstFrame)
                    {
                        state.LastFrameInstructionIndices.Clear();
                        state.LastFrameInstructionIndices.Add(state.InstructionIndex);
                    }
                    firstFrame = false;
                    if (state.FramesToWait == 0 && state.CurrentFrame <= frameIndex)
                    {
                        state.LastFrameInstructionIndices.Clear();
                        state.LastFrameInstructionIndices.Add(state.InstructionIndex);
                        state.InstructionIndex++;
                    }
                }

                if (state.CurrentFrame > frameIndex)
                {
                    break;
                }

                if (state.Ended)
                {
                    state.InstructionIndex--;
                }

                var currentInstruction = animation.ExtraData.Instructions[state.InstructionIndex];
                state.LastFrameInstructionIndices.Add(state.InstructionIndex);

                short ReadDataAsShort(int startIdx) => (short)(currentInstruction.Data[startIdx] | (currentInstruction.Data[startIdx + 1] << 8));
                var nextInstructionIndex = state.InstructionIndex + 1;
                switch (currentInstruction.Opcode)
                {
                    case AnimationModel.AnimationInstruction.AnimationOpcode.End:
                        state.FramesToWait = 1;
                        state.Ended = true;
                        nextInstructionIndex = state.InstructionIndex;
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.PushParameterToStack:
                        state.Stack.Add(ReadDataAsShort(0));
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Jump:
                        //TODO: condition?
                        nextInstructionIndex = animation.ExtraData.InstructionLabels[currentInstruction.Label];
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.ConditionalJump:
                        if (!state.CompareFlag)
                        {
                            nextInstructionIndex = animation.ExtraData.InstructionLabels[currentInstruction.Label];
                        }
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite:
                        var spriteIndex = currentInstruction.StackParameters[0];
                        var posX = currentInstruction.StackParameters[2];
                        var posY = currentInstruction.StackParameters[3];
                        var stepIndex = animation.ExtraData.DataLabels[currentInstruction.DataLabel];

                        var existingSprite = state.Sprites.FirstOrDefault(x => x.Index == spriteIndex);
                        if (existingSprite != null)
                        {
                            if (existingSprite.Active)
                            {
                                //TODO: what happens here?
                                throw new Exception("Tried to add sprite that already exists and is active");
                            }
                            existingSprite.Active = true;
                            existingSprite.PositionX = posX;
                            existingSprite.PositionY = posY;
                            existingSprite.OriginalStepIndex = stepIndex;
                            existingSprite.StepIndex = stepIndex;
                        }
                        else
                        {
                            state.Sprites.Add(new AnimationState.Sprite()
                            {
                                Active = true,
                                Index = spriteIndex,
                                PositionX = posX,
                                OriginalPositionX = posX,
                                PositionY = posY,
                                OriginalPositionY = posY,
                                OriginalStepIndex = stepIndex,
                                StepIndex = stepIndex
                            });
                        }
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.WaitForFrames:
                        state.FramesToWait = currentInstruction.StackParameters[0];
                        nextInstructionIndex = state.InstructionIndex;
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.KeepSpriteDrawn:
                        var sprite = state.Sprites.FirstOrDefault(x => x.Index == currentInstruction.StackParameters[0]);
                        if (sprite != null && sprite.Active && sprite.ImageId >= 0)
                        {
                            //TODO: not 100% accurate, in-game it looks like unless the sprite hits Restart, it will not draw itself there UNTIL it ends
                            state.DrawnImages.Add(new AnimationState.DrawnImage()
                            {
                                SpriteIndex = sprite.Index,
                                ImageId = sprite.ImageId,
                                PositionX = sprite.PositionX,
                                PositionY = sprite.PositionY
                            });
                        }
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.PopStackToRegister:
                    {
                        var registerIndex = (ushort)(currentInstruction.Data[0] | (currentInstruction.Data[1] << 8));
                        if (!state.Registers.ContainsKey(registerIndex))
                        {
                            state.Registers[registerIndex] = 0;
                        }

                        if (state.Stack.Count == 0)
                        {
                            state.Stack.Add(0);
                        }
                        var s1 = state.Stack[state.Stack.Count - 1];
                        state.Stack.RemoveAt(state.Stack.Count - 1); //TODO: should pop or not?

                        state.Registers[registerIndex] = s1;
                        break;
                    }
                    case AnimationModel.AnimationInstruction.AnimationOpcode.PushRegisterToStack:
                    {
                        var registerIndex = (ushort)(currentInstruction.Data[0] | (currentInstruction.Data[1] << 8));
                        if (!state.Registers.ContainsKey(registerIndex))
                        {
                            state.Registers[registerIndex] = 0;
                        }

                        state.Stack.Add((short)state.Registers[registerIndex]);
                        break;
                    }
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareEqual:
                    {
                        var target = currentInstruction.StackParameters[0];
                        var value = state.Stack[state.Stack.Count - 1];
                        //state.Stack.RemoveAt(state.Stack.Count - 1); //TODO: should pop or not?
                        state.CompareFlag = target == value;
                        break;
                    }
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareNotEqual:
                    {
                        var target = currentInstruction.StackParameters[0];
                        if (state.Stack.Count == 0)
                        {
                            state.Stack.Add(0);
                        }
                        var value = state.Stack[state.Stack.Count - 1];
                        //state.Stack.RemoveAt(state.Stack.Count - 1); //TODO: should pop or not?
                        state.CompareFlag = target != value;
                        break;
                    }
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Add:
                        var delta = currentInstruction.StackParameters[0];
                        if (state.Stack.Count == 0)
                        {
                            state.Stack.Add(0);
                        }
                        state.Stack[state.Stack.Count - 1] += delta;
                        break;
                    default:
                        break;
                }

                state.InstructionIndex = nextInstructionIndex;
            }

            foreach (var sprite in state.Sprites)
            {
                if (!sprite.Active || sprite.ImageId < 0)
                {
                    continue;
                }
                
                state.DrawnImages.Add(new AnimationState.DrawnImage()
                {
                    SpriteIndex = sprite.Index,
                    ImageId = sprite.ImageId,
                    PositionX = sprite.PositionX,
                    PositionY = sprite.PositionY
                });
            }

            return state;
        }
    }
}