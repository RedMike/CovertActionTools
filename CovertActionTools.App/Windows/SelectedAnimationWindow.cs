using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedAnimationWindow : SharedImageWindow
{
    public class AnimationState
    {
        //data
        public AnimationModel.SetupAnimationRecord Record { get; set; }
        
        public int Index { get; set; }
        public int ImageId { get; set; } = -1;
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        
        //flags
        public bool KeepDrawingOnceInactive { get; set; }
        
        //only if active
        public int Delay { get; set; }
        public int InstructionIndex { get; set; }
    }
    
    private readonly ILogger<SelectedAnimationWindow> _logger;
    private readonly MainEditorState _mainEditorState;

    private int _selectedImage = 0;
    private int _selectedFrameId = 0;
    private int _selectedAnimation = 0;

    public SelectedAnimationWindow(RenderWindow renderWindow, ILogger<SelectedAnimationWindow> logger, MainEditorState mainEditorState) : base(renderWindow)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
    }

    public override void Draw()
    {
        if (!_mainEditorState.IsPackageLoaded)
        {
            return;
        }

        if (_mainEditorState.SelectedItem == null ||
            _mainEditorState.SelectedItem.Value.type != MainEditorState.ItemType.Animation)
        {
            return;
        }
        
        var key = _mainEditorState.SelectedItem.Value.id;
        
        var screenSize = ImGui.GetMainViewport().Size;
        var initialPos = new Vector2(300.0f, 20.0f);
        var initialSize = new Vector2(screenSize.X - 300.0f, screenSize.Y - 200.0f);
        ImGui.SetNextWindowSize(initialSize);
        ImGui.SetNextWindowPos(initialPos);
        ImGui.Begin($"Animation", //TODO: change label but not ID to prevent unfocusing
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);

        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            if (model.Animations.TryGetValue(key, out var animation))
            {
                DrawAnimationWindow(model, animation);
            }
            else
            {
                ImGui.Text("Something went wrong, animation is missing..");
            }
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawAnimationWindow(PackageModel model, AnimationModel animation)
    {
        ImGui.Text("The PAN file format is not fully understood, so this is read-only and likely completely wrong.");
        ImGui.Text("");
        
        //TODO: keep a pending model and have a save button?
        var windowSize = ImGui.GetContentRegionAvail();

        ImGui.BeginTabBar("AnimationTabs", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton);

        if (ImGui.BeginTabItem("Preview"))
        {
            DrawAnimationPreviewWindow(model, animation);    
            
            ImGui.EndTabItem();
        }
        
        if (ImGui.BeginTabItem("Instructions"))
        {
            DrawAnimationInstructionsWindow(model, animation);    
            
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Images"))
        {
            DrawAnimationImageWindow(model, animation);
            
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawAnimationPreviewWindow(PackageModel model, AnimationModel animation)
    {
        var newFrameId = ImGuiExtensions.Input("Frame ID", _selectedFrameId);
        if (newFrameId != null)
        {
            _selectedFrameId = newFrameId.Value;
        }
        
        var width = animation.ExtraData.BoundingWidth + 1;
        var height = animation.ExtraData.BoundingHeight + 1;
        var offsetX = 100;
        var offsetY = 100;
        var fullWidth = width + 2 * offsetX;
        var fullHeight = height + 2 * offsetY;
        
        var pos = ImGui.GetCursorPos();
        
        var drawInstructions = animation.ExtraData.Records
            .Where(x => x.RecordType == AnimationModel.SetupRecord.SetupType.Animation)
            .Select(x => (AnimationModel.SetupAnimationRecord)x)
            .OrderBy(x => x.Index)
            .ToList();
        var validIndexes = drawInstructions
            .DistinctBy(x => x.Index)
            .Select(x => x.Index)
            .ToList();
        
        //draw the checkerboard first
        ImGui.SetCursorPos(pos);
        var bgTexture = RenderWindow.RenderCheckerboardRectangle(25, fullWidth, fullHeight,
            (40, 30, 40, 255), (50, 40, 50, 255));
        ImGui.Image(bgTexture, new Vector2(fullWidth, fullHeight));

        //now draw background
        ImGui.SetCursorPos(pos + new Vector2(offsetX, offsetY));
        if (animation.ExtraData.BackgroundType == AnimationModel.BackgroundType.ClearToColor)
        {
            var backgroundTexture = RenderWindow.RenderCheckerboardRectangle(100, width, height,
                Core.Constants.VgaColorMapping[animation.ExtraData.ClearColor],
                Core.Constants.VgaColorMapping[animation.ExtraData.ClearColor]);
            ImGui.Image(backgroundTexture, new Vector2(width, height));
        }
        else
        {
            var backgroundImage = animation.Images.OrderBy(x => x.Key).First().Value;
            var id = $"image_{animation.Key}_frame";
            //TODO: cache?
            var texture = RenderWindow.RenderImage(RenderWindow.RenderType.Image, id, 
                backgroundImage.ExtraData.LegacyWidth, backgroundImage.ExtraData.LegacyHeight, backgroundImage.VgaImageData);
            ImGui.Image(texture, new Vector2(backgroundImage.ExtraData.LegacyWidth, backgroundImage.ExtraData.LegacyHeight));
        }
        
        //now iterate over the setup records in order
        //as soon as a SetupAnimationRecord is handled, the animation is added as active
        //however the actual drawing happens once we hit instructions that wait frames/for animation ends
        //there are also records that mark animations to remain drawn after finishing (do not disappear) or other effects
        //when a frame is handled, every active animation record is handled at the same time
        //active means "simulate and run instructions for"
        //drawn means "draw the current known state of the animation every frame"
        var activeAnimations = new HashSet<int>();
        var drawnAnimations = new HashSet<int>();
        var animations = new Dictionary<int, AnimationState>();
        var frameNumber = 0;

        bool ProcessFrame(int frames)
        {
            var shouldStop = false;
            var framesProcessed = 0;
            while (framesProcessed < frames)
            {
                if (frameNumber >= _selectedFrameId)
                {
                    shouldStop = true;
                    break;
                }

                var activeAnimationsCopy = activeAnimations.ToList();
                foreach (var activeAnimationIndex in activeAnimationsCopy)
                {
                    var activeAnimation = animations[activeAnimationIndex];
                    var frameDone = false;
                    var iterations = 0; //for safety against infinite loops
                    while (!frameDone && (iterations++ < 2000))
                    {
                        if (activeAnimation.InstructionIndex < 0 || activeAnimation.InstructionIndex >= activeAnimation.Record.Instructions.Count)
                        {
                            _logger.LogError($"Animation {activeAnimationIndex} on invalid instruction index {activeAnimation.InstructionIndex}");
                            activeAnimations.Remove(activeAnimationIndex);
                            frameDone = true;
                            continue;
                        }

                        var activeInstruction = activeAnimation.Record.Instructions[activeAnimation.InstructionIndex];
                        if (activeInstruction is AnimationModel.DelayInstruction delay)
                        {
                            activeAnimation.Delay = delay.Frames;
                        }
                        else if (activeInstruction is AnimationModel.ImageChangeInstruction imageChange)
                        {
                            activeAnimation.ImageId = imageChange.Id;
                        }
                        else if (activeInstruction is AnimationModel.PositionChangeInstruction positionChange)
                        {
                            activeAnimation.PositionX += positionChange.PositionX;
                            activeAnimation.PositionY += positionChange.PositionY;
                        }
                        else if (activeInstruction is AnimationModel.JumpInstruction jump)
                        {
                            //TODO: a null jump instruction is an end?
                            if (jump.Null)
                            {
                                activeAnimations.Remove(activeAnimationIndex);
                                frameDone = true;
                                break;
                            }

                            //a jump instruction either jumps to another instruction and advances a frame 
                            //or is skipped over if delay is 0
                            if (activeAnimation.Delay != 0)
                            {
                                activeAnimation.Delay -= 1;
                                if (activeAnimation.Delay != 0)
                                {
                                    activeAnimation.InstructionIndex += jump.IndexDelta - 1; //one will be added later
                                }

                                frameDone = true;
                            }
                        } else if (activeInstruction is AnimationModel.Unknown8Instruction)
                        {
                            //reset to start
                            activeAnimation.InstructionIndex = 0;
                            frameDone = true;
                            continue;
                        }

                        activeAnimation.InstructionIndex += 1;

                        if (activeAnimation.InstructionIndex == activeAnimation.Record.Instructions.Count)
                        {
                            activeAnimations.Remove(activeAnimationIndex);
                            if (!activeAnimation.KeepDrawingOnceInactive)
                            {
                                drawnAnimations.Remove(activeAnimationIndex);
                            }

                            frameDone = true;
                            continue;
                        }
                    }
                }

                framesProcessed++;
                frameNumber++;
            }

            return shouldStop;
        }

        var recordIndex = 0;
        foreach (var record in animation.ExtraData.Records)
        {
            recordIndex++;
            if (record is AnimationModel.SetupAnimationRecord setupAnimation)
            {
                if (activeAnimations.Contains(setupAnimation.Index) || drawnAnimations.Contains(setupAnimation.Index))
                {
                    _logger.LogError($"Got animation setup for {setupAnimation.Index} but already previously set up");
                    continue;
                }

                var animationState = new AnimationState()
                {
                    Record = setupAnimation,
                    Index = setupAnimation.Index,
                    PositionX = setupAnimation.PositionX,
                    PositionY = setupAnimation.PositionY
                };
                animations[animationState.Index] = animationState;
                activeAnimations.Add(animationState.Index);
                drawnAnimations.Add(animationState.Index);
                continue;
            }
            if (record is AnimationModel.Instruction3Record instruction3)
            {
                if (instruction3.Type == AnimationModel.Instruction3Record.Instruction3Type.KeepDrawingAfterEnd)
                {
                    //prevent image disappearing once inactive
                    if (!animations.TryGetValue(instruction3.Data, out var animationState))
                    {
                        _logger.LogError($"Missing animation {instruction3.Data}");
                        continue;
                    }

                    animationState.KeepDrawingOnceInactive = true;
                    continue;
                }

                if (instruction3.Type == AnimationModel.Instruction3Record.Instruction3Type.WaitFrames)
                {
                    var shouldStop = ProcessFrame(instruction3.Data);

                    if (shouldStop)
                    {
                        break;
                    }
                }

                continue;
            }
            
            //_logger.LogWarning($"Unhandled record: {record.RecordType}");
        }

        while (frameNumber < _selectedFrameId)
        {
            var shouldStop = ProcessFrame(_selectedFrameId - frameNumber);

            if (shouldStop)
            {
                break;
            }
        }

        animations.TryGetValue(_selectedAnimation, out var selectedAnimation);
        
        foreach (var drawnAnimationIndex in drawnAnimations)
        {
            var drawnAnimation = animations[drawnAnimationIndex];
            
            if (!animation.ExtraData.ImageIdToIndex.TryGetValue(drawnAnimation.ImageId, out var imageIndex))
            {
                imageIndex = -1;
            }

            if (imageIndex < 0)
            {
                continue;
            }

            var ox = offsetX + drawnAnimation.PositionX;
            var oy = offsetY + drawnAnimation.PositionY;
            var image = animation.Images[imageIndex];
            
            //draw an overlay
            if (_selectedAnimation == drawnAnimationIndex)
            {
                ImGui.SetCursorPos(pos + new Vector2(ox - 6, oy - 6));
                var outlineTexture = RenderWindow.RenderOutlineRectangle(5, 
                    image.ExtraData.LegacyWidth + 12,
                    image.ExtraData.LegacyHeight + 12,
                    (255, 0, 200, 255));
                ImGui.Image(outlineTexture, new Vector2(image.ExtraData.LegacyWidth + 12, image.ExtraData.LegacyHeight + 12));
            }

            ImGui.SetCursorPos(pos + new Vector2(ox, oy));
            var id = $"image_{animation.Key}_frame_{_selectedFrameId}_{imageIndex}";
            //TODO: cache?
            var texture = RenderWindow.RenderImage(RenderWindow.RenderType.Image, id, 
                image.ExtraData.LegacyWidth, image.ExtraData.LegacyHeight, image.VgaImageData);
            ImGui.Image(texture, new Vector2(image.ExtraData.LegacyWidth, image.ExtraData.LegacyHeight));
        }

        //lastly draw the menu
        ImGui.SetCursorPos(pos + new Vector2(fullWidth + 10, 0));
        ImGui.BeginChild("menu", new Vector2(300, fullHeight), true);
        
        ImGui.Text($"Record: {recordIndex-1} ({animation.ExtraData.Records.Count})");
        
        //make a list of strings with the existence state
        var strings = validIndexes.Select(x => $"{x} {drawnAnimations.Contains(x)} {activeAnimations.Contains(x)}").ToList();
        var newDrawInstruction = ImGuiExtensions.Input("Animation", _selectedAnimation, 
            validIndexes,
            strings,
            width:200);
        if (newDrawInstruction != null)
        {
            _selectedAnimation = newDrawInstruction.Value;
        }

        if (selectedAnimation != null)
        {
            ImGui.Text($"Drawn: {drawnAnimations.Contains(_selectedAnimation)}");
            ImGui.Text($"Image: {selectedAnimation.ImageId} => {animation.ExtraData.ImageIdToIndex.GetValueOrDefault(selectedAnimation.ImageId, -1)}");
            ImGui.Text($"Position: ({selectedAnimation.PositionX}, {selectedAnimation.PositionY})");
            ImGui.Text($"Delay: {selectedAnimation.Delay}");
            ImGui.Text($"Instruction index: {selectedAnimation.InstructionIndex} ({selectedAnimation.Record.Instructions.Count})");
            ImGui.Text($"Keep drawing on end: {selectedAnimation.KeepDrawingOnceInactive}");
        }

        ImGui.EndChild();
    }
    
    private void DrawAnimationInstructionsWindow(PackageModel model, AnimationModel animation)
    {
        var id = 0;
        foreach (var record in animation.ExtraData.Records)
        {
            var name = $"{id++} - ";
            if (record is AnimationModel.SetupAnimationRecord setupAnimation)
            {
                name += $"Setup Animation {setupAnimation.Index} at ({setupAnimation.PositionX}, {setupAnimation.PositionY})";
            }
            else if (record is AnimationModel.UnknownRecord unknown)
            {
                name += $"Unknown {unknown.RecordType}: {string.Join(" ", unknown.Data.Select(x => $"{x:X2}"))}";
            } else if (record is AnimationModel.Instruction3Record unknown3)
            {
                name += $"{unknown3.RecordType}: {unknown3.Type} {unknown3.Data}";
            }
            else
            {
                name += $"Unknown record: {record.RecordType}";
            }

            if (ImGui.CollapsingHeader(name))
            {
                if (record is AnimationModel.SetupAnimationRecord setupAnimationRecord)
                {
                    ImGui.Text($"{setupAnimationRecord.Unknown1:X4} {setupAnimationRecord.Unknown2:X4}");

                    if (ImGui.BeginTable("instructions", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                    {
                        foreach (var instruction in setupAnimationRecord.Instructions)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.Text($"{instruction.Type}");

                            if (instruction is AnimationModel.DelayInstruction delay)
                            {
                                ImGui.TableNextColumn();
                                ImGui.Text($"{delay.Frames}");
                            }
                            else if (instruction is AnimationModel.ImageChangeInstruction imageChange)
                            {
                                ImGui.TableNextColumn();
                                ImGui.Text($"{imageChange.Id}");
                            }
                            else if (instruction is AnimationModel.PositionChangeInstruction positionChange)
                            {
                                ImGui.TableNextColumn();
                                ImGui.Text($"({positionChange.PositionX}, {positionChange.PositionY})");
                            }
                            else if (instruction is AnimationModel.JumpInstruction jump)
                            {
                                ImGui.TableNextColumn();
                                ImGui.Text($"Null: {jump.Null} Index: {jump.IndexDelta}");
                            }
                            else if (instruction is AnimationModel.UnknownInstruction unknown)
                            {
                                ImGui.TableNextColumn();
                                ImGui.Text($"{string.Join(" ", unknown.Data.Select(x => $"{x:X2}"))}");
                            }
                            else if (instruction is AnimationModel.Unknown8Instruction unknown8)
                            {
                                ImGui.TableNextColumn();
                            }
                            else if (instruction is AnimationModel.EndInstruction end)
                            {
                                ImGui.TableNextColumn();
                            }
                            else
                            {
                                ImGui.TableNextColumn();
                                ImGui.Text("Unknown record");
                            }
                        }

                        ImGui.EndTable();
                    }
                }
            }
        }
    }

    private void DrawAnimationImageWindow(PackageModel model, AnimationModel animation)
    {
        var newSelectedImage = ImGuiExtensions.Input("ID", _selectedImage);
        if (newSelectedImage != null)
        {
            _selectedImage = newSelectedImage.Value;
        }

        if (!animation.ExtraData.ImageIdToIndex.ContainsKey(_selectedImage))
        {
            ImGui.Text("ID is not mapped to an image");
            return;
        }

        if (!animation.Images.TryGetValue(animation.ExtraData.ImageIdToIndex[_selectedImage], out var image))
        {
            ImGui.Text("Something went wrong, image is missing..");
            return;
        }

        DrawImageTabs(image);
    }
}