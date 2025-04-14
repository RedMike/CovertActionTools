using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedCatalogImageWindow : BaseWindow
{
    private readonly ILogger<SelectedCatalogImageWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly RenderWindow _renderWindow;

    public SelectedCatalogImageWindow(ILogger<SelectedCatalogImageWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow)
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
        ImGui.Begin($"Catalog", //TODO: change label but not ID to prevent unfocusing
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);
        
        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            if (model.Catalogs.TryGetValue(catalogKey, out var catalog))
            {
                if (catalog.Entries.TryGetValue(imageId, out var image))
                {
                    DrawCatalogWindow(model, catalog, image);
                }
                else
                {
                    ImGui.Text("Something went wrong, image is missing in catalog..");    
                }
            }
            else
            {
                ImGui.Text("Something went wrong, catalog is missing..");
            }
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawCatalogWindow(PackageModel model, CatalogModel catalog, SimpleImageModel image)
    {
        var windowSize = ImGui.GetContentRegionAvail();
        
        //TODO: keep a pending model and have a save button?
        //first draw the catalog-specific info
        var newName = ImGuiExtensions.Input("Catalog Name", catalog.ExtraData.Name, 128);
        if (newName != null)
        {
            catalog.ExtraData.Name = newName;
        }

        var oldComment = catalog.ExtraData.Comment;
        var comment = catalog.ExtraData.Comment;
        ImGui.InputTextMultiline("Comment", ref comment, 2048, new Vector2(windowSize.X, 50.0f));
        if (comment != oldComment)
        {
            catalog.ExtraData.Comment = comment;
        }
        
        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        DrawImageWindow(model, catalog, image);
    }

    private void DrawImageWindow(PackageModel model, CatalogModel catalog, SimpleImageModel image)
    {
        var windowSize = ImGui.GetContentRegionAvail();
        
        
    }
}