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

    public SelectedSimpleImageWindow(ILogger<SelectedSimpleImageWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow, PendingEditorSimpleImageState pendingState, ImageEditorState editorState) : base(renderWindow, editorState)
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
            DrawImageWindow(model, key);
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawImageWindow(PackageModel model, string key)
    {
        if (!model.SimpleImages.ContainsKey(key))
        {
            ImGui.Text("Something went wrong, missing image");
            return;
        }
        var image = ImGuiExtensions.PendingSaveChanges(_pendingState, key,
            () => model.SimpleImages[key].Clone(),
            (data) =>
            {
                model.SimpleImages[key] = data;
                _mainEditorState.RecordChange();
                if (model.Index.SimpleImageChanges.Add(key))
                {
                    model.Index.SimpleImageIncluded.Add(key);
                }
            });
        
        DrawSharedMetadataEditor(image.Metadata, () => { _pendingState.RecordChange(); });
        
        DrawImageTabs(key, image.Image, () => { _pendingState.RecordChange(); }, (enabled) =>
        {
            if (enabled)
            {
                image.SpriteSheet = new SimpleImageModel.SpriteSheetData()
                {
                    Sprites = new Dictionary<string, SimpleImageModel.Sprite>()
                };
            }
            else
            {
                image.SpriteSheet = null;
            }

            _pendingState.RecordChange();
        }, spriteSheet: image.SpriteSheet);
    }
}