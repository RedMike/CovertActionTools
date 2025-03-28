using CovertActionTools.App.ViewModels;
using ImGuiNET;

namespace CovertActionTools.App.Windows;

public class MainMenuWindow : BaseWindow
{
    private readonly MainEditorState _mainEditorState;

    public MainMenuWindow(MainEditorState mainEditorState)
    {
        _mainEditorState = mainEditorState;
    }

    public override void Draw()
    {
        ImGui.BeginMainMenuBar();
        //is a package loaded?
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
                //TODO: show dialog for triggering a decompilation and where to save it
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