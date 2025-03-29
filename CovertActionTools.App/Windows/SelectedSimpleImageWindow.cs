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

    public SelectedSimpleImageWindow(ILogger<SelectedSimpleImageWindow> logger, MainEditorState mainEditorState)
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
    }
}