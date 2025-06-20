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

    public SelectedCatalogImageWindow(ILogger<SelectedCatalogImageWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow, PendingEditorCatalogState pendingState) : base(renderWindow)
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
        if (!model.Catalogs.TryGetValue(catalogKey, out var existingCatalog))
        {
            ImGui.Text("Something went wrong, missing catalog");
            return;
        }
        
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
        
        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        ImGui.PushID("image");
        DrawImageWindow(model, catalog, image);
        ImGui.PopID();
    }

    private void DrawImageWindow(PackageModel model, CatalogModel catalog, SimpleImageModel image)
    {
        var windowSize = ImGui.GetContentRegionAvail();
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
                // if (catalog.Entries.ContainsKey(newKey))
                // {
                //     //TODO: error
                // }
                // else
                // {
                //     catalog.Entries.Remove(image.Key);
                //     catalog.ExtraData.Keys.Remove(image.Key);
                //     image.Key = newKey;
                //     catalog.Entries[newKey] = image;
                //     catalog.ExtraData.Keys.Add(newKey);
                //     //we also have the change the "selected" item
                //     _mainEditorState.SelectedItem = (MainEditorState.ItemType.CatalogImage, $"{catalog.Key}:{newKey}");
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

        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        DrawImageTabs(image, () => { _pendingState.RecordChange(); });
    }
}