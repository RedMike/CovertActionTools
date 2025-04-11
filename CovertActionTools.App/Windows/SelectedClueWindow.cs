using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedClueWindow : BaseWindow
{
    private readonly ILogger<SelectedClueWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly RenderWindow _renderWindow;

    public SelectedClueWindow(ILogger<SelectedClueWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow)
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
            _mainEditorState.SelectedItem.Value.type != MainEditorState.ItemType.Clue)
        {
            return;
        }

        int? crimeId = null;
        if (_mainEditorState.SelectedItem.Value.id != "any")
        {
            crimeId = int.Parse(_mainEditorState.SelectedItem.Value.id);
        }
        
        var screenSize = ImGui.GetMainViewport().Size;
        var initialPos = new Vector2(300.0f, 20.0f);
        var initialSize = new Vector2(screenSize.X - 300.0f, screenSize.Y - 200.0f);
        ImGui.SetNextWindowSize(initialSize);
        ImGui.SetNextWindowPos(initialPos);
        ImGui.Begin($"Clues {crimeId}", //TODO: change label but not ID to prevent unfocusing
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);
        
        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            
            DrawClueWindow(model, crimeId);
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawClueWindow(PackageModel model, int? crimeId)
    {
        //TODO: keep a pending model and have a save button?

        var clues = model.Clues.Values
            .Where(x => x.CrimeId == crimeId)
            .OrderBy(x => x.CrimeId != null ? x.Id : (int)x.Type)
            .ThenBy(x => x.Id)
            .ToList();
        var i = 0;
        foreach (var clue in clues)
        {
            ImGui.PushID($"Clue{i++}");
            
            //non-crime clues aren't tied to any crime
            var newCrimeId = ImGuiExtensions.Input("Crime ID", clue.CrimeId ?? -1, width: 100);
            if (newCrimeId != null)
            {
                //TODO: change crime ID
            }
            ImGuiExtensions.SameLineSpace();

            var newId = ImGuiExtensions.Input("ID", clue.Id, width: 100);
            if (newId != null)
            {
                if (model.Texts.Any(x => x.Value.Id == newId && x.Value.CrimeId == crimeId))
                {
                    ImGui.SameLine();
                    ImGui.Text("Key already taken");
                }
                else
                {
                    //TODO: change ID?
                }
            }
            
            ImGuiExtensions.SameLineSpace();

            var newClueType = ImGuiExtensions.InputEnum("Clue Type", clue.Type, false, ClueType.Unknown, width: 150);
            if (newClueType != null)
            {
                //TODO: change
            }
            
            ImGuiExtensions.SameLineSpace();

            var newClueSource = ImGuiExtensions.InputEnum("Source", clue.Source, false, ClueModel.ClueSource.Unknown, width: 150);
            if (newClueSource != null)
            {
                //TODO: change
            }
            
            var windowSize = ImGui.GetContentRegionAvail();
            var message = clue.Message.Replace("\r", ""); //strip out \r and re-add after, for consistency across OS
            var origMessage = message;
            ImGui.InputTextMultiline($"Message {clue.GetMessagePrefix()}", ref message, 1024, new Vector2(windowSize.X, 30.0f),
                ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.CtrlEnterForNewLine);
            if (message != origMessage)
            {
                var fixedMessage = message.Replace("\n", "\r\n"); //re-add \r, for consistency across OS
                //TODO: change message
            }
            
            ImGui.Separator();
            ImGui.PopID();
        }
    }
}