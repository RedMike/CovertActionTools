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
        
        var drawInstructions = animation.ExtraData.Records
            .Where(x => x.Type == AnimationModel.SetupRecord.SetupType.Animation)
            .Select(x => (AnimationModel.SetupAnimationRecord)x)
            .OrderBy(x => x.Index)
            .ToList();
        
        //draw the checkerboard first
        var pos = ImGui.GetCursorPos();
        var bgTexture = RenderWindow.RenderCheckerboardRectangle(25, fullWidth, fullHeight,
            (40, 30, 40, 255), (50, 40, 50, 255));
        ImGui.Image(bgTexture, new Vector2(fullWidth, fullHeight));

        //now draw background
        ImGui.SetCursorPos(pos + new Vector2(offsetX, offsetY));
        if (animation.ExtraData.BackgroundType == AnimationModel.BackgroundType.ClearToColor)
        {
            var backgroundTexture = RenderWindow.RenderCheckerboardRectangle(100, width, height,
                Core.Constants.VgaColorMapping[animation.ExtraData.HeaderData[0]],
                Core.Constants.VgaColorMapping[animation.ExtraData.HeaderData[0]]);
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
        
        var i = 0;
        foreach (var drawInstruction in drawInstructions)
        {
            var ox = offsetX + drawInstruction.PositionX;
            var oy = offsetY + drawInstruction.PositionY;
            
            if (drawInstruction.Instructions.All(x => x.Type != AnimationModel.InstructionRecord.InstructionType.ImageChange))
            {
                //don't know what the image should be
                continue;
            }

            var f = 0;
            var delay = 0;
            var imageId = -1;
            var dx = 0;
            var dy = 0;
            var x = 0;
            var y = 0;
            foreach (var instruction in drawInstruction.Instructions)
            {
                if (instruction is AnimationModel.DelayInstruction delayInstruction)
                {
                    if (delay != 0)
                    {
                        var framesToWait = delay;
                        if (f + framesToWait > _selectedFrameId)
                        {
                            framesToWait = _selectedFrameId - f + 1;
                        }
                        f += framesToWait;
                        x += dx * framesToWait;
                        y += dy * framesToWait;
                        //TODO: image change?
                        if (f > _selectedFrameId)
                        {
                            break;
                        }
                    }
                    delay = delayInstruction.Frames;
                }

                if (instruction is AnimationModel.ImageChangeInstruction imageChangeInstruction)
                {
                    imageId = imageChangeInstruction.Id - 1;
                }

                if (instruction is AnimationModel.PositionChangeInstruction positionChangeInstruction)
                {
                    dx = positionChangeInstruction.PositionX;
                    dy = positionChangeInstruction.PositionY;
                }
            }
            
            if (delay != 0)
            {
                var framesToWait = delay;
                if (f + framesToWait > _selectedFrameId)
                {
                    framesToWait = _selectedFrameId - f + 1;
                }
                x += dx * framesToWait;
                y += dy * framesToWait;
                //TODO: image change?
            }

            //TODO: frame number/delay
            if (!animation.Images.ContainsKey(imageId))
            {
                //TODO: what here?
                continue;
            }

            i++;

            ImGui.SetCursorPos(pos + new Vector2(ox + x, oy + y));
            var image = animation.Images[imageId];
            var id = $"image_{animation.Key}_frame_{_selectedFrameId}_{i}";
            //TODO: cache?
            var texture = RenderWindow.RenderImage(RenderWindow.RenderType.Image, id, 
                image.ExtraData.LegacyWidth, image.ExtraData.LegacyHeight, image.VgaImageData);
            ImGui.Image(texture, new Vector2(image.ExtraData.LegacyWidth, image.ExtraData.LegacyHeight));
        }
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
                name += $"Unknown {unknown.Type}: {string.Join(" ", unknown.Data.Select(x => $"{x:X2}"))}";
            }
            else
            {
                name += $"Unknown record: {record.Type}";
            }

            if (ImGui.CollapsingHeader(name))
            {
                if (!(record is AnimationModel.SetupAnimationRecord))
                {
                    continue;
                }
                
                
            }
        }
        
        var drawInstructions = animation.ExtraData.Records
            .Where(x => x.Type == AnimationModel.SetupRecord.SetupType.Animation)
            .Select(x => (AnimationModel.SetupAnimationRecord)x)
            .OrderBy(x => x.Index)
            .ToList();

        foreach (var instruction in drawInstructions)
        {
            var debugText = instruction.Instructions
                .Where(x => x.Type == AnimationModel.InstructionRecord.InstructionType.Delay ||
                            x.Type == AnimationModel.InstructionRecord.InstructionType.ImageChange ||
                            x.Type == AnimationModel.InstructionRecord.InstructionType.PositionChange)
                .Select(x => $"{x.Type}")
                .ToList();
            ImGui.Text($"Draw ({instruction.PositionX}, {instruction.PositionY}): {string.Join(", ", debugText)}");
        }
    }

    private void DrawAnimationImageWindow(PackageModel model, AnimationModel animation)
    {
        var newSelectedImage = ImGuiExtensions.Input("Image", _selectedImage);
        if (newSelectedImage != null)
        {
            _selectedImage = newSelectedImage.Value;
        }

        if (!animation.Images.TryGetValue(_selectedImage, out var image))
        {
            ImGui.Text("Something went wrong, image is missing..");
            return;
        }

        DrawImageTabs(image);
    }
}