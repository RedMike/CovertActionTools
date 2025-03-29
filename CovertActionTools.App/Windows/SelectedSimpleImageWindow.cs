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
        
        var origKey = image.Key;
        var key = origKey;
        ImGui.SetNextItemWidth(100.0f);
        ImGui.InputText("Key", ref key, 128, ImGuiInputTextFlags.CharsUppercase);
        if (key != origKey)
        {
            if (model.SimpleImages.ContainsKey(key))
            {
                ImGui.SameLine();
                ImGui.Text("Key already taken");
            }
            else
            {
                image.Key = key;
                model.SimpleImages.Remove(origKey);
                model.SimpleImages[image.Key] = image;
                //we also have the change the "selected" item
                _mainEditorState.SelectedItem = (MainEditorState.ItemType.SimpleImage, key);
            }
        }
        
        var origName = image.ExtraData.Name;
        var name = origName;
        ImGui.SetNextItemWidth(100.0f);
        ImGui.InputText("Name", ref name, 128, ImGuiInputTextFlags.None);
        if (name != origName)
        {
            image.ExtraData.Name = name;
        }

        var legacyWidth = image.Width;
        var origLegacyWidth = legacyWidth;
        ImGui.SetNextItemWidth(100.0f);
        ImGui.InputInt("Legacy Width", ref legacyWidth);
        if (legacyWidth != origLegacyWidth)
        {
            //TODO: resize? confirmation dialog?
        }
        
        ImGui.SameLine();
        
        var legacyHeight = image.Height;
        var origLegacyHeight = legacyHeight;
        ImGui.SetNextItemWidth(100.0f);
        ImGui.InputInt("Legacy Height", ref legacyHeight);
        if (legacyHeight != origLegacyHeight)
        {
            //TODO: resize? confirmation dialog?
        }
        
        var origComment = image.ExtraData.Comment;
        var comment = origComment;
        ImGui.InputTextMultiline("Comment", ref comment, 2048, new Vector2(400.0f, 50.0f), ImGuiInputTextFlags.None);
        if (comment != origComment)
        {
            image.ExtraData.Comment = comment;
        }

        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        ImGui.BeginTabBar("ImageTabs", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton);

#if MODERN_ENABLED
        if (ImGui.BeginTabItem("Modern"))
        {
            DrawModernImageTab(model, image);
            
            ImGui.EndTabItem();
        }
#endif
        
        if (ImGui.BeginTabItem("Legacy VGA"))
        {
            DrawVgaImageTab(model, image);
            
            ImGui.EndTabItem();
        }
        
        if (ImGui.BeginTabItem("Legacy CGA"))
        {
            //TODO: CGA
            ImGui.Text("TODO");
            
            ImGui.EndTabItem();
        }
        
        ImGui.EndTabBar();
    }

    private void DrawVgaImageTab(PackageModel model, SimpleImageModel image)
    {
        var width = image.Width;
        var height = image.Height;
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
#if MODERN_ENABLED
    private void DrawModernImageTab(PackageModel model, SimpleImageModel image)
    {
        ImGui.Text("Note: this image will only be used by a modern version, not the original game engine.");
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
#endif
}