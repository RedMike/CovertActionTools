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
    private int _selectedDrawInstruction = 0;

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
        
        var i = 0;
        var selectedOverlayX = 0;
        var selectedOverlayY = 0;
        var selectedOverlayImageId = 0;
        var selectedOverlayImageIndex = 0;
        var selectedOverlayDelay = 0;
        var selectedOverlayDx = 0;
        var selectedOverlayDy = 0;
        var overlaysExistHaveImage = new Dictionary<int, (bool, bool)>();
        foreach (var drawInstruction in drawInstructions)
        {
            var ox = offsetX + drawInstruction.PositionX;
            var oy = offsetY + drawInstruction.PositionY;

            var isSelected = drawInstruction.Index == _selectedDrawInstruction;
            overlaysExistHaveImage[drawInstruction.Index] = (true, false);
            
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
            var iterations = 0;
            for (var index = 0; index < drawInstruction.Instructions.Count; index++)
            {
                iterations++;
                if (iterations > 2000)
                {
                    //safety check to prevent an infinite loop
                    break;
                }
                
                var instruction = drawInstruction.Instructions[index];
                if (instruction is AnimationModel.DelayInstruction delayInstruction)
                {
                    //delay sets up the amount of frames after which the next jump instruction will be skipped
                    delay = delayInstruction.Frames;
                }

                if (instruction is AnimationModel.ImageChangeInstruction imageChangeInstruction)
                {
                    imageId = imageChangeInstruction.Id;
                }

                if (instruction is AnimationModel.PositionChangeInstruction positionChangeInstruction)
                {
                    dx = positionChangeInstruction.PositionX;
                    dy = positionChangeInstruction.PositionY;
                }

                if (instruction is AnimationModel.JumpInstruction jumpInstruction)
                {
                    //the jump instruction will happen only until the number of frames from the last delay has passed
                    if (delay > 0)
                    {
                        delay -= 1;
                        ox += dx;
                        oy += dy;
                        
                        if (jumpInstruction.Null || index + jumpInstruction.IndexDelta < 0)
                        {
                            index -= 1;
                        }
                        else
                        {
                            index += jumpInstruction.IndexDelta;
                        }
                    }
                    
                    f += 1;
                    if (f > _selectedFrameId)
                    {
                        break;
                    }
                }
            }

            var imageIndex = animation.ExtraData.ImageIdToIndex.GetValueOrDefault(imageId, -1);
            
            if (isSelected)
            {
                selectedOverlayX = ox;
                selectedOverlayY = oy;
                selectedOverlayDelay = delay;
                selectedOverlayImageId = imageId;
                selectedOverlayImageIndex = imageIndex;
                selectedOverlayDx = dx;
                selectedOverlayDy = dy;
            }
            
            //TODO: frame number/delay
            if (!animation.Images.ContainsKey(imageIndex))
            {
                //TODO: what here?
                continue;
            }

            overlaysExistHaveImage[drawInstruction.Index] = (true, true);

            i++;
            var image = animation.Images[imageIndex];
            
            //draw an overlay
            if (isSelected)
            {
                ImGui.SetCursorPos(pos + new Vector2(ox - 6, oy - 6));
                var outlineTexture = RenderWindow.RenderOutlineRectangle(5, 
                    image.ExtraData.LegacyWidth + 12,
                    image.ExtraData.LegacyHeight + 12,
                    (255, 0, 200, 255));
                ImGui.Image(outlineTexture, new Vector2(image.ExtraData.LegacyWidth + 12, image.ExtraData.LegacyHeight + 12));
            }

            ImGui.SetCursorPos(pos + new Vector2(ox, oy));
            var id = $"image_{animation.Key}_frame_{_selectedFrameId}_{i}";
            //TODO: cache?
            var texture = RenderWindow.RenderImage(RenderWindow.RenderType.Image, id, 
                image.ExtraData.LegacyWidth, image.ExtraData.LegacyHeight, image.VgaImageData);
            ImGui.Image(texture, new Vector2(image.ExtraData.LegacyWidth, image.ExtraData.LegacyHeight));
        }

        //lastly draw the menu
        ImGui.SetCursorPos(pos + new Vector2(fullWidth + 10, 0));
        ImGui.BeginChild("menu", new Vector2(300, fullHeight), true);
        //make a list of strings with the existence state
        var strings = validIndexes.Select(x => $"{x} {overlaysExistHaveImage.GetValueOrDefault(x)}").ToList();
        var newDrawInstruction = ImGuiExtensions.Input("Sprite", _selectedDrawInstruction, 
            validIndexes,
            strings,
            width:200);
        if (newDrawInstruction != null)
        {
            _selectedDrawInstruction = newDrawInstruction.Value;
        }
        ImGui.Text($"Exists: {overlaysExistHaveImage.GetValueOrDefault(_selectedDrawInstruction).Item1}");
        ImGui.Text($"Image: {selectedOverlayImageId} => {selectedOverlayImageIndex} (exists {overlaysExistHaveImage.GetValueOrDefault(_selectedDrawInstruction).Item2})");
        ImGui.Text($"Velocity: ({selectedOverlayDx}, {selectedOverlayDy})");
        ImGui.Text($"Position: ({selectedOverlayX}, {selectedOverlayY})");
        ImGui.Text($"Delay: {selectedOverlayDelay}");
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
            } else if (record is AnimationModel.Unknown3Record unknown3)
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