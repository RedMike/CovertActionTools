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
    private readonly PendingEditorSimpleImageState _pendingState;

    public SelectedSimpleImageWindow(ILogger<SelectedSimpleImageWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow, PendingEditorSimpleImageState pendingState) : base(renderWindow)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _pendingState = pendingState;
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
        ImGui.Begin("Image",
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);

        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            if (model.SimpleImages.TryGetValue(key, out var _))
            {
                DrawImageWindow(model, key);
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

    private void DrawImageWindow(PackageModel model, string key)
    {
        SimpleImageModel image;
        if (_pendingState.Id != key)
        {
            image = model.SimpleImages[key];
            _pendingState.Reset(key, image);
        }
        else
        {
            if (_pendingState.PendingData == null)
            {
                return;
            }
            image = _pendingState.PendingData;
        }

        var windowSize = ImGui.GetContentRegionAvail();
        if (_pendingState.HasChanges && _pendingState.PendingData != null)
        {
            ImGui.SetNextItemWidth(windowSize.X);
            if (ImGui.Button("Save Changes"))
            {
                model.SimpleImages[image.Key] = _pendingState.PendingData;
                _pendingState.Reset(key, image);
                _mainEditorState.RecordChange();
                if (model.Index.SimpleImageChanges.Add(key))
                {
                    model.Index.SimpleImageIncluded.Add(key);
                }
            }
            ImGui.NewLine();
        }
        if (ImGui.BeginTable("i_1", 4))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var newType = ImGuiExtensions.InputEnum("Type", image.ExtraData.Type, true, SimpleImageModel.ImageType.Unknown);
            if (newType != null)
            {
                image.ExtraData.Type = newType.Value;
                _pendingState.RecordChange();
            }

            ImGui.TableNextColumn();
            var newKey = ImGuiExtensions.Input("Key", image.Key, 128, readOnly: true);
            if (newKey != null)
            {
                //not currently handled
                // newKey = newKey.ToUpperInvariant();
                // if (model.SimpleImages.ContainsKey(newKey))
                // {
                //     //TODO: error
                // }
                // else
                // {
                //     model.SimpleImages.Remove(image.Key);
                //     image.Key = newKey;
                //     model.SimpleImages[newKey] = image;
                //     //we also have the change the "selected" item
                //     _mainEditorState.SelectedItem = (MainEditorState.ItemType.SimpleImage, newKey);
                //     _pendingState.RecordChange();
                // }
            }

            ImGui.TableNextColumn();
            var newWidth = ImGuiExtensions.Input("Legacy Width", image.ExtraData.LegacyWidth);
            if (newWidth != null)
            {
                //TODO: resize? confirmation dialog?
                _pendingState.RecordChange();
            }

            ImGui.TableNextColumn();
            var newHeight = ImGuiExtensions.Input("Legacy Height", image.ExtraData.LegacyHeight);
            if (newHeight != null)
            {
                //TODO: resize? confirmation dialog?
                _pendingState.RecordChange();
            }
            
            ImGui.EndTable();
        }

        var newName = ImGuiExtensions.Input("Name", image.ExtraData.Name, 128);
        if (newName != null)
        {
            image.ExtraData.Name = newName;
            _pendingState.RecordChange();
        }

        var origComment = image.ExtraData.Comment;
        var comment = origComment;
        ImGui.InputTextMultiline("Comment", ref comment, 2048, new Vector2(windowSize.X, 50.0f));
        if (comment != origComment)
        {
            image.ExtraData.Comment = comment;
            _pendingState.RecordChange();
        }

        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        DrawImageTabs(image, () => { _pendingState.RecordChange(); });
    }
}