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
        var packageName = "Package";
        if (!string.IsNullOrEmpty(model.Index.Metadata.Name))
        {
            packageName = model.Index.Metadata.Name;
        }

        var flags = ImGuiTreeNodeFlags.DefaultOpen |
                    ImGuiTreeNodeFlags.SpanFullWidth |
                    ImGuiTreeNodeFlags.OpenOnDoubleClick |
                    ImGuiTreeNodeFlags.OpenOnArrow;
        if (ImGui.TreeNodeEx(packageName, flags))
        {
            if (ImGui.IsItemClicked())
            {
                _mainEditorState.SelectedItem = (MainEditorState.ItemType.Package, string.Empty);
            }
            
            DrawFontsView(model);
            DrawImagesView(model);
            DrawCatalogsView(model);
            DrawAnimationsView(model);
            DrawCrimesView(model);
            DrawCluesView(model);
            DrawTextsView(model);
            DrawPlotsView(model);
            DrawProseView(model);
            DrawWorldsView(model);

            ImGui.TreePop();
        }
    }

    private void DrawImagesView(PackageModel model)
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
    }

    private void DrawCrimesView(PackageModel model)
    {
        if (ImGui.TreeNodeEx("Crimes", ImGuiTreeNodeFlags.SpanAvailWidth))
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

                var name =
                    $"Crime {crime.Key} ({crime.Value.Participants.Count} p, {crime.Value.Events.Count} e, {crime.Value.Objects.Count} o)";
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

    private void DrawTextsView(PackageModel model)
    {
        if (ImGui.TreeNodeEx("Texts", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            var types = Enum.GetValues<TextModel.StringType>()
                .Where(x => x != TextModel.StringType.Unknown)
                .ToList();

            foreach (var textType in types)
            {
                var secondaryIds = new List<string>() { "" };
                if (textType == TextModel.StringType.CrimeMessage)
                {
                    secondaryIds = model.Crimes.Keys.OrderBy(x => x).Select(x => $"{x}").ToList();
                }

                foreach (var secondaryId in secondaryIds)
                {
                    var textTypeString = $"{textType}";
                    if (textType == TextModel.StringType.CrimeMessage)
                    {
                        textTypeString += $" {secondaryId}";
                    }

                    var texts = model.Texts.Values
                        .Where(x => x.Type == textType &&
                                    (string.IsNullOrEmpty(secondaryId) || int.Parse(secondaryId) == x.CrimeId)
                        )
                        .OrderBy(x => x.Id)
                        .ThenBy(x => x.CrimeId)
                        .ToList();

                    var nodeFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth;
                    if (_mainEditorState.SelectedItem != null &&
                        _mainEditorState.SelectedItem.Value.type == MainEditorState.ItemType.Text &&
                        _mainEditorState.SelectedItem.Value.id == textTypeString)
                    {
                        nodeFlags |= ImGuiTreeNodeFlags.Selected;
                    }

                    var name = $"{textTypeString} ({texts.Count})";
                    if (ImGui.TreeNodeEx(name, nodeFlags))
                    {
                        if (ImGui.IsItemClicked())
                        {
                            _mainEditorState.SelectedItem = (MainEditorState.ItemType.Text, textTypeString);
                        }

                        ImGui.TreePop();
                    }
                }
            }

            ImGui.TreePop();
        }
    }

    private void DrawCluesView(PackageModel model)
    {
        if (ImGui.TreeNodeEx("Clues", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            var crimeIds = model.Clues.Values.Select(x => x.CrimeId).Distinct().OrderBy(x => x ?? int.MinValue)
                .ToList();
            foreach (var crimeId in crimeIds)
            {
                var crimeString = "Any Crime";
                var id = "any";
                if (crimeId != null)
                {
                    crimeString = $"Crime {crimeId}";
                    id = $"{crimeId}";
                }

                var nodeFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth;
                if (_mainEditorState.SelectedItem != null &&
                    _mainEditorState.SelectedItem.Value.type == MainEditorState.ItemType.Clue &&
                    _mainEditorState.SelectedItem.Value.id == id)
                {
                    nodeFlags |= ImGuiTreeNodeFlags.Selected;
                }

                var name = $"{crimeString} ({model.Clues.Count(x => x.Value.CrimeId == crimeId)})";
                if (ImGui.TreeNodeEx(name, nodeFlags))
                {
                    if (ImGui.IsItemClicked())
                    {
                        _mainEditorState.SelectedItem = (MainEditorState.ItemType.Clue, id);
                    }

                    ImGui.TreePop();
                }
            }

            ImGui.TreePop();
        }
    }

    private void DrawPlotsView(PackageModel model)
    {
        if (ImGui.TreeNodeEx("Plots", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            var missionSetIds = model.Plots.Values.Select(x => x.MissionSetId)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
            foreach (var missionSetId in missionSetIds)
            {
                var nodeFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth;
                if (_mainEditorState.SelectedItem != null &&
                    _mainEditorState.SelectedItem.Value.type == MainEditorState.ItemType.Plot &&
                    _mainEditorState.SelectedItem.Value.id == missionSetId.ToString())
                {
                    nodeFlags |= ImGuiTreeNodeFlags.Selected;
                }

                var name = $"Mission Set {missionSetId}";
                if (ImGui.TreeNodeEx(name, nodeFlags))
                {
                    if (ImGui.IsItemClicked())
                    {
                        _mainEditorState.SelectedItem = (MainEditorState.ItemType.Plot, missionSetId.ToString());
                    }

                    ImGui.TreePop();
                }
            }

            ImGui.TreePop();
        }
    }

    private void DrawWorldsView(PackageModel model)
    {
        if (ImGui.TreeNodeEx("Worlds", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            foreach (var worldId in model.Worlds.Keys)
            {
                var nodeFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth;
                if (_mainEditorState.SelectedItem != null &&
                    _mainEditorState.SelectedItem.Value.type == MainEditorState.ItemType.World &&
                    _mainEditorState.SelectedItem.Value.id == worldId.ToString())
                {
                    nodeFlags |= ImGuiTreeNodeFlags.Selected;
                }

                var name = $"World {worldId}";
                if (ImGui.TreeNodeEx(name, nodeFlags))
                {
                    if (ImGui.IsItemClicked())
                    {
                        _mainEditorState.SelectedItem = (MainEditorState.ItemType.World, worldId.ToString());
                    }

                    ImGui.TreePop();
                }
            }

            ImGui.TreePop();
        }
    }

    private void DrawCatalogsView(PackageModel model)
    {
        if (ImGui.TreeNodeEx("Catalog Images", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            foreach (var catalogKey in model.Catalogs.Keys.OrderBy(x => x))
            {
                var catalog = model.Catalogs[catalogKey];
                var catalogName = $"{catalogKey}";
                if (!string.IsNullOrEmpty(catalog.ExtraData.Name) && catalogKey != catalog.ExtraData.Name)
                {
                    catalogName += $" ({catalog.ExtraData.Name})";
                }

                if (ImGui.TreeNodeEx(catalogName, ImGuiTreeNodeFlags.SpanAvailWidth))
                {
                    foreach (var entryKey in catalog.ExtraData.Keys)
                    {
                        var entry = catalog.Entries[entryKey];
                        var nodeFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth;
                        if (_mainEditorState.SelectedItem != null &&
                            _mainEditorState.SelectedItem.Value.type == MainEditorState.ItemType.CatalogImage &&
                            _mainEditorState.SelectedItem.Value.id == $"{catalogKey}:{entryKey}")
                        {
                            nodeFlags |= ImGuiTreeNodeFlags.Selected;
                        }

                        var name = $"{entryKey}";
                        if (!string.IsNullOrEmpty(entry.ExtraData.Name) && entry.ExtraData.Name != entryKey)
                        {
                            name += $" ({entry.ExtraData.Name})";
                        }

                        if (ImGui.TreeNodeEx(name, nodeFlags))
                        {
                            if (ImGui.IsItemClicked())
                            {
                                _mainEditorState.SelectedItem = (MainEditorState.ItemType.CatalogImage,
                                    $"{catalogKey}:{entryKey}");
                            }

                            ImGui.TreePop();
                        }
                    }

                    ImGui.TreePop();
                }
            }

            ImGui.TreePop();
        }
    }

    private void DrawAnimationsView(PackageModel model)
    {
        if (ImGui.TreeNodeEx("Animations", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            foreach (var animationKey in model.Animations.Keys.OrderBy(x => x))
            {
                var animation = model.Animations[animationKey];
                var animationName = $"{animationKey}";
                if (!string.IsNullOrEmpty(animation.Metadata.Name) && animationKey != animation.Metadata.Name)
                {
                    animationName += $" ({animation.Metadata.Name})";
                }

                var nodeFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth;
                if (_mainEditorState.SelectedItem != null &&
                    _mainEditorState.SelectedItem.Value.type == MainEditorState.ItemType.Animation &&
                    _mainEditorState.SelectedItem.Value.id == animationKey)
                {
                    nodeFlags |= ImGuiTreeNodeFlags.Selected;
                }

                if (ImGui.TreeNodeEx(animationName, nodeFlags))
                {
                    if (ImGui.IsItemClicked())
                    {
                        _mainEditorState.SelectedItem = (MainEditorState.ItemType.Animation, animationKey);
                    }

                    ImGui.TreePop();
                }
            }

            ImGui.TreePop();
        }
    }

    private void DrawFontsView(PackageModel model)
    {
        if (ImGui.TreeNodeEx("Fonts", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            foreach (var fontId in model.Fonts.ExtraData.Fonts.Keys.OrderBy(x => x))
            {
                var nodeFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth;
                if (_mainEditorState.SelectedItem != null &&
                    _mainEditorState.SelectedItem.Value.type == MainEditorState.ItemType.Font &&
                    _mainEditorState.SelectedItem.Value.id == fontId.ToString())
                {
                    nodeFlags |= ImGuiTreeNodeFlags.Selected;
                }

                if (ImGui.TreeNodeEx($"{fontId}", nodeFlags))
                {
                    if (ImGui.IsItemClicked())
                    {
                        _mainEditorState.SelectedItem = (MainEditorState.ItemType.Font, fontId.ToString());
                    }

                    ImGui.TreePop();
                }
            }

            ImGui.TreePop();
        }
    }

    private void DrawProseView(PackageModel model)
    {
        if (ImGui.TreeNodeEx("Prose", ImGuiTreeNodeFlags.SpanAvailWidth))
        {
            foreach (var proseKey in model.Prose.Keys.OrderBy(x => x))
            {
                var nodeFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth;
                if (_mainEditorState.SelectedItem != null &&
                    _mainEditorState.SelectedItem.Value.type == MainEditorState.ItemType.Prose &&
                    _mainEditorState.SelectedItem.Value.id == proseKey)
                {
                    nodeFlags |= ImGuiTreeNodeFlags.Selected;
                }

                if (ImGui.TreeNodeEx($"{proseKey}", nodeFlags))
                {
                    if (ImGui.IsItemClicked())
                    {
                        _mainEditorState.SelectedItem = (MainEditorState.ItemType.Prose, proseKey);
                    }

                    ImGui.TreePop();
                }
            }
            
            ImGui.TreePop();
        }
    }
}