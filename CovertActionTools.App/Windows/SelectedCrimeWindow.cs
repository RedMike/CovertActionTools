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
        
        var key = int.Parse(_mainEditorState.SelectedItem.Value.id);
        
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
            else if (model.Crimes.ContainsKey(id))
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
        var windowSize = ImGui.GetContentRegionAvail();

        if (ImGui.CollapsingHeader("Participants"))
        {
            if (ImGui.Button("Add Participant"))
            {
                crime.Participants.Add(new CrimeModel.Participant());
            }

            ImGui.Text("");

            for (var i = 0; i < crime.Participants.Count; i++)
            {
                DrawParticipant(model, crime, i);
                ImGui.Text("");
            }
        }

        if (ImGui.CollapsingHeader("Events"))
        {
            if (ImGui.Button("Add Event"))
            {
                crime.Events.Add(new CrimeModel.Event());
            }

            ImGui.Text("");
            
            for (var i = 0; i < crime.Events.Count; i++)
            {
                DrawEvent(model, crime, i);
                ImGui.Text("");
            }
        }
        
        if (ImGui.CollapsingHeader("Objects"))
        {
            if (crime.Objects.Count < 4)
            {
                if (ImGui.Button("Add Object"))
                {
                    crime.Objects.Add(new CrimeModel.Object());
                }

                ImGui.Text("");
            }
            
            for (var i = 0; i < crime.Objects.Count; i++)
            {
                DrawObject(model, crime, i);
            }
        }
    }

    private void DrawParticipant(PackageModel model, CrimeModel crime, int i)
    {
        var windowSize = ImGui.GetContentRegionAvail();
        
        var participant = crime.Participants[i];
        ImGui.BeginChild($"Participant {i}", new Vector2(windowSize.X, 100.0f), true);

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
        var u3 = $"{participant.Unknown3:X4}";
        var origU3 = u3;
        ImGui.InputText("Unknown 3", ref u3, 4);
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

    private void DrawEvent(PackageModel model, CrimeModel crime, int i)
    {
        var windowSize = ImGui.GetContentRegionAvail();
        
        var ev = crime.Events[i];
        ImGui.BeginChild($"Event {i}", new Vector2(windowSize.X, 180.0f), true);

        ImGui.Text($"Event {i + 1}");
        
        ImGui.SetNextItemWidth(250.0f);
        var participantList = crime.Participants
            .Select((x, idx) => string.IsNullOrEmpty(x.Role) ? $"Participant {idx + 1}" : x.Role)
            .ToArray();
        var participantIndex = ev.SourceParticipantId;
        var origParticipantIndex = participantIndex;
        ImGui.Combo("Participant", ref participantIndex, participantList, participantList.Length);
        if (participantIndex != origParticipantIndex)
        {
            //TODO: change
        }
        
        ImGui.SameLine();
        ImGui.Text("  =>  ");
        ImGui.SameLine();
        
        ImGui.SetNextItemWidth(250.0f);
        var destParticipantList = new[] { "None" }.Concat(participantList).ToArray();
        var destParticipantIndex = (ev.TargetParticipantId ?? -1) + 1;
        var origDestParticipantIndex = destParticipantIndex;
        ImGui.Combo("Target", ref destParticipantIndex, destParticipantList, destParticipantList.Length);
        if (destParticipantIndex != origDestParticipantIndex)
        {
            //TODO: change
        }
        
        ImGui.SetNextItemWidth(200.0f);
        var eventTypes = Enum.GetValues<CrimeModel.EventType>().Select(x => x.ToString()).ToArray();
        var eventType = eventTypes.ToList().FindIndex(x => x == ev.EventType.ToString());
        var origEventType = eventType;
        ImGui.Combo("Type", ref eventType, eventTypes, eventTypes.Length);
        if (eventType != origEventType)
        {
            //TODO: change
        }
        
        ImGui.SameLine();
        ImGui.Text(" ");
        ImGui.SameLine();
        
        ImGui.SetNextItemWidth(200.0f);
        var description = ev.Description;
        var origDescription = description;
        ImGui.InputText("Description", ref description, 32);
        if (description != origDescription)
        {
            ev.Description = description;
        }
        
        ImGui.SameLine();
        ImGui.Text(" ");
        ImGui.SameLine();
        
        ImGui.SetNextItemWidth(100.0f);
        var score = ev.Score;
        var origScore = score;
        ImGui.InputInt("Score", ref score);
        if (score != origScore)
        {
            //TODO: change
        }
        
        ImGui.SetNextItemWidth(100.0f);
        var messageId = ev.MessageId;
        var origMessageId = messageId;
        ImGui.InputInt("Message ID", ref messageId);
        if (messageId != origMessageId)
        {
            ev.MessageId = messageId;
        }

        ImGui.BeginChild($"Message text {messageId}", new Vector2(ImGui.GetContentRegionAvail().X, 50.0f), true);
        var text = model.Texts.Values.FirstOrDefault(x => x.CrimeId == crime.Id && x.Id == messageId);
        if (text != null)
        {
            ImGui.Text(text.Message);
        }
        else
        {
            ImGui.Text($"ERROR: Failed to find MSG{crime.Id:D2}{messageId:D2}");
        }

        ImGui.EndChild();
        
        //TODO: received/destroyed items
        
        ImGui.EndChild();
    }

    private void DrawObject(PackageModel model, CrimeModel crime, int i)
    {
        var windowSize = ImGui.GetContentRegionAvail();
        var obj = crime.Objects[i];
        ImGui.BeginChild($"Object {i}", new Vector2(windowSize.X, 35.0f), true);
        ImGui.SetNextItemWidth(150.0f);
        var name = obj.Name;
        var origName = name;
        ImGui.InputText("Name", ref name, 16);
        if (name != origName)
        {
            obj.Name = name;
        }
        
        ImGui.SameLine();
        ImGui.Text("  ");
        ImGui.SameLine();
        
        
        ImGui.SetNextItemWidth(150.0f);
        var pictureId = obj.PictureId;
        var origPictureId = pictureId;
        ImGui.InputInt("Icon", ref pictureId);
        if (pictureId != origPictureId && pictureId >= 0 && pictureId < 16)
        {
            obj.PictureId = pictureId;
        }
        
        if (model.SimpleImages.TryGetValue("ICONS", out var iconImage))
        {
            ImGui.SameLine();
            ImGui.Text("  ");
            ImGui.SameLine();
            
            var iconImageId = $"icon_{pictureId}";
            var iconBytes = new List<byte>();
            var ox = 16 * pictureId;
            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    iconBytes.Add(iconImage.VgaImageData[(y * iconImage.Width + ox + x) * 4 + 0]);
                    iconBytes.Add(iconImage.VgaImageData[(y * iconImage.Width + ox + x) * 4 + 1]);
                    iconBytes.Add(iconImage.VgaImageData[(y * iconImage.Width + ox + x) * 4 + 2]);
                    iconBytes.Add(iconImage.VgaImageData[(y * iconImage.Width + ox + x) * 4 + 3]);
                }
            }

            var texture = _renderWindow.RenderImage(RenderWindow.RenderType.Icon, iconImageId, 16, 16, iconBytes.ToArray());

            ImGui.Image(texture, new Vector2(16, 16));
        }
        ImGui.EndChild();
    }
}