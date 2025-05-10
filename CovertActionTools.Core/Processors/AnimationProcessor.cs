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
            /// <summary>
            /// When set to a valid sprite, drawn position is an offset from that sprite's position
            /// TODO: does this chain in the real game?
            /// </summary>
            public int FollowIndex { get; set; }
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
        public bool WaitWithoutDraw { get; set; }
        public List<int> PendingSpriteStamps { get; set; } = new();
        public int CurrentFrame { get; set; }
        public bool Ended { get; set; }

        public List<int> LastFrameInstructionIndices { get; set; } = new();

        public (int x, int y) GetSpritePosition(int spriteIndex)
        {
            var sprite = Sprites.FirstOrDefault(x => x.Index == spriteIndex);
            if (sprite == null)
            {
                return (-9999, -9999);
            }

            //following sprites get referenced as offsets
            var positionX = 0;
            var positionY = 0;
            var curSprite = sprite;
            do
            {
                positionX += curSprite.PositionX;
                positionY += curSprite.PositionY;
                curSprite = Sprites.FirstOrDefault(x => x.Index == curSprite.FollowIndex);
            } while (curSprite != null);

            return (positionX, positionY);
        }
    }
    
    public interface IAnimationProcessor
    {
        AnimationState Process(AnimationModel animation, int frameIndex, Dictionary<int, (int value, int frameIndex)> inputRegisters);
    }
    
    internal class AnimationProcessor : IAnimationProcessor
    {
        public AnimationState Process(AnimationModel animation, int frameIndex, Dictionary<int, (int value, int frameIndex)> inputRegisters)
        {
            var state = new AnimationState();
            //apply input registers on inputs only
            state.Registers = inputRegisters
                .Where(x => x.Value.frameIndex == 0)
                .ToDictionary(x => x.Key, x => x.Value.value);
            var laterRegisterValues = inputRegisters
                .Where(x => x.Value.frameIndex != 0)
                .Select(x => x.Value.frameIndex)
                .Distinct()
                .ToDictionary(x => x, targetFrame => inputRegisters
                    .Where(x => x.Value.frameIndex == targetFrame)
                    .ToDictionary(x => x.Key, x => x.Value.value)
                );
            while (state.CurrentFrame <= frameIndex)
            {
                var hadWait = false;
                var firstFrame = true;
                while (state.WaitWithoutDraw || (state.FramesToWait > 0 && state.CurrentFrame <= frameIndex))
                {
                    hadWait = true;
                    if (laterRegisterValues.TryGetValue(state.CurrentFrame, out var registersToSet))
                    {
                        foreach (var pair in registersToSet)
                        {
                            state.Registers[pair.Key] = pair.Value;
                        }
                        //remove so that we don't re-trigger it for some reason
                        laterRegisterValues.Remove(state.CurrentFrame);
                    }
                    
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
                            if (step.Type == AnimationModel.AnimationStep.StepType.Stop)
                            {
                                sprite.Active = false;
                                frameDone = true;
                                continue;
                            }

                            var nextIndex = sprite.StepIndex + 1;
                            switch (step.Type)
                            {
                                case AnimationModel.AnimationStep.StepType.Pause:
                                    //effectively acts like a constant frame wait without continuing past this instruction
                                    frameDone = true;
                                    nextIndex = sprite.StepIndex;
                                    break;
                                case AnimationModel.AnimationStep.StepType.Restart:
                                    nextIndex = sprite.OriginalStepIndex;
                                    //reset the position to the original
                                    sprite.PositionX = sprite.OriginalPositionX;
                                    sprite.PositionY = sprite.OriginalPositionY;
                                    //TODO: should counter clear here?
                                    sprite.Counter = 0;
                                    sprite.CounterStack.Clear();
                                    break;
                                case AnimationModel.AnimationStep.StepType.Loop:
                                    nextIndex = sprite.OriginalStepIndex;
                                    //TODO: should counter clear here?
                                    sprite.Counter = 0;
                                    sprite.CounterStack.Clear();
                                    break;
                                case AnimationModel.AnimationStep.StepType.JumpIfCounter:
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
                                case AnimationModel.AnimationStep.StepType.MoveRelative:
                                    var dx = (short)(step.Data[0] | (step.Data[1] << 8));
                                    var dy = (short)(step.Data[2] | (step.Data[3] << 8));
                                    sprite.PositionX += dx;
                                    sprite.PositionY += dy;
                                    break;
                                case AnimationModel.AnimationStep.StepType.MoveAbsolute:
                                    var newX = (short)(step.Data[0] | (step.Data[1] << 8));
                                    var newY = (short)(step.Data[2] | (step.Data[3] << 8));
                                    sprite.PositionX = newX;
                                    sprite.PositionY = newY;
                                    break;
                                case AnimationModel.AnimationStep.StepType.DrawFrame:
                                    sprite.ImageId = (sbyte)step.Data[0];
                                    frameDone = true;
                                    break;
                                case AnimationModel.AnimationStep.StepType.PushCounter:
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

                    if (state.WaitWithoutDraw)
                    {
                        //when WaitForFrames 0 is used, we don't actually advance the frame, we just ran one update
                        state.WaitWithoutDraw = false;
                    }
                    else
                    {
                        state.CurrentFrame++;
                        state.FramesToWait--;
                    }

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

                if (hadWait)
                {
                    foreach (var spriteToStamp in state.PendingSpriteStamps)
                    {
                        var sprite = state.Sprites.FirstOrDefault(x => x.Index == spriteToStamp); 
                        if (sprite != null && sprite.Active && sprite.ImageId >= 0)
                        {
                            var (x, y) = state.GetSpritePosition(sprite.Index);
                            state.DrawnImages.Add(new AnimationState.DrawnImage()
                            {
                                SpriteIndex = sprite.Index,
                                ImageId = sprite.ImageId,
                                PositionX = x,
                                PositionY = y
                            });
                        }
                        else
                        {
                            throw new Exception(
                                $"Attempted to stamp missing/inactive sprite: {spriteToStamp} {sprite?.ImageId} {sprite?.Active}");
                        }
                    }
                    state.PendingSpriteStamps.Clear();
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
                    case AnimationModel.AnimationInstruction.AnimationOpcode.EndImmediate:
                        state.Ended = true;
                        nextInstructionIndex = state.InstructionIndex;
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.End:
                        state.FramesToWait = 1;
                        state.Ended = true;
                        nextInstructionIndex = state.InstructionIndex;
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.PushToStack:
                        state.Stack.Add(ReadDataAsShort(0));
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Jump:
                        //TODO: condition?
                        nextInstructionIndex = animation.ExtraData.InstructionLabels[currentInstruction.Label];
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.ConditionalJump:
                        if (state.CompareFlag)
                        {
                            nextInstructionIndex = animation.ExtraData.InstructionLabels[currentInstruction.Label];
                        }
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite:
                        var spriteIndex = currentInstruction.StackParameters[0];
                        var followIndex = currentInstruction.StackParameters[1];
                        var posX = currentInstruction.StackParameters[2];
                        var posY = currentInstruction.StackParameters[3];
                        var stepIndex = animation.ExtraData.DataLabels[currentInstruction.DataLabel];

                        if (spriteIndex == 41)
                        {
                            var q = 0;
                        }

                        var existingSprite = state.Sprites.FirstOrDefault(x => x.Index == spriteIndex);
                        if (existingSprite != null)
                        {
                            existingSprite.Active = true;
                            existingSprite.ImageId = -1;
                            existingSprite.FollowIndex = followIndex;
                            existingSprite.OriginalPositionX = posX;
                            existingSprite.PositionX = posX;
                            existingSprite.OriginalPositionY = posY;
                            existingSprite.PositionY = posY;
                            existingSprite.OriginalStepIndex = stepIndex;
                            existingSprite.StepIndex = stepIndex;
                            existingSprite.Counter = 0;
                            existingSprite.CounterStack.Clear();
                            existingSprite.LastFrameStepIndices.Clear();
                        }
                        else
                        {
                            state.Sprites.Add(new AnimationState.Sprite()
                            {
                                Active = true,
                                Index = spriteIndex,
                                FollowIndex = followIndex,
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
                        if (currentInstruction.StackParameters[0] != 0)
                        {
                            state.FramesToWait = currentInstruction.StackParameters[0];
                        }
                        else
                        {
                            state.WaitWithoutDraw = true;
                        }
                        nextInstructionIndex = state.InstructionIndex;

                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.StampSprite:
                    {
                        //stamping is done after the wait finishes, to allow the sprites to have an init set of steps
                        state.PendingSpriteStamps.Add(currentInstruction.StackParameters[0]);
                        break;
                    }
                    case AnimationModel.AnimationInstruction.AnimationOpcode.RemoveSprite:
                    {
                        var sprite = state.Sprites.FirstOrDefault(x => x.Index == currentInstruction.StackParameters[0]);
                        if (sprite != null)
                        {
                            sprite.Active = false;
                        }

                        break;
                    }
                    case AnimationModel.AnimationInstruction.AnimationOpcode.PopStackToRegister:
                    {
                        var registerIndex = (ushort)(currentInstruction.Data[0] | (currentInstruction.Data[1] << 8));
                        if (registerIndex == 0xFFFF)
                        {
                            //pop discard, so the value is popped but nothing happens
                            state.Stack.RemoveAt(state.Stack.Count - 1);
                        }
                        else
                        {
                            if (!state.Registers.ContainsKey(registerIndex))
                            {
                                state.Registers[registerIndex] = 0;
                            }

                            var s1 = state.Stack[state.Stack.Count - 1];
                            state.Stack.RemoveAt(state.Stack.Count - 1); //TODO: should pop or not? 

                            state.Registers[registerIndex] = s1;
                        }
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
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareNotEqual:
                    {
                        var target = currentInstruction.StackParameters[0];
                        var value = state.Stack[state.Stack.Count - 1];
                        state.Stack.RemoveAt(state.Stack.Count - 1);
                        state.CompareFlag = target != value;
                        break;
                    }
                    case AnimationModel.AnimationInstruction.AnimationOpcode.CompareEqual:
                    {
                        var target = currentInstruction.StackParameters[0];
                        var value = state.Stack[state.Stack.Count - 1];
                        state.Stack.RemoveAt(state.Stack.Count - 1);
                        state.CompareFlag = target == value;
                        break;
                    }
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Add:
                        var delta = currentInstruction.StackParameters[0];
                        state.Stack[state.Stack.Count - 1] += delta;
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.PushCopyOfStackValue:
                        state.Stack.Add(state.Stack[state.Stack.Count - 1]);
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.TriggerAudio:
                        //TODO: add to state
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
                
                var (x, y) = state.GetSpritePosition(sprite.Index);
                state.DrawnImages.Add(new AnimationState.DrawnImage()
                {
                    SpriteIndex = sprite.Index,
                    ImageId = sprite.ImageId,
                    PositionX = x,
                    PositionY = y
                });
            }

            return state;
        }
    }
}