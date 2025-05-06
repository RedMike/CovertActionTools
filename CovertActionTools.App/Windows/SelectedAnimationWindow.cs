using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedAnimationWindow : SharedImageWindow
{
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

                ImGui.Text(name);
            
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
            
                var name = $"{index} - {step.Type}";
                if (step.Type == AnimationModel.AnimationStep.StepType.JumpAndReduceCounter)
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

                ImGui.Text(name);
            
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
}