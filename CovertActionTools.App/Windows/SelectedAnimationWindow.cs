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

    private int _selectedImage = 0;
    private int _selectedFrameId = 0;
    private int _selectedAnimation = 0;

    public SelectedAnimationWindow(RenderWindow renderWindow, ILogger<SelectedAnimationWindow> logger, MainEditorState mainEditorState, IAnimationProcessor animationProcessor) : base(renderWindow)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _animationProcessor = animationProcessor;
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

        var state = _animationProcessor.Process(animation, _selectedFrameId);
        foreach (var drawnImage in state.DrawnImages)
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

        if (state.Sprites.TryGetValue(_selectedAnimation, out var selectedSprite))
        {
            var ox = offsetX + selectedSprite.PositionX;
            var oy = offsetY + selectedSprite.PositionY;
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
        if (ImGui.BeginChild("menu", new Vector2(200, fullHeight), true))
        {
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
            
            var newSelectedAnimation = ImGuiExtensions.Input("Sprite", _selectedAnimation, spriteIndexes);
            if (newSelectedAnimation != null)
            {
                _selectedAnimation = newSelectedAnimation.Value;
            }
            
            if (state.Sprites.TryGetValue(_selectedAnimation, out var sprite))
            {
                if (sprite.ImageId >= 0)
                {
                    if (animation.ExtraData.ImageIdToIndex.TryGetValue(sprite.ImageId, out var imageIndex))
                    {
                        ImGui.Text($"Image: {sprite.ImageId} ({imageIndex})");
                    }
                }
                else
                {
                    ImGui.Text($"Image: Hidden ({sprite.ImageId})");
                }

                ImGui.Text($"Active: {sprite.Active} Counter: {sprite.Counter}");
                ImGui.Text($"Position: ({sprite.PositionX}, {sprite.PositionY})");
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
        if (ImGui.CollapsingHeader("Instructions"))
        {
            var index = 0;
            foreach (var instruction in animation.ExtraData.Instructions)
            {
                var labelsOnIndex = animation.ExtraData.InstructionLabels
                    .Where(x => x.Value == index)
                    .Select(x => x.Key)
                    .ToList();
                if (labelsOnIndex.Count > 0)
                {
                    foreach (var label in labelsOnIndex)
                    {
                        ImGui.Text($"{label}:");
                    }
                }
                
                ImGui.Text(GetInstructionText(index, instruction));
            
                index++;
            }
        }

        if (ImGui.CollapsingHeader("Steps"))
        {
            var index = 0;
            foreach (var step in animation.ExtraData.Steps)
            {
                var labelsOnIndex = animation.ExtraData.DataLabels
                    .Where(x => x.Value == index)
                    .Select(x => x.Key)
                    .ToList();
                if (labelsOnIndex.Count > 0)
                {
                    foreach (var label in labelsOnIndex)
                    {
                        ImGui.Text($"{label}:");
                    }
                }
            
                ImGui.Text(GetStepText(index, step));
            
                index++;
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
    
    private string GetInstructionText(int index, AnimationModel.AnimationInstruction instruction)
    {
        var name = $"{index} - {instruction.Opcode}";
        if (instruction.Opcode == AnimationModel.AnimationInstruction.AnimationOpcode.Jump12 ||
            instruction.Opcode == AnimationModel.AnimationInstruction.AnimationOpcode.Jump13)
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
        if (step.Type == AnimationModel.AnimationStep.StepType.Jump)
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