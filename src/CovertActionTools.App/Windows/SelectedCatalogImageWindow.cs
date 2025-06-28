using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedCatalogImageWindow : SharedImageWindow
{
    private readonly ILogger<SelectedCatalogImageWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly PendingEditorCatalogState _pendingState;

    public SelectedCatalogImageWindow(ILogger<SelectedCatalogImageWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow, PendingEditorCatalogState pendingState, ImageEditorState editorState) : base(renderWindow, editorState)
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
            _mainEditorState.SelectedItem.Value.type != MainEditorState.ItemType.CatalogImage)
        {
            return;
        }

        var rawId = _mainEditorState.SelectedItem.Value.id;
        var catalogKey = rawId.Substring(0, rawId.IndexOf(':'));
        var imageId = rawId.Substring(rawId.IndexOf(':') + 1);
        
        var screenSize = ImGui.GetMainViewport().Size;
        var initialPos = new Vector2(300.0f, 20.0f);
        var initialSize = new Vector2(screenSize.X - 300.0f, screenSize.Y - 200.0f);
        ImGui.SetNextWindowSize(initialSize);
        ImGui.SetNextWindowPos(initialPos);
        ImGui.Begin($"Catalog",
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);
        
        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            DrawCatalogWindow(model, catalogKey, imageId);
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawCatalogWindow(PackageModel model, string catalogKey, string imageId)
    {
        var catalog = ImGuiExtensions.PendingSaveChanges(_pendingState, catalogKey,
            () => model.Catalogs[catalogKey].Clone(),
            (data) =>
            {
                model.Catalogs[catalogKey] = data;
                _mainEditorState.RecordChange();
                if (model.Index.CatalogChanges.Add(catalogKey))
                {
                    model.Index.CatalogIncluded.Add(catalogKey);
                }
            });
        if (!catalog.Entries.TryGetValue(imageId, out var image))
        {
            ImGui.Text("Something went wrong, missing image in catalog");
            return;
        }
        
        //first draw the catalog-specific info
        DrawSharedMetadataEditor(catalog.Metadata, () => { _pendingState.RecordChange(); });

        ImGui.PushID("image");
        DrawImageWindow(model, catalog, imageId, image);
        ImGui.PopID();
    }

    private void DrawImageWindow(PackageModel model, CatalogModel catalog, string imageId, SharedImageModel image)
    {
        DrawImageTabs($"{catalog.Key}_{imageId}", image, () => { _pendingState.RecordChange(); }, null);
    }
}