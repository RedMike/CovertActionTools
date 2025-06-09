using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedPackageWindow : BaseWindow
{
    private readonly ILogger<SelectedPackageWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly PackageEditorState _packageEditorState;

    public SelectedPackageWindow(ILogger<SelectedPackageWindow> logger, MainEditorState mainEditorState, PackageEditorState packageEditorState)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _packageEditorState = packageEditorState;
    }

    public override void Draw()
    {
        if (!_mainEditorState.IsPackageLoaded)
        {
            return;
        }

        if (_mainEditorState.SelectedItem == null ||
            _mainEditorState.SelectedItem.Value.type != MainEditorState.ItemType.Package)
        {
            return;
        }
        
        var screenSize = ImGui.GetMainViewport().Size;
        var initialPos = new Vector2(300.0f, 20.0f);
        var initialSize = new Vector2(screenSize.X - 300.0f, screenSize.Y - 200.0f);
        ImGui.SetNextWindowSize(initialSize);
        ImGui.SetNextWindowPos(initialPos);
        ImGui.Begin("Package",
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);
        
        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            
            DrawPackageWindow(model);
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }
        
        ImGui.End();
    }

    private void DrawPackageWindow(PackageModel model)
    {
        DrawSharedMetadataEditor(model.Index.Metadata);

        var contentSize = ImGui.GetContentRegionAvail();
        
        if (string.IsNullOrEmpty(_packageEditorState.EditingVersion))
        {
            _packageEditorState.EditingVersion = model.Index.PackageVersion.ToString();
        }

        var currentEditingVersion = _packageEditorState.EditingVersion;
        var currentSavedVersion = model.Index.PackageVersion.ToString();
        ImGui.SetNextItemWidth(100.0f);
        ImGui.InputText("Package Version", ref currentEditingVersion, 64);
        if (currentEditingVersion != _packageEditorState.EditingVersion)
        {
            _packageEditorState.EditingVersion = currentEditingVersion;
        }

        if (currentEditingVersion != currentSavedVersion)
        {
            ImGui.SameLine();
            if (Version.TryParse(currentEditingVersion, out var parsedEditingVersion))
            {
                ImGui.SetNextItemWidth(50.0f);
                if (ImGui.Button("Apply"))
                {
                    model.Index.PackageVersion = parsedEditingVersion;
                    _packageEditorState.EditingVersion = parsedEditingVersion.ToString();
                }
            }
            else
            {
                ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), "Version should be in SemVer format, for example: 1.12.103");
            }
        }
        
        ImGui.Separator();
        ImGui.NewLine();

        if (ImGui.BeginTable("Diffs", 4, ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("Type");
            ImGui.TableSetupColumn("ID");
            ImGui.TableSetupColumn("Included?");
            ImGui.TableSetupColumn("Actions");
            ImGui.TableHeadersRow();
            
            DrawDiffsFonts(model);
            DrawDiffsImages(model);
            DrawDiffsCatalogs(model);
            DrawDiffsAnimations(model);
            DrawDiffsCrimes(model);
            DrawDiffsClues(model);
            DrawDiffsTexts(model);
            DrawDiffsPlots(model);
            DrawDiffsProse(model);
            DrawDiffsWorlds(model);
            ImGui.EndTable();
        }
        
        
    }

    private void DrawDiffsFonts(PackageModel model)
    {
        if (model.Index.FontChanges)
        {
            DrawDiffsRow("Fonts", "", model.Index.FontIncluded, (ch) =>
            {
                model.Index.FontIncluded = ch;
            });
        }
    }
    
    private void DrawDiffsImages(PackageModel model)
    {
        foreach (var key in model.Index.SimpleImageChanges)
        {
           DrawDiffsRow("Image", key, model.Index.SimpleImageIncluded.Contains(key), (ch) => {
               if (ch)
               {
                   model.Index.SimpleImageIncluded.Add(key);
               }
               else
               {
                   model.Index.SimpleImageIncluded.Remove(key);
               }
           });
        }
    }
    
    private void DrawDiffsCatalogs(PackageModel model)
    {
        foreach (var key in model.Index.CatalogChanges)
        {
            DrawDiffsRow("Catalog", key, model.Index.CatalogIncluded.Contains(key), (ch) => {
                if (ch)
                {
                    model.Index.CatalogIncluded.Add(key);
                }
                else
                {
                    model.Index.CatalogIncluded.Remove(key);
                }
            });
        }
    }
    
    private void DrawDiffsAnimations(PackageModel model)
    {
        foreach (var key in model.Index.AnimationChanges)
        {
            DrawDiffsRow("Animation", key, model.Index.AnimationIncluded.Contains(key), (ch) => {
                if (ch)
                {
                    model.Index.AnimationIncluded.Add(key);
                }
                else
                {
                    model.Index.AnimationIncluded.Remove(key);
                }
            });
        }
    }
    
    private void DrawDiffsCrimes(PackageModel model)
    {
        foreach (var key in model.Index.CrimeChanges)
        {
            DrawDiffsRow("Crime", key.ToString(), model.Index.CrimeIncluded.Contains(key), (ch) => {
                if (ch)
                {
                    model.Index.CrimeIncluded.Add(key);
                }
                else
                {
                    model.Index.CrimeIncluded.Remove(key);
                }
            });
        }
    }
    
    private void DrawDiffsClues(PackageModel model)
    {
        if (model.Index.ClueChanges)
        {
            DrawDiffsRow("Clues", "", model.Index.ClueIncluded, (ch) =>
            {
                model.Index.ClueIncluded = ch;
            });
        }
    }
    
    private void DrawDiffsTexts(PackageModel model)
    {
        if (model.Index.TextChanges)
        {
            DrawDiffsRow("Texts", "", model.Index.TextIncluded, (ch) =>
            {
                model.Index.TextIncluded = ch;
            });
        }
    }
    
    private void DrawDiffsPlots(PackageModel model)
    {
        if (model.Index.PlotChanges)
        {
            DrawDiffsRow("Plots", "", model.Index.PlotIncluded, (ch) =>
            {
                model.Index.PlotIncluded = ch;
            });
        }
    }
    
    private void DrawDiffsProse(PackageModel model)
    {
        if (model.Index.ProseChanges)
        {
            DrawDiffsRow("Prose", "", model.Index.ProseIncluded, (ch) =>
            {
                model.Index.ProseIncluded = ch;
            });
        }
    }
    
    private void DrawDiffsWorlds(PackageModel model)
    {
        foreach (var key in model.Index.WorldChanges)
        {
            DrawDiffsRow("World", key.ToString(), model.Index.WorldIncluded.Contains(key), (ch) => {
                if (ch)
                {
                    model.Index.WorldIncluded.Add(key);
                }
                else
                {
                    model.Index.WorldIncluded.Remove(key);
                }
            });
        }
    }

    private void DrawDiffsRow(string type, string id, bool included, Action<bool> callback)
    {
        ImGui.PushID($"row_{type}_{id}");
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(type);
        ImGui.TableNextColumn();
        ImGui.Text(id);
        ImGui.TableNextColumn();
        if (included)
        {
            ImGui.Text("YES");
        }
        else
        {
            ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), "NO ");
        }
        ImGui.TableNextColumn();
        if (ImGui.Button(included ? "DISABLE" : "ENABLE"))
        {
            callback(!included);
        }
        ImGui.PopID();
    }
}