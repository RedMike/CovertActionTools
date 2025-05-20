using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedProseWindow : BaseWindow
{
    private readonly ILogger<SelectedTextWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly RenderWindow _renderWindow;

    public SelectedProseWindow(ILogger<SelectedTextWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _renderWindow = renderWindow;
    }

    public override void Draw()
    {
        if (!_mainEditorState.IsPackageLoaded)
        {
            return;
        }

        if (_mainEditorState.SelectedItem == null ||
            _mainEditorState.SelectedItem.Value.type != MainEditorState.ItemType.Prose)
        {
            return;
        }

        var proseKey = _mainEditorState.SelectedItem.Value.id;
        
        var screenSize = ImGui.GetMainViewport().Size;
        var initialPos = new Vector2(300.0f, 20.0f);
        var initialSize = new Vector2(screenSize.X - 300.0f, screenSize.Y - 200.0f);
        ImGui.SetNextWindowSize(initialSize);
        ImGui.SetNextWindowPos(initialPos);
        ImGui.Begin($"Prose", //TODO: change label but not ID to prevent unfocusing
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);
        
        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            
            DrawProseWindow(model, model.Prose[proseKey]);
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawProseWindow(PackageModel model, ProseModel prose)
    {
        //TODO: keep a pending model and have a save button?
        var newPrefix = ImGuiExtensions.Input("Prefix", prose.GetMessagePrefix(), 64);
        if (newPrefix != null)
        {
            //TODO: change ID
        }
        
        var windowSize = ImGui.GetContentRegionAvail();
        var message = prose.Message.Replace("\r", ""); //strip out \r and re-add after, for consistency across OS
        var origMessage = message;
        ImGui.InputTextMultiline($"Message {prose.GetMessagePrefix()}", ref message, 1024, new Vector2(windowSize.X, 50.0f),
            ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.CtrlEnterForNewLine);
        if (message != origMessage)
        {
            var fixedMessage = message.Replace("\n", "\r\n"); //re-add \r, for consistency across OS
            prose.Message = fixedMessage;
        }
    }
}