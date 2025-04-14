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

    public SelectedCatalogImageWindow(ILogger<SelectedCatalogImageWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow) : base(renderWindow)
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

        ImGui.PushID("image");
        DrawImageWindow(model, catalog, image);
        ImGui.PopID();
    }

    private void DrawImageWindow(PackageModel model, CatalogModel catalog, SimpleImageModel image)
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
                if (catalog.Entries.ContainsKey(newKey))
                {
                    //TODO: error
                }
                else
                {
                    catalog.Entries.Remove(image.Key);
                    catalog.ExtraData.Keys.Remove(image.Key);
                    image.Key = newKey;
                    catalog.Entries[newKey] = image;
                    catalog.ExtraData.Keys.Add(newKey);
                    //we also have the change the "selected" item
                    _mainEditorState.SelectedItem = (MainEditorState.ItemType.CatalogImage, $"{catalog.Key}:{newKey}");
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