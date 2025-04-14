using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedSimpleImageWindow : BaseWindow
{
    private readonly ILogger<SelectedSimpleImageWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly RenderWindow _renderWindow;

    public SelectedSimpleImageWindow(ILogger<SelectedSimpleImageWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _renderWindow = renderWindow;
    }

    public override void Draw()
    {
        if (!_mainEditorState.IsPackageLoaded)
        {
            return;
        }

        if (_mainEditorState.SelectedItem == null ||
            _mainEditorState.SelectedItem.Value.type != MainEditorState.ItemType.SimpleImage)
        {
            return;
        }

        var key = _mainEditorState.SelectedItem.Value.id;
        
        var screenSize = ImGui.GetMainViewport().Size;
        var initialPos = new Vector2(300.0f, 20.0f);
        var initialSize = new Vector2(screenSize.X - 300.0f, screenSize.Y - 200.0f);
        ImGui.SetNextWindowSize(initialSize);
        ImGui.SetNextWindowPos(initialPos);
        ImGui.Begin($"Image", //TODO: change label but not ID to prevent unfocusing
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);

        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            if (model.SimpleImages.TryGetValue(key, out var image))
            {
                DrawImageWindow(model, image);
            }
            else
            {
                ImGui.Text("Something went wrong, image is missing..");
            }
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawImageWindow(PackageModel model, SimpleImageModel image)
    {
        //TODO: keep a pending model and have a save button?
        if (ImGui.BeginTable("i_1", 4))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var newType = ImGuiExtensions.InputEnum("Type", image.ExtraData.Type, true, SimpleImageModel.ImageType.Unknown);
            if (newType != null)
            {
                image.ExtraData.Type = newType.Value;
            }

            ImGui.TableNextColumn();
            var newKey = ImGuiExtensions.Input("Key", image.Key, 128);
            if (newKey != null)
            {
                newKey = newKey.ToUpperInvariant();
                if (model.SimpleImages.ContainsKey(newKey))
                {
                    //TODO: error
                }
                else
                {
                    model.SimpleImages.Remove(image.Key);
                    image.Key = newKey;
                    model.SimpleImages[newKey] = image;
                    //we also have the change the "selected" item
                    _mainEditorState.SelectedItem = (MainEditorState.ItemType.SimpleImage, newKey);
                }
            }

            ImGui.TableNextColumn();
            var newWidth = ImGuiExtensions.Input("Legacy Width", image.ExtraData.LegacyWidth);
            if (newWidth != null)
            {
                //TODO: resize? confirmation dialog?
            }

            ImGui.TableNextColumn();
            var newHeight = ImGuiExtensions.Input("Legacy Height", image.ExtraData.LegacyHeight);
            if (newHeight != null)
            {
                //TODO: resize? confirmation dialog?
            }
            
            ImGui.EndTable();
        }

        var newName = ImGuiExtensions.Input("Name", image.ExtraData.Name, 128);
        if (newName != null)
        {
            image.ExtraData.Name = newName;
        }

        var windowSize = ImGui.GetContentRegionAvail();
        var origComment = image.ExtraData.Comment;
        var comment = origComment;
        ImGui.InputTextMultiline("Comment", ref comment, 2048, new Vector2(windowSize.X, 50.0f));
        if (comment != origComment)
        {
            image.ExtraData.Comment = comment;
        }

        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        ImGui.BeginTabBar("ImageTabs", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton);

        if (ImGui.BeginTabItem("Modern"))
        {
            DrawModernImageTab(model, image);
            
            ImGui.EndTabItem();
        }
        
        if (ImGui.BeginTabItem("Legacy VGA"))
        {
            DrawVgaImageTab(model, image);
            
            ImGui.EndTabItem();
        }
        
        if (ImGui.BeginTabItem("Legacy CGA"))
        {
            DrawCgaImageTab(model, image);
            
            ImGui.EndTabItem();
        }
        
        ImGui.EndTabBar();
    }

    private void DrawVgaImageTab(PackageModel model, SimpleImageModel image)
    {
        var width = image.ExtraData.LegacyWidth;
        var height = image.ExtraData.LegacyHeight;
        var rawPixels = image.VgaImageData;

        var pos = ImGui.GetCursorPos();
        var bgTexture = _renderWindow.RenderCheckerboardRectangle(25, width, height,
            (40, 30, 40, 255), (50, 40, 50, 255));
        ImGui.Image(bgTexture, new Vector2(width, height));

        ImGui.SetCursorPos(pos);
        var id = $"image_vga_{image.Key}";
        //TODO: cache?
        var texture = _renderWindow.RenderImage(RenderWindow.RenderType.Image, id, width, height, rawPixels);
        
        ImGui.Image(texture, new Vector2(width, height));
    }
    
    private void DrawCgaImageTab(PackageModel model, SimpleImageModel image)
    {
        if (image.ExtraData.LegacyColorMappings == null)
        {
            ImGui.Text("No CGA mapping, TODO");
            return;
        }
        
        var width = image.ExtraData.LegacyWidth;
        var height = image.ExtraData.LegacyHeight;
        var rawPixels = image.CgaImageData;

        var pos = ImGui.GetCursorPos();
        var bgTexture = _renderWindow.RenderCheckerboardRectangle(25, width, height,
            (40, 30, 40, 255), (50, 40, 50, 255));
        ImGui.Image(bgTexture, new Vector2(width, height));

        ImGui.SetCursorPos(pos);
        var id = $"image_cga_{image.Key}";
        //TODO: cache?
        var texture = _renderWindow.RenderImage(RenderWindow.RenderType.Image, id, width, height, rawPixels);
        
        ImGui.Image(texture, new Vector2(width, height));
        
        //TODO: allow changing CGA mapping
        //TODO: split CGA mapping into separate colour choices for each pixel
        for (byte i = 0; i < 8; i++)
        {
            var m = (int)image.ExtraData.LegacyColorMappings[i];
            ImGui.SetNextItemWidth(100.0f);
            ImGui.InputInt($"Color {i:00}", ref m);

            ImGui.SameLine();
            
            var j = (byte)(i + 8);
            var m2 = (int)image.ExtraData.LegacyColorMappings[j];
            ImGui.SetNextItemWidth(100.0f);
            ImGui.InputInt($"Color {j:00}", ref m2);
        }
    }
    
    private void DrawModernImageTab(PackageModel model, SimpleImageModel image)
    {
        ImGui.Text("Note: this image will not be shown in the original game engine.");
        var width = image.ExtraData.Width;
        var origWidth = width;
        ImGui.SetNextItemWidth(100.0f);
        ImGui.InputInt("Width", ref width);
        if (width != origWidth)
        {
            //TODO: resize? confirmation dialog?
        }
        
        ImGui.SameLine();
        
        var height = image.ExtraData.Height;
        var origHeight = height;
        ImGui.SetNextItemWidth(100.0f);
        ImGui.InputInt("Height", ref height);
        if (height != origHeight)
        {
            //TODO: resize? confirmation dialog?
        }

        ImGui.Text("");

        DrawModernImage(model, image);
        
        //TODO: 'generate VGA from this' button?
    }

    private void DrawModernImage(PackageModel model, SimpleImageModel image)
    {
        var width = image.ExtraData.Width;
        var height = image.ExtraData.Height;
        
        var pos = ImGui.GetCursorPos();
        var bgTexture = _renderWindow.RenderCheckerboardRectangle(25, width, height,
            (40, 30, 40, 255), (50, 40, 50, 255));
        ImGui.Image(bgTexture, new Vector2(width, height));

        ImGui.SetCursorPos(pos);
        
        var id = $"image_{image.Key}";
        var rawPixels = image.ModernImageData;
        //TODO: cache?
        var texture = _renderWindow.RenderImage(RenderWindow.RenderType.Image, id, width, height, rawPixels);
        
        ImGui.Image(texture, new Vector2(width, height));
    }
}