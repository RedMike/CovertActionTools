using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class PackageExplorerWindow : BaseWindow
{
    private readonly ILogger<PackageExplorerWindow> _logger;
    private readonly MainEditorState _mainEditorState;

    public PackageExplorerWindow(ILogger<PackageExplorerWindow> logger, MainEditorState mainEditorState)
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

        var screenSize = ImGui.GetMainViewport().Size;
        var initialPos = new Vector2(0.0f, 20.0f);
        var initialSize = new Vector2(300.0f, screenSize.Y - 200.0f);
        ImGui.SetNextWindowSize(initialSize);
        ImGui.SetNextWindowPos(initialPos);
        ImGui.Begin("Package Explorer", 
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);

        if (_mainEditorState.LoadedPackage != null)
        {
            DrawTreeView(_mainEditorState.LoadedPackage);
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawTreeView(PackageModel model)
    {
        if (ImGui.TreeNodeEx("Images", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            foreach (var image in model.SimpleImages.OrderBy(x => x.Key))
            {
                var nodeFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth;
                if (_mainEditorState.SelectedItem != null &&
                    _mainEditorState.SelectedItem.Value.type == MainEditorState.ItemType.SimpleImage &&
                    _mainEditorState.SelectedItem.Value.id == image.Key)
                {
                    nodeFlags |= ImGuiTreeNodeFlags.Selected;
                }

                var name = $"{image.Key}";
                if (image.Value.ExtraData.Name != image.Key)
                {
                    name += $" ({image.Value.ExtraData.Name})";
                }
                if (ImGui.TreeNodeEx(name, nodeFlags))
                {
                    if (ImGui.IsItemClicked())
                    {
                        _mainEditorState.SelectedItem = (MainEditorState.ItemType.SimpleImage, image.Key);
                    }
                    
                    ImGui.TreePop();
                }
            }
            
            ImGui.TreePop();
        }
        
        if (ImGui.TreeNodeEx("Crimes", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            foreach (var crime in model.Crimes.OrderBy(x => x.Key))
            {
                var nodeFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth;
                if (_mainEditorState.SelectedItem != null &&
                    _mainEditorState.SelectedItem.Value.type == MainEditorState.ItemType.Crime &&
                    _mainEditorState.SelectedItem.Value.id == crime.Key.ToString())
                {
                    nodeFlags |= ImGuiTreeNodeFlags.Selected;
                }

                var name = $"{crime.Key} ({crime.Value.Participants.Count} p, {crime.Value.Events.Count} e, {crime.Value.Objects.Count} o)";
                if (ImGui.TreeNodeEx(name, nodeFlags))
                {
                    if (ImGui.IsItemClicked())
                    {
                        _mainEditorState.SelectedItem = (MainEditorState.ItemType.Crime, crime.Key.ToString());
                    }
                    
                    ImGui.TreePop();
                }
            }
            
            ImGui.TreePop();
        }
    }
}