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
        ImGui.EndMainMenuBar();
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