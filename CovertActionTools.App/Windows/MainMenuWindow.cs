using System.Reflection;
using CovertActionTools.App.ViewModels;
using ImGuiNET;

namespace CovertActionTools.App.Windows;

public class MainMenuWindow : BaseWindow
{
    #if DEBUG
    //when running locally, just default to the known path with the original
    private static readonly string DefaultSourcePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./", "../../../../../../Original/MPS/COVERT"));
    #else
    private static readonly string DefaultSourcePath = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./");
    #endif
    private static readonly string DefaultDestinationPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "./", "published"));
    
    private readonly MainEditorState _mainEditorState;
    private readonly ParsePublishedState _parsePublishedState;

    public MainMenuWindow(MainEditorState mainEditorState, ParsePublishedState parsePublishedState)
    {
        _mainEditorState = mainEditorState;
        _parsePublishedState = parsePublishedState;
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

            if (ImGui.MenuItem("Save Package"))
            {
                //TODO: save
            }

            if (ImGui.MenuItem("Publish to Folder"))
            {
                //TODO: dialog
            }
            
            ImGui.EndMenu();
        }
    }

    private void DrawNotLoadedMenu()
    {
        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("Open Package"))
            {
                //TODO: show dialog for selecting location
            }
            if (ImGui.MenuItem("Parse Published Folder"))
            {
                if (string.IsNullOrEmpty(_parsePublishedState.SourcePath))
                {
                    _parsePublishedState.SourcePath = DefaultSourcePath;
                }

                if (string.IsNullOrEmpty(_parsePublishedState.DestinationPath))
                {
                    var newName = $"package-{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}";
                    _parsePublishedState.DestinationPath = Path.Combine(DefaultDestinationPath, newName);
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