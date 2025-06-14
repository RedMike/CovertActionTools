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
    private readonly PendingEditorClueState _pendingState;

    public SelectedClueWindow(ILogger<SelectedClueWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow, PendingEditorClueState pendingState)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _renderWindow = renderWindow;
        _pendingState = pendingState;
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

        int? id = null;
        if (_mainEditorState.SelectedItem.Value.id != "any")
        {
            id = int.Parse(_mainEditorState.SelectedItem.Value.id);
        }
        
        var screenSize = ImGui.GetMainViewport().Size;
        var initialPos = new Vector2(300.0f, 20.0f);
        var initialSize = new Vector2(screenSize.X - 300.0f, screenSize.Y - 200.0f);
        ImGui.SetNextWindowSize(initialSize);
        ImGui.SetNextWindowPos(initialPos);
        ImGui.Begin("Clue",
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);
        
        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            
            DrawClueWindow(model, id);
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawClueWindow(PackageModel model, int? crimeId)
    {
        var allClues = ImGuiExtensions.PendingSaveChanges(_pendingState, "id",
            () => model.Clues.ToDictionary(x => x.Key, x => x.Value.Clone()),
            (data) =>
            {
                model.Clues = data;
                _mainEditorState.RecordChange();
                if (!model.Index.ClueChanges)
                {
                    model.Index.ClueChanges = true;
                    model.Index.ClueIncluded = true;
                }
            });
        if (ImGui.Button("Add Clue"))
        {
            _pendingState.RecordChange();
            var clue = new ClueModel();
            if (crimeId == null)
            {
                clue.Type = ClueType.Weapon;
                clue.Id = allClues.Where(x =>
                        x.Value.CrimeId == null &&
                        x.Value.Type == ClueType.Weapon)
                    .Max(x => x.Value.Id) + 1;
            }
            else
            {
                clue.CrimeId = crimeId;
                clue.Id = allClues.Where(x => x.Value.CrimeId == crimeId)
                    .Max(x => x.Value.Id) + 1;
            }
            allClues.Add(clue.GetMessagePrefix(), clue);
        }
        var clues = allClues.Values
            .Where(x => x.CrimeId == crimeId)
            .OrderBy(x => x.CrimeId != null ? x.Id : (int)x.Type)
            .ThenBy(x => x.Id)
            .ToList();
        for (var i = 0; i < clues.Count; i++)
        {
            ImGui.PushID($"Clue_{i}");
            DrawClue(allClues, clues[i], i);
            ImGui.Text("");
            ImGui.PopID();
        }
    }

    private void DrawClue(Dictionary<string, ClueModel> clues, ClueModel clue, int i)
    {
        var windowSize = ImGui.GetContentRegionAvail();
        
        ImGui.BeginChild($"Clue {clue.GetMessagePrefix()}", new Vector2(windowSize.X, 130.0f), true);
        
        var cursorPos = ImGui.GetCursorPos();
        ImGui.Text($"Clue {clue.GetMessagePrefix()}");
        var nextCursorPos = ImGui.GetCursorPos();
        
        //now move to the right to make the delete button
        var o = ImGui.CalcTextSize(" Remove ");
        ImGui.SetCursorPos(new Vector2(windowSize.X - o.X, cursorPos.Y));
        if (ImGui.Button("Remove"))
        {
            clues.Remove(clue.GetMessagePrefix());
            _pendingState.RecordChange();
            return;
        }
        
        ImGui.SetCursorPos(nextCursorPos);
        ImGui.Text("");

        if (ImGui.BeginTable("c1", 4))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            //non-crime clues aren't tied to any crime
            var newCrimeId = ImGuiExtensions.Input("Crime ID", clue.CrimeId ?? -1, width: 100);
            if (newCrimeId != null)
            {
                var oldPrefix = clue.GetMessagePrefix();
                var oldCrimeId = clue.CrimeId;
                clue.CrimeId = newCrimeId;
                var newPrefix = clue.GetMessagePrefix();
                if (clues.ContainsKey(newPrefix))
                {
                    //TODO: error
                    clue.CrimeId = oldCrimeId;
                }
                else
                {
                    clues.Remove(oldPrefix);
                    clues.Add(newPrefix, clue);
                }
                _pendingState.RecordChange();
            }

            ImGui.TableNextColumn();
            var newId = ImGuiExtensions.Input("ID", clue.Id, width: 100);
            if (newId != null)
            {
                var oldPrefix = clue.GetMessagePrefix();
                var oldId = clue.Id;
                clue.Id = newId.Value;
                var newPrefix = clue.GetMessagePrefix();
                if (clues.ContainsKey(newPrefix))
                {
                    //TODO: error
                    clue.Id = oldId;
                }
                else
                {
                    clues.Remove(oldPrefix);
                    clues.Add(newPrefix, clue);
                }
                _pendingState.RecordChange();
            }

            ImGui.TableNextColumn();
            var newClueType = ImGuiExtensions.InputEnum("Clue Type", clue.Type, false, ClueType.Unknown, width: 150);
            if (newClueType != null)
            {
                var oldPrefix = clue.GetMessagePrefix();
                var oldType = clue.Type;
                clue.Type = newClueType.Value;
                var newPrefix = clue.GetMessagePrefix();
                if (clues.ContainsKey(newPrefix))
                {
                    //TODO: error
                    clue.Type = oldType;
                }
                else
                {
                    clues.Remove(oldPrefix);
                    clues.Add(newPrefix, clue);
                }
                _pendingState.RecordChange();
            }


            ImGui.TableNextColumn();
            var newClueSource =
                ImGuiExtensions.InputEnum("Source", clue.Source, false, ClueModel.ClueSource.Unknown, width: 150);
            if (newClueSource != null)
            {
                clue.Source = newClueSource.Value;
                _pendingState.RecordChange();
            }
            ImGui.EndTable();
        }
        
        var message = clue.Message.Replace("\r", ""); //strip out \r and re-add after, for consistency across OS
        var origMessage = message;
        ImGui.InputTextMultiline($"Message {clue.GetMessagePrefix()}", ref message, 1024,
            new Vector2(windowSize.X, 50.0f),
            ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.CtrlEnterForNewLine);
        if (message != origMessage)
        {
            var fixedMessage = message.Replace("\n", "\r\n"); //re-add \r, for consistency across OS
            clue.Message = fixedMessage;
            _pendingState.RecordChange();
        }

        ImGui.EndChild();
    }
}