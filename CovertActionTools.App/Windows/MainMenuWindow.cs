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
    private readonly IPackageExporter _exporter;
    private readonly EditorSettingsState _editorSettingsState;
    
    public MainMenuWindow(ILogger<MainMenuWindow> logger, MainEditorState mainEditorState, ParsePublishedState parsePublishedState, LoadPackageState loadPackageState, SavePackageState savePackageState, IPackageExporter<IExporter> exporter, EditorSettingsState editorSettingsState)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _parsePublishedState = parsePublishedState;
        _loadPackageState = loadPackageState;
        _savePackageState = savePackageState;
        _exporter = exporter;
        _editorSettingsState = editorSettingsState;
    }

    public override void Draw()
    {
        if (_parsePublishedState.Show)
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
                //TODO: check if need to save
                //TODO: close
            }
            
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Save Package"))
        {
            SavePackage();
            
            ImGui.EndMenu();
        }
    }

    private void SavePackage()
    {
        if (_savePackageState.Show)
        {
            return;
        }
        _savePackageState.Show = true;
        _savePackageState.Run = true;
        _savePackageState.Exporter = _exporter;
        _savePackageState.Exporter.StartExport(_mainEditorState.LoadedPackage!, _mainEditorState.LoadedPackagePath!);
    }

    private void DrawNotLoadedMenu()
    {
        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("Open Package"))
            {
                if (string.IsNullOrEmpty(_loadPackageState.SourcePath))
                {
                    _loadPackageState.SourcePath = Constants.DefaultParseSourcePath;
                }

                _logger.LogInformation($"Showing Load Package dialog");
                _loadPackageState.Show = true;
                _loadPackageState.Run = false;
                _loadPackageState.AutoRun = false;
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
                            _loadPackageState.SourcePath = path;
                            _loadPackageState.Show = true;
                            _loadPackageState.AutoRun = true;
                        }
                    }
                    ImGui.EndMenu();
                }
            }
            if (ImGui.MenuItem("Parse Published Folder"))
            {
                var now = DateTime.Now;
                if (string.IsNullOrEmpty(_parsePublishedState.SourcePath))
                {
                    _parsePublishedState.SourcePath = Constants.DefaultParseSourcePath;
                }

                if (string.IsNullOrEmpty(_parsePublishedState.DestinationPath))
                {
                    var newName = $"package-{now:yyyy-MM-dd_HH-mm-ss}";
                    _parsePublishedState.DestinationPath = Path.Combine(Constants.DefaultParseDestinationPath, newName);
                }

                _parsePublishedState.Show = true;
                _parsePublishedState.Run = false;
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
                //TODO: extra info
                ImGui.EndMenu();
            }
        }
        finally
        {
            ImGui.SetCursorPos(oldCursorPos);
        }
    }
}