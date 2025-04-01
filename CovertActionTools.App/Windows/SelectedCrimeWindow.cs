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

        var windowSize = ImGui.GetContentRegionAvail();
        for (var i = 0; i < crime.Participants.Count; i++)
        {
            DrawParticipant(model, crime, i, windowSize.X);
            ImGui.Text("");
        }
    }

    private void DrawParticipant(PackageModel model, CrimeModel crime, int i, float w)
    {
        var participant = crime.Participants[i];
        ImGui.BeginChild($"Participant {i}", new Vector2(w, 100.0f), true);

        ImGui.Text($"Participant {i + 1}");

        ImGui.SetNextItemWidth(150.0f);
        var role = participant.Role;
        var origRole = role;
        ImGui.InputText("Role", ref role, 32);
        if (role != origRole)
        {
            participant.Role = role;
        }
        
        ImGui.SameLine();
        ImGui.Text("  ");
        ImGui.SameLine();
        
        ImGui.SetNextItemWidth(150.0f);
        var exposure = participant.Exposure;
        var origExposure = exposure;
        ImGui.InputInt("Exposure", ref exposure);
        if (exposure != origExposure)
        {
            //TODO: change
        }
        
        ImGui.SetNextItemWidth(150.0f);
        var participantTypes = Enum.GetValues<CrimeModel.ParticipantType>().Select(x => x.ToString()).ToArray();
        var participantTypeIndex = participantTypes.ToList().FindIndex(x => x == participant.ParticipantType.ToString());
        var origParticipantTypeIndex = participantTypeIndex;
        ImGui.Combo("Type", ref participantTypeIndex, participantTypes, participantTypes.Length);
        if (participantTypeIndex != origParticipantTypeIndex)
        {
            //TODO: change
        }
        
        ImGui.SameLine();
        ImGui.Text("  ");
        ImGui.SameLine();
        
        ImGui.SetNextItemWidth(150.0f);
        var u1 = $"{participant.Unknown1:X4}";
        var origU1 = u1;
        ImGui.InputText("Unknown 1", ref u1, 4);
        if (u1 != origU1)
        {
            //TODO: change
        }
        
        ImGui.SameLine();
        ImGui.Text("  ");
        ImGui.SameLine();
        
        ImGui.SetNextItemWidth(150.0f);
        var u2 = $"{participant.Unknown2:B8}";
        var origU2 = u2;
        ImGui.InputText("Unknown 2", ref u2, 8);
        if (u2 != origU2)
        {
            //TODO: change
        }
        
        ImGui.SetNextItemWidth(150.0f);
        var u3 = $"{participant.Unknown3:X2}";
        var origU3 = u3;
        ImGui.InputText("Unknown 3", ref u3, 2);
        if (u3 != origU3)
        {
            //TODO: change
        }
        
        ImGui.SameLine();
        ImGui.Text("  ");
        ImGui.SameLine();
        
        ImGui.SetNextItemWidth(150.0f);
        var u4 = $"{participant.Unknown4:X4}";
        var origU4 = u4;
        ImGui.InputText("Unknown 4", ref u4, 4);
        if (u4 != origU4)
        {
            //TODO: change
        }
        
        ImGui.SameLine();
        ImGui.Text("  ");
        ImGui.SameLine();
        
        ImGui.SetNextItemWidth(150.0f);
        var u5 = $"{participant.Unknown5:X2}";
        var origU5 = u5;
        ImGui.InputText("Unknown 5", ref u5, 2);
        if (u5 != origU5)
        {
            //TODO: change
        }
        
        ImGui.EndChild();
    }
}