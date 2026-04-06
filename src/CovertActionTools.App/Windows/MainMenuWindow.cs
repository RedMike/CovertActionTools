using System.Diagnostics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Exporting;
using CovertActionTools.Core.Importing;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class MainMenuWindow : BaseWindow
{
    private readonly ILogger<MainMenuWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly ParsePublishedState _parsePublishedState;
    private readonly LoadPackageState _loadPackageState;
    private readonly SavePackageState _savePackageState;
    private readonly EditorSettingsState _editorSettingsState;
    private readonly ConfirmDialogueState _confirmDialogueState;
    private readonly PublishPackageState _publishPackageState;
    private bool _showStringTagsHelp;

    public MainMenuWindow(ILogger<MainMenuWindow> logger, MainEditorState mainEditorState, ParsePublishedState parsePublishedState, LoadPackageState loadPackageState, SavePackageState savePackageState, EditorSettingsState editorSettingsState, ConfirmDialogueState confirmDialogueState, PublishPackageState publishPackageState)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _parsePublishedState = parsePublishedState;
        _loadPackageState = loadPackageState;
        _savePackageState = savePackageState;
        _editorSettingsState = editorSettingsState;
        _confirmDialogueState = confirmDialogueState;
        _publishPackageState = publishPackageState;
    }

    public override void Draw()
    {
        if (_parsePublishedState.Show || _confirmDialogueState.Show)
        {
            return;
        }
        
        ImGui.BeginMainMenuBar();
        if (_mainEditorState.IsPackageLoaded)
        {
            DrawLoadedMenus();
            DrawLoadedInfo();
        }
        else
        {
            DrawNotLoadedMenu();
        }
        DrawHelpMenu();
        ImGui.EndMainMenuBar();

        if (_showStringTagsHelp)
        {
            DrawStringTagsWindow();
        }
    }

    private void DrawLoadedMenus()
    {
        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("Close Package"))
            {
                if (_mainEditorState.HasChanges)
                {
                    _confirmDialogueState.ShowDialog([
                        "You have unsaved changes, are you sure?"
                    ], (c) =>
                    {
                        if (c)
                        {
                            _mainEditorState.UnloadPackage();
                        }
                    });
                }
                else
                {
                    _mainEditorState.UnloadPackage();
                }
            }
            
            if (ImGui.MenuItem("Save Package"))
            {
                SavePackage(_mainEditorState.LoadedPackagePath!, true);
            }
            
            if (ImGui.MenuItem("Save Package As.."))
            {
                SavePackage(_mainEditorState.LoadedPackagePath!, false);
            }
            
            if (ImGui.MenuItem("Publish Package"))
            {
                PublishPackage();
            }
            
            ImGui.EndMenu();
        }

        if (_mainEditorState.HasChanges)
        {
            if (ImGui.MenuItem("Save Package"))
            {
                SavePackage(_mainEditorState.LoadedPackagePath!, true);
            }
        }
    }

    private void SavePackage(string path, bool autoRun)
    {
        if (_savePackageState.Show)
        {
            return;
        }

        _savePackageState.ShowDialog(path, autoRun);
    }
    
    private void PublishPackage()
    {
        if (_publishPackageState.Show)
        {
            return;
        }

        var path = _publishPackageState.DestinationPath;
        if (string.IsNullOrEmpty(path))
        {
            path = Path.Combine(_mainEditorState.LoadedPackagePath!, "published");
        }

        _publishPackageState.ShowDialog(path, false);
    }

    private void DrawNotLoadedMenu()
    {
        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("Open Package"))
            {
                var path = _loadPackageState.SourcePath;
                if (string.IsNullOrEmpty(path))
                {
                    path = Constants.DefaultParseSourcePath;
                }

                _loadPackageState.ShowDialog(path, false);
            }

            var recentlyOpenedProjects = _editorSettingsState.GetRecentlyOpenedProjects().ToList();
            if (recentlyOpenedProjects.Count > 0)
            {
                if (ImGui.BeginMenu("Open Recent"))
                {
                    foreach (var path in recentlyOpenedProjects)
                    {
                        var shortenedPath = path;
                        if (path.Length > 20)
                        {
                            shortenedPath = path.Substring(path.Length - 20, 20);
                        }

                        if (ImGui.MenuItem($"{shortenedPath}"))
                        {
                            _loadPackageState.ShowDialog(path, true);
                        }
                    }
                    ImGui.EndMenu();
                }
            }
            if (ImGui.MenuItem("Parse Game Install"))
            {
                var now = DateTime.Now;
                var sourcePath = _parsePublishedState.SourcePath;
                if (string.IsNullOrEmpty(sourcePath))
                {
                    sourcePath = Constants.DefaultParseSourcePath;
                }

                var destPath = _parsePublishedState.DestinationPath;
                if (string.IsNullOrEmpty(destPath))
                {
                    var newName = $"package-{now:yyyy-MM-dd_HH-mm-ss}";
                    destPath = Path.Combine(Constants.DefaultParseDestinationPath, newName);
                }

                _parsePublishedState.ShowDialog(sourcePath, destPath);
            }
            ImGui.EndMenu();
        }
    }

    private void DrawHelpMenu()
    {
        if (ImGui.BeginMenu("Help"))
        {
            if (ImGui.MenuItem("String Tags", null, _showStringTagsHelp))
            {
                _showStringTagsHelp = !_showStringTagsHelp;
            }
            ImGui.EndMenu();
        }
    }

    private void DrawStringTagsWindow()
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(ImGui.GetIO().DisplaySize.X - 280, 30), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(260, 0), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("String Tags", ref _showStringTagsHelp))
        {
            ImGui.TextWrapped("Bytes >= 0x80 in strings change the text color. The low nibble (byte & 0x0F) selects the VGA palette index. The high nibble is ignored.");
            ImGui.Separator();
            ImGui.TextWrapped("Named shortcuts:");
            if (ImGui.BeginTable("TagsTable", 2, ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Tag", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Description");
                ImGui.TableHeadersRow();
                var tags = new[]
                {
                    ("[black]", "Color 0 — black"),
                    ("[blue]", "Color 1 — blue"),
                    ("[green]", "Color 2 — green"),
                    ("[cyan]", "Color 3 — cyan"),
                    ("[red]", "Color 4 — red"),
                    ("[magenta]", "Color 5 — magenta"),
                    ("[brown]", "Color 6 — brown"),
                    ("[grey]", "Color 7 — light grey"),
                    ("[dkgrey]", "Color 8 — dark grey"),
                    ("[ltblue]", "Color 9 — light blue"),
                    ("[ltgreen]", "Color 10 — light green"),
                    ("[ltcyan]", "Color 11 — light cyan"),
                    ("[ltred]", "Color 12 — light red"),
                    ("[ltmagenta]", "Color 13 — light magenta*"),
                    ("[yellow]", "Color 14 — yellow*"),
                    ("[white]", "Color 15 — white"),
                    ("[0xNN]", "Any hex byte (low nibble = color)"),
                };
                foreach (var (tag, desc) in tags)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(tag);
                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(desc);
                }
                ImGui.EndTable();
            }
            ImGui.Separator();
            ImGui.TextWrapped("VGA palette (low nibble):");
            if (ImGui.BeginTable("PaletteTable", 4, ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 20);
                ImGui.TableSetupColumn("Color", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 20);
                ImGui.TableSetupColumn("Color");
                ImGui.TableHeadersRow();
                var colors = new[]
                {
                    "Black", "Blue", "Green", "Cyan",
                    "Red", "Magenta", "Brown", "Light grey",
                    "Dark grey", "Light blue", "Light green", "Light cyan",
                    "Light red", "Lt magenta*", "Yellow*", "White",
                };
                for (var c = 0; c < 8; c++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn(); ImGui.Text($"{c}");
                    ImGui.TableNextColumn(); ImGui.Text(colors[c]);
                    ImGui.TableNextColumn(); ImGui.Text($"{c + 8}");
                    ImGui.TableNextColumn(); ImGui.Text(colors[c + 8]);
                }
                ImGui.EndTable();
            }
            ImGui.TextDisabled("* Colors 13/14 are replaced by player/enemy clothing colors in sprites.");
        }
        ImGui.End();
    }

    private void DrawLoadedInfo()
    {
        if (!_mainEditorState.IsPackageLoaded)
        {
            return;
        }

        var packagePath = _mainEditorState.LoadedPackagePath;
        var text = $"Loaded: {packagePath}";
        var oldCursorPos = ImGui.GetCursorPos();
        try
        {
            ImGui.SetCursorPos(ImGui.GetWindowContentRegionMax() - ImGui.CalcTextSize(text));
            if (ImGui.BeginMenu(text, false))
            {
                ImGui.EndMenu();
            }
        }
        finally
        {
            ImGui.SetCursorPos(oldCursorPos);
        }
    }
}