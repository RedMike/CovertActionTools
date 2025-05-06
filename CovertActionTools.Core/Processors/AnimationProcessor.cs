using System;
using System.Collections.Generic;
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
        
            public int ImageId { get; set; } //maybe not actually maintained?
            public int PositionX { get; set; }
            public int PositionY { get; set; }
            
            public bool Active { get; set; }
            public int OriginalStepIndex { get; set; }
            public int StepIndex { get; set; }
        }

        /// <summary>
        /// A Sprite may emit a DrawnImage multiple times.
        /// These persist across frames.
        /// </summary>
        public class DrawnImage
        {
            public int ImageId { get; set; }
            public int PositionX { get; set; }
            public int PositionY { get; set; }
        }

        public Dictionary<int, Sprite> Sprites { get; set; } = new();
        public List<DrawnImage> DrawnImages { get; set; } = new();

        public List<short> Stack { get; set; } = new();
        public int InstructionIndex { get; set; }
        public int FramesToWait { get; set; }
        public int CurrentFrame { get; set; }
    }
    
    public interface IAnimationProcessor
    {
        AnimationState Process(AnimationModel animation, int frameIndex);
    }
    
    internal class AnimationProcessor : IAnimationProcessor
    {
        public AnimationState Process(AnimationModel animation, int frameIndex)
        {
            var state = new AnimationState();
            while (state.CurrentFrame <= frameIndex)
            {
                while (state.FramesToWait > 0 && state.CurrentFrame <= frameIndex)
                {
                    foreach (var sprite in state.Sprites.Values)
                    {
                        if (!sprite.Active)
                        {
                            continue;
                        }

                        var frameDone = false;
                        while (!frameDone)
                        {
                            var step = animation.ExtraData.Steps[sprite.StepIndex];
                            if (step.Type == AnimationModel.AnimationStep.StepType.End)
                            {
                                sprite.Active = false;
                                frameDone = true;
                                continue;
                            }

                            if (step.Type == AnimationModel.AnimationStep.StepType.End7 ||
                                step.Type == AnimationModel.AnimationStep.StepType.End9)
                            {
                                //TODO: is this correct? conditional?
                                sprite.Active = false;
                                frameDone = true;
                                continue;
                            }

                            var nextIndex = sprite.StepIndex + 1;
                            switch (step.Type)
                            {
                                case AnimationModel.AnimationStep.StepType.Restart:
                                    nextIndex = sprite.OriginalStepIndex;
                                    frameDone = true;
                                    break;
                                case AnimationModel.AnimationStep.StepType.JumpAndReduceCounter:
                                    if (sprite.Counter > 0)
                                    {
                                        sprite.Counter--;
                                        nextIndex = animation.ExtraData.DataLabels[step.Label];
                                        frameDone = true;
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
                                    break;
                                case AnimationModel.AnimationStep.StepType.SetCounter:
                                    var count = (short)(step.Data[0] | (step.Data[1] << 8));
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
                    if (state.FramesToWait == 0)
                    {
                        state.InstructionIndex++;
                    }
                }

                if (state.CurrentFrame > frameIndex)
                {
                    break;
                }

                var currentInstruction = animation.ExtraData.Instructions[state.InstructionIndex];
                if (currentInstruction.Opcode == AnimationModel.AnimationInstruction.AnimationOpcode.End)
                {
                    break;
                }

                short ReadDataAsShort(int startIdx) => (short)(currentInstruction.Data[startIdx] | (currentInstruction.Data[startIdx + 1] << 8));
                var nextInstructionIndex = state.InstructionIndex + 1;
                switch (currentInstruction.Opcode)
                {
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Push:
                        state.Stack.Add(ReadDataAsShort(0));
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Jump13:
                        //TODO: condition?
                        nextInstructionIndex = animation.ExtraData.InstructionLabels[currentInstruction.Label];
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.Jump12:
                        //TODO: condition?
                        nextInstructionIndex = animation.ExtraData.InstructionLabels[currentInstruction.Label];
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite:
                        var spriteIndex = currentInstruction.StackParameters[0];
                        if (state.Sprites.ContainsKey(spriteIndex))
                        {
                            //TODO: what happens here?
                            throw new Exception("Tried to add sprite that already exists");
                        }

                        var posX = currentInstruction.StackParameters[2];
                        var posY = currentInstruction.StackParameters[3];
                        var stepIndex = animation.ExtraData.DataLabels[currentInstruction.DataLabel];
                        state.Sprites[spriteIndex] = new AnimationState.Sprite()
                        {
                            Active = true,
                            Index = spriteIndex,
                            PositionX = posX,
                            PositionY = posY,
                            OriginalStepIndex = stepIndex, 
                            StepIndex = stepIndex
                        };
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.WaitForFrames:
                        state.FramesToWait = currentInstruction.StackParameters[0];
                        nextInstructionIndex = state.InstructionIndex;
                        break;
                    case AnimationModel.AnimationInstruction.AnimationOpcode.KeepSpriteDrawn:
                        var sprite = state.Sprites[currentInstruction.StackParameters[0]];
                        if (sprite.Active && sprite.ImageId >= 0)
                        {
                            state.DrawnImages.Add(new AnimationState.DrawnImage()
                            {
                                ImageId = sprite.ImageId,
                                PositionX = sprite.PositionX,
                                PositionY = sprite.PositionY
                            });
                        }
                        break;
                    default:
                        break;
                }

                state.InstructionIndex = nextInstructionIndex;
            }

            foreach (var sprite in state.Sprites.Values)
            {
                if (!sprite.Active || sprite.ImageId < 0)
                {
                    continue;
                }
                
                state.DrawnImages.Add(new AnimationState.DrawnImage()
                {
                    ImageId = sprite.ImageId,
                    PositionX = sprite.PositionX,
                    PositionY = sprite.PositionY
                });
            }

            return state;
        }
    }
}