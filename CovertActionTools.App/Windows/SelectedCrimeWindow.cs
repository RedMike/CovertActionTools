using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedCrimeWindow : BaseWindow
{
    private readonly ILogger<SelectedCrimeWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly RenderWindow _renderWindow;

    public SelectedCrimeWindow(ILogger<SelectedCrimeWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow)
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
            _mainEditorState.SelectedItem.Value.type != MainEditorState.ItemType.Crime)
        {
            return;
        }
        
        var key = _mainEditorState.SelectedItem.Value.id;
        
        var screenSize = ImGui.GetMainViewport().Size;
        var initialPos = new Vector2(300.0f, 20.0f);
        var initialSize = new Vector2(screenSize.X - 300.0f, screenSize.Y - 200.0f);
        ImGui.SetNextWindowSize(initialSize);
        ImGui.SetNextWindowPos(initialPos);
        ImGui.Begin($"Crime", //TODO: change label but not ID to prevent unfocusing
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);
        
        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            if (model.Crimes.TryGetValue(key, out var crime))
            {
                DrawCrimeWindow(model, crime);
            }
            else
            {
                ImGui.Text("Something went wrong, crime is missing..");
            }
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawCrimeWindow(PackageModel model, CrimeModel crime)
    {
        //TODO: keep a pending model and have a save button?
        
        var origId = crime.Id;
        var id = origId;
        ImGui.SetNextItemWidth(100.0f);
        ImGui.InputInt("ID", ref id);
        if (id != origId)
        {
            if (id < 0 || id > 12)
            {
                ImGui.SameLine();
                ImGui.Text("Only crimes 0-12 are supported");
            } 
            else if (model.Crimes.ContainsKey($"CRIME{id}"))
            {
                ImGui.SameLine();
                ImGui.Text("Key already taken");
            }
            else
            {
                //TODO: change ID?    
            }
        }

        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        if (ImGui.Button("Add Participant"))
        {
            crime.Participants.Add(new CrimeModel.Participant());
        }
        
        ImGui.Text("");

        var windowSize = ImGui.GetContentRegionMax();
        for (var i = 0; i < crime.Participants.Count; i++)
        {
            var participant = crime.Participants[i];
            ImGui.BeginChild($"Participant {i}", new Vector2(windowSize.X, 200.0f), true);

            var role = participant.Role;
            var origRole = role;
            ImGui.InputText("Role", ref role, 32);
            if (role != origRole)
            {
                participant.Role = role;
            }
            
            ImGui.EndChild();
            ImGui.Text("");
        }
    }
}