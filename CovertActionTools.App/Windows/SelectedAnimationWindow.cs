using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using CovertActionTools.Core.Processors;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedAnimationWindow : SharedImageWindow
{
    private readonly ILogger<SelectedAnimationWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly IAnimationProcessor _animationProcessor;
    private readonly AnimationPreviewState _animationPreviewState;
    private readonly AnimationEditorState _animationEditorState;

    private int _selectedImage = 0;
    private int _selectedSprite = 0;

    public SelectedAnimationWindow(RenderWindow renderWindow, ILogger<SelectedAnimationWindow> logger, MainEditorState mainEditorState, IAnimationProcessor animationProcessor, AnimationPreviewState animationPreviewState, AnimationEditorState animationEditorState) : base(renderWindow)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _animationProcessor = animationProcessor;
        _animationPreviewState = animationPreviewState;
        _animationEditorState = animationEditorState;
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
        if (_animationPreviewState.SelectedId != key)
        {
            _animationPreviewState.Reset(key);
        }
        if (_animationEditorState.SelectedId != key)
        {
            _animationEditorState.Reset(key);
        }
        
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
        _animationEditorState.Update(animation);
        
        //TODO: keep a pending model and have a save button?
        ImGui.BeginTabBar("AnimationTabs", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton);
        
        if (ImGui.BeginTabItem("Images"))
        {
            DrawAnimationImageWindow(model, animation);
            
            ImGui.EndTabItem();
        }
        
        if (ImGui.BeginTabItem("Instructions"))
        {
            DrawAnimationInstructionsWindow(model, animation);    
            
            ImGui.EndTabItem();
        }
        
        if (ImGui.BeginTabItem("Preview"))
        {
            DrawAnimationPreviewWindow(model, animation);    
            
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawAnimationPreviewWindow(PackageModel model, AnimationModel animation)
    {
        var newFrameId = ImGuiExtensions.Input("Frame ID", _animationPreviewState.SelectedFrameId);
        if (newFrameId != null)
        {
            _animationPreviewState.SelectedFrameId = newFrameId.Value;
        }
        
        var inputRegisters = animation.ExtraData.Instructions
            .Where(x => x.Opcode == AnimationModel.AnimationInstruction.AnimationOpcode.PushRegisterToStack)
            .Select(x => x.Data[0])
            .Distinct()
            .Order()
            .ToList();
        var windowSize = ImGui.GetContentRegionAvail();
        var fieldWidth = ((int)windowSize.X / 2) - 100;
        foreach (var inputRegister in inputRegisters)
        {
            _animationPreviewState.InputRegisters.TryGetValue(inputRegister, out var val);
            var (oldValue, oldFrameIndex) = val;
            var newValue = ImGuiExtensions.Input($"Register {inputRegister}", oldValue, width:fieldWidth);
            if (newValue != null)
            {
                _animationPreviewState.SetInputRegister(inputRegister, newValue.Value, oldFrameIndex);
            }

            ImGui.SameLine();

            var newFrameIndex = ImGuiExtensions.Input($"Apply Frame {inputRegister}", oldFrameIndex, width:fieldWidth);
            if (newFrameIndex != null)
            {
                _animationPreviewState.SetInputRegister(inputRegister, _animationPreviewState.InputRegisters[inputRegister].value, newFrameIndex.Value);
            }
                
            ImGui.Text("");
        }
        
        var width = animation.ExtraData.BoundingWidth + 1;
        var height = animation.ExtraData.BoundingHeight + 1;
        var offsetX = 100;
        var offsetY = 100;
        var fullWidth = width + 2 * offsetX;
        var fullHeight = height + 2 * offsetY;
        
        var pos = ImGui.GetCursorPos();
        
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

        var state = _animationPreviewState.GetState(animation, _animationProcessor);
        foreach (var drawnImage in state.DrawnImages.OrderBy(x => x.SpriteIndex))
        {
            ImGui.SetCursorPos(pos + new Vector2(offsetX + drawnImage.PositionX, offsetY + drawnImage.PositionY));
            
            var drawnImageIndex = animation.ExtraData.ImageIdToIndex[drawnImage.ImageId];
            var drawnImageImg = animation.Images[drawnImageIndex];
            var id = $"image_{animation.Key}_{drawnImageIndex}";
            //TODO: cache?
            var texture = RenderWindow.RenderImage(RenderWindow.RenderType.Image, id,
                drawnImageImg.ExtraData.LegacyWidth, drawnImageImg.ExtraData.LegacyHeight, drawnImageImg.VgaImageData);
            ImGui.Image(texture, new Vector2(drawnImageImg.ExtraData.LegacyWidth, drawnImageImg.ExtraData.LegacyHeight));
        }

        var selectedSprite = state.Sprites.FirstOrDefault(x => x.Index == _selectedSprite);
        if (selectedSprite != null)
        {
            var (spriteX, spriteY) = state.GetSpritePosition(selectedSprite.Index);
            var ox = offsetX + spriteX;
            var oy = offsetY + spriteY;
            var w = 5;
            var h = 5;
            if (selectedSprite.ImageId >= 0 &&
                animation.ExtraData.ImageIdToIndex.TryGetValue(selectedSprite.ImageId, out var imageIndex) &&
                animation.Images.TryGetValue(imageIndex, out var image))
            {
                w = image.ExtraData.LegacyWidth;
                h = image.ExtraData.LegacyHeight;
            }
            
            //draw an overlay
            ImGui.SetCursorPos(pos + new Vector2(ox - 6, oy - 6));
            var outlineTexture = RenderWindow.RenderOutlineRectangle(5, 
                w + 12,
                h + 12,
                (255, 0, 200, 255));
            ImGui.Image(outlineTexture, new Vector2(w + 12, h + 12));
        }
        
        ImGui.SetCursorPos(pos + new Vector2(fullWidth + 10, 0));
        if (ImGui.BeginChild("menu", new Vector2(300, fullHeight), true))
        {
            ImGui.Text($"Stack ({state.Stack.Count}): {string.Join(" ", state.Stack.Select(x => $"{x}"))}");
            foreach (var pair in state.Registers)
            {
                ImGui.Text($"Register {pair.Key}: {pair.Value}");
            }
            
            ImGui.Text($"Instruction: {state.InstructionIndex}");
            ImGui.Text($"Wait: {state.FramesToWait}");
            if (ImGui.CollapsingHeader("Instruction View"))
            {
                foreach (var instructionIndex in state.LastFrameInstructionIndices)
                {
                    var instruction = animation.ExtraData.Instructions[instructionIndex];
                    ImGui.Text(GetInstructionText(instructionIndex, instruction));
                }
            }

            ImGui.Text("");

            var spriteIndexes = animation.ExtraData.Instructions
                .Where(x => x.Opcode == AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite)
                .Select(x => (int)x.StackParameters[0])
                .Distinct()
                .ToList();
            
            var newSelectedAnimation = ImGuiExtensions.Input("Sprite", _selectedSprite, spriteIndexes);
            if (newSelectedAnimation != null)
            {
                _selectedSprite = newSelectedAnimation.Value;
            }
            
            var sprite = state.Sprites.FirstOrDefault(x => x.Index == _selectedSprite);
            if (sprite != null)
            {
                if (sprite.ImageId >= 0)
                {
                    if (animation.ExtraData.ImageIdToIndex.TryGetValue(sprite.ImageId, out var imageIndex))
                    {
                        ImGui.Text($"Image: {sprite.ImageId} ({imageIndex})");
                    }
                    else
                    {
                        ImGui.Text($"Image: {sprite.ImageId} (not in index)");
                    }
                }
                else
                {
                    ImGui.Text($"Image: Hidden ({sprite.ImageId})");
                }

                ImGui.Text($"Active: {sprite.Active}");
                ImGui.Text($"Counter: {sprite.Counter} ({string.Join(" ", sprite.CounterStack)})");
                ImGui.Text($"Follow: ({sprite.FollowIndex})");
                var (x, y) = state.GetSpritePosition(sprite.Index);
                ImGui.Text($"Position: ({x}, {y})");
                ImGui.Text($"Step: {sprite.StepIndex}");
                    
            }
            else
            {
                ImGui.Text("Does not exist");
            }

            ImGui.Text("");

            if (ImGui.CollapsingHeader("Step View"))
            {
                if (sprite != null)
                {
                    foreach (var stepIndex in sprite.LastFrameStepIndices)
                    {
                        ImGui.Text(GetStepText(stepIndex, animation.ExtraData.Steps[stepIndex]));
                    }
                }
            }

            ImGui.EndChild();
        }
    }

    private void DrawAnimationInstructionsWindow(PackageModel model, AnimationModel animation)
    {
        if (_animationEditorState.HasChanges() || true)
        {
            if (ImGui.Button("Save"))
            {
                animation.ExtraData.ParseInstructionsAndSteps(_animationEditorState.SerialisedInstructions, _animationEditorState.SerialisedSteps);

                //mark it for reset so that the model gets updated
                _animationEditorState.Reset("");
            }
        }
        
        var windowSize = ImGui.GetContentRegionAvail();
        
        if (ImGui.CollapsingHeader("Instructions"))
        {
            ImGui.InputTextMultiline("InstructionsText", ref _animationEditorState.SerialisedInstructions, 32000, new Vector2(windowSize.X, 400), ImGuiInputTextFlags.AllowTabInput);
        }

        if (ImGui.CollapsingHeader("Steps"))
        {
            ImGui.InputTextMultiline("StepsText", ref _animationEditorState.SerialisedSteps, 32000, new Vector2(windowSize.X, 400), ImGuiInputTextFlags.AllowTabInput);
        }
    }

    private void DrawAnimationImageWindow(PackageModel model, AnimationModel animation)
    {
        if (!animation.ExtraData.ImageIdToIndex.TryGetValue(_selectedImage, out var index))
        {
            index = -2;
        }
        
        var imageIds = new List<int>();
        var imageIdNames = new List<string>();
        if (animation.ExtraData.BackgroundType == AnimationModel.BackgroundType.ClearToImage)
        {
            imageIds.Add(-1);
            imageIdNames.Add("Background");
        }

        var unselectedIds = new List<int>();
        if (!animation.Images.ContainsKey(-1))
        {
            unselectedIds.Add(-1);
        }
        for (var i = 0; i < 250; i++)
        {
            if (animation.ExtraData.ImageIdToIndex.TryGetValue(i, out var targetIndex))
            {
                var name = $"{i} - Index {targetIndex}";
                if (animation.Images.TryGetValue(targetIndex, out var targetImage))
                {
                    name += $" - {targetImage.ExtraData.Name} ({targetImage.ExtraData.LegacyWidth}x{targetImage.ExtraData.LegacyHeight})";
                }
                imageIds.Add(i);
                imageIdNames.Add(name);
            }
            else
            {
                unselectedIds.Add(i);
            }
        }

        var newSelectedId = ImGuiExtensions.Input("ID", _selectedImage, imageIds, imageIdNames);
        if (newSelectedId != null)
        {
            _selectedImage = newSelectedId.Value;
        }

        var replacementIds = new List<int>();
        replacementIds.Add(_selectedImage);
        replacementIds.AddRange(unselectedIds);
        var replacementIdNames = new List<string>();
        replacementIdNames.Add(" ");
        replacementIdNames.AddRange(unselectedIds.Select(x => $"{(x == -1 ? "Background" : x)}"));
        var replacementId = ImGuiExtensions.Input("Replacement ID", _selectedImage, replacementIds, replacementIdNames);
        if (replacementId != null)
        {
            //TODO: handle change
        }

        var selectedIndex = index;
        if (_selectedImage == -1)
        {
            selectedIndex = -1;
        }
        if (!animation.Images.TryGetValue(selectedIndex, out var image))
        {
            ImGui.Text("Something went wrong, image is missing..");
            return;
        }

        DrawImageTabs(image);
    }
    
    private string GetInstructionText(int index, AnimationModel.AnimationInstruction instruction)
    {
        var name = $"{index} - {instruction.Opcode}";
        if (instruction.Opcode == AnimationModel.AnimationInstruction.AnimationOpcode.ConditionalJump ||
            instruction.Opcode == AnimationModel.AnimationInstruction.AnimationOpcode.Jump)
        {
            name += $" {instruction.Label}";
        }
        else if (instruction.Opcode == AnimationModel.AnimationInstruction.AnimationOpcode.SetupSprite)
        {
            name += $" {instruction.DataLabel}";
        }
        else
        {
            if (instruction.Data.Length > 0)
            {
                name += $" {string.Join(" ", instruction.Data.Select(x => $"{x:X2}"))}";
            }
        }

        if (instruction.StackParameters.Length > 0)
        {
            name += $" {string.Join(" ", instruction.StackParameters.Select(x => $"{x}"))}";
        }

        return name;
    }

    private string GetStepText(int index, AnimationModel.AnimationStep step)
    {
        var name = $"{index} - {step.Type}";
        if (step.Type == AnimationModel.AnimationStep.StepType.JumpIfCounter)
        {
            name += $" {step.Label}";
        }
        else
        {
            if (step.Data.Length > 0)
            {
                name += $" {string.Join(" ", step.Data.Select(x => $"{x:X2}"))}";
            }
        }

        return name;
    }
}