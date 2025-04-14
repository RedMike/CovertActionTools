using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedSimpleImageWindow : SharedImageWindow
{
    private readonly ILogger<SelectedSimpleImageWindow> _logger;
    private readonly MainEditorState _mainEditorState;

    public SelectedSimpleImageWindow(ILogger<SelectedSimpleImageWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow) : base(renderWindow)
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
        var windowSize = ImGui.GetContentRegionAvail();
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

        DrawImageTabs(image);
    }
}