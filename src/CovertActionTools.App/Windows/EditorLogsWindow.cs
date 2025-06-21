using System.Numerics;
using CovertActionTools.App.ViewModels;
using ImGuiNET;

namespace CovertActionTools.App.Windows;

public class EditorLogsWindow : BaseWindow
{
    private readonly AppLoggingState _appLoggingState;
    private readonly MainEditorState _mainEditorState;

    public EditorLogsWindow(AppLoggingState appLoggingState, MainEditorState mainEditorState)
    {
        _appLoggingState = appLoggingState;
        _mainEditorState = mainEditorState;
    }

    public override void Draw()
    {
        if (!_mainEditorState.IsPackageLoaded)
        {
            return;
        }
        
        var screenSize = ImGui.GetMainViewport().Size;
        var initialPos = new Vector2(0.0f, screenSize.Y - 180.0f);
        var initialSize = new Vector2(screenSize.X, 180.0f);
        ImGui.SetNextWindowSize(initialSize);
        ImGui.SetNextWindowPos(initialPos);
        ImGui.Begin("Logs", 
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse | 
            ImGuiWindowFlags.NoTitleBar);
        
        var logs = _appLoggingState.Logs.ToList();
        foreach (var log in logs)
        {
            ImGui.TextUnformatted(log);
        }
        
        ImGui.End();
    }
}