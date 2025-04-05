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
        foreach (var clue in clues)
        {
            if (crimeId != null)
            {
                ImGui.SetNextItemWidth(100.0f);
                var crime = clue.CrimeId ?? -1;
                var origCrime = crime;
                ImGui.InputInt("Crime ID", ref crime);
                if (crime != origCrime)
                {
                    //TODO: change crime ID
                }
            }
            else
            {
                ImGui.SetNextItemWidth(100.0f);
                var crime = 0;
                ImGui.InputInt("Crime ID", ref crime, 1, 1, ImGuiInputTextFlags.ReadOnly);
            }
            
            ImGui.SameLine();
            ImGui.Text("");
            ImGui.SameLine();

            var origId = clue.Id;
            var id = origId;
            ImGui.SetNextItemWidth(100.0f);
            ImGui.InputInt("ID", ref id);
            if (id != origId)
            {
                if (model.Texts.Any(x => x.Value.Id == id && x.Value.CrimeId == crimeId))
                {
                    ImGui.SameLine();
                    ImGui.Text("Key already taken");
                }
                else
                {
                    //TODO: change ID?
                }
            }
            
            ImGui.SameLine();
            ImGui.Text("");
            ImGui.SameLine();

            ImGui.SetNextItemWidth(200.0f);
            var types = Enum.GetValues<ClueType>()
                .Where(x => x != ClueType.Unknown)
                .Select(x => $"{x}")
                .ToArray();
            var typeIndex = types.ToList().FindIndex(x => x == clue.Type.ToString());
            var origTypeIndex = typeIndex;
            ImGui.Combo("Clue Type", ref typeIndex, types, types.Length);
            if (typeIndex != origTypeIndex)
            {
                //TODO: change
            }
            
            ImGui.SameLine();
            ImGui.Text("");
            ImGui.SameLine();

            ImGui.SetNextItemWidth(100.0f);
            var u1 = clue.Unknown1;
            var origU1 = u1;
            ImGui.InputInt("Unknown 1", ref u1);
            if (u1 != origU1)
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
        }
    }
}