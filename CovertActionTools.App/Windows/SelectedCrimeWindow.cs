using System.Globalization;
using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using CovertActionTools.Core.Processors;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedCrimeWindow : BaseWindow
{
    private readonly ILogger<SelectedCrimeWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly RenderWindow _renderWindow;
    private readonly ICrimeTimelineProcessor _crimeTimelineProcessor;

    private string _idError = "";

    public SelectedCrimeWindow(ILogger<SelectedCrimeWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow, ICrimeTimelineProcessor crimeTimelineProcessor)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _renderWindow = renderWindow;
        _crimeTimelineProcessor = crimeTimelineProcessor;
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

        var newId = ImGuiExtensions.Input("ID", crime.Id, width: 100);
        if (newId != null)
        {
            if (newId < 0 || newId > 12)
            {
                _idError = "Only crimes 0-12 are supported";
            }
            else if (model.Crimes.ContainsKey(newId.Value))
            {
                _idError = "Key already taken";
            }
            else
            {
                _idError = "";
                //TODO: change ID
            }
        }

        if (!string.IsNullOrEmpty(_idError))
        {
            ImGuiExtensions.SameLineSpace();
            ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), _idError);
        }
        
        if (ImGui.CollapsingHeader("Participants"))
        {
            if (ImGui.Button("Add Participant"))
            {
                crime.Participants.Add(new CrimeModel.Participant());
            }

            for (var i = 0; i < crime.Participants.Count; i++)
            {
                ImGui.PushID($"Participant_{i}");
                DrawParticipant(model, crime, i);
                ImGui.Text("");
                ImGui.PopID();
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

        if (ImGui.CollapsingHeader("Timeline"))
        {
            try
            {   
                var timeline = _crimeTimelineProcessor.ProcessCrimeIntoTimeline(model, crime);
                var i = 0;
                foreach (var ev in timeline)
                {
                    if (ev.Type == CrimeTimelineEvent.CrimeTimelineEventType.Error)
                    {
                        ImGui.Text($"Error: {ev.ErrorMessage}");
                        
                        ImGui.Text("");
                        ImGui.Separator();
                        ImGui.Text("");
                        continue;
                    }

                    if (ev.Type == CrimeTimelineEvent.CrimeTimelineEventType.ItemUpdate)
                    {
                        
                        ImGui.Text($"Item update: {ev.ErrorMessage}");
                        
                        ImGui.Text("");
                        ImGui.Separator();
                        ImGui.Text("");
                        continue;
                    }
                    
                    i++;
                    ImGui.SetNextItemWidth(100.0f);
                    ImGui.Text($"Event {i} Day {ev.Iteration} {ev.Type}");
                    ImGui.SameLine();
                    var source = crime.Participants[ev.SourceParticipantId].Role.Trim() + $" ({ev.SourceParticipantId})";
                    var target = "";
                    if (ev.TargetParticipantId != null)
                    {
                        target = crime.Participants[ev.TargetParticipantId.Value].Role.Trim() + $" ({ev.TargetParticipantId})";
                    }
                    var message = model.Texts.Values.FirstOrDefault(x =>
                        x.Type == TextModel.StringType.CrimeMessage && 
                        x.CrimeId == crime.Id && 
                        x.Id == ev.MessageId)?.Message ?? $"Missing message ID {ev.MessageId}";
                    
                    ImGui.SetNextItemWidth(100.0f);
                    ImGui.Text($"({string.Join(", ", ev.EventIds)})");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100.0f);
                    ImGui.Text($"from {source}");
                    ImGui.SameLine();
                    if (ev.TargetParticipantId != null)
                    {
                        ImGui.SetNextItemWidth(100.0f);
                        ImGui.Text($"to {target}");
                        ImGui.SameLine();
                    }

                    if (ev.ItemsCreated.Count > 0)
                    {
                        ImGui.SetNextItemWidth(100.0f);
                        ImGui.Text($"Created: [{string.Join(", ", ev.ItemsCreated.Select(x => crime.Objects[x].Name.Trim()))}]");
                        ImGui.SameLine();
                    }
                    if (ev.ItemsTransferred.Count > 0)
                    {
                        ImGui.SetNextItemWidth(100.0f);
                        ImGui.Text($"Transferred: [{string.Join(", ", ev.ItemsTransferred.Select(x => crime.Objects[x].Name.Trim()))}]");
                        ImGui.SameLine();
                    }
                    if (ev.ItemsDestroyed.Count > 0)
                    {
                        ImGui.SetNextItemWidth(100.0f);
                        ImGui.Text($"Destroyed: [{string.Join(", ", ev.ItemsDestroyed.Select(x => crime.Objects[x].Name.Trim()))}]");
                        ImGui.SameLine();
                    }

                    ImGui.Text("");

                    var messageSize = ImGui.GetContentRegionAvail();
                    ImGui.BeginChild($"Timeline message {i}", new Vector2(messageSize.X, 50.0f), true);
                    ImGui.Text(message);
                    ImGui.EndChild();

                    ImGui.Text("");
                    ImGui.Separator();
                    ImGui.Text("");
                }
            }
            catch (Exception e)
            {
                ImGui.Text($"Exception while processing: {e}");
            }
        }
    }

    private void DrawParticipant(PackageModel model, CrimeModel crime, int i)
    {
        var windowSize = ImGui.GetContentRegionAvail();
        
        var participant = crime.Participants[i];
        ImGui.BeginChild($"Participant {i}", new Vector2(windowSize.X, 130.0f), true);

        var cursorPos = ImGui.GetCursorPos();
        ImGui.Text($"Participant {i + 1}");
        var nextCursorPos = ImGui.GetCursorPos();
        
        //now move to the right to make the delete button
        var o = ImGui.CalcTextSize(" Remove ");
        ImGui.SetCursorPos(new Vector2(windowSize.X - o.X, cursorPos.Y));
        if (ImGui.Button("Remove"))
        {
            crime.Participants.RemoveAt(i);
            return;
        }
        
        ImGui.SetCursorPos(nextCursorPos);
        ImGui.Text("");

        if (ImGui.BeginTable("p1", 3))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var newRole = ImGuiExtensions.Input("Role", participant.Role, 32, width: 150);
            if (newRole != null)
            {
                participant.Role = newRole;
            }
        
            ImGui.TableNextColumn();
            var newExposure = ImGuiExtensions.Input("Exposure", participant.Exposure, width: 150);
            if (newExposure != null)
            {
                participant.Exposure = newExposure.Value;
            }
        
            ImGui.TableNextColumn();
            var newClueType = ImGuiExtensions.InputEnum("Clue Type", participant.ClueType, false, ClueType.Unknown, width: 150);
            if (newClueType != null)
            {
                participant.ClueType = newClueType.Value;
            }
            
            ImGui.EndTable();
        }
        
        if (ImGui.BeginTable("p2", 4))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var newMastermind = ImGuiExtensions.Input("Mastermind?", participant.IsMastermind);
            if (newMastermind != null)
            {
                participant.IsMastermind = newMastermind.Value;
            }
        
            
            ImGui.TableNextColumn();
            var newFemale = ImGuiExtensions.Input("Force Female?", participant.ForceFemale);
            if (newFemale != null)
            {
                participant.ForceFemale = newFemale.Value;
            }
            
            ImGui.TableNextColumn();
            var canComeOut = ImGuiExtensions.Input("Back from Hiding?", participant.CanComeOutOfHiding);
            if (canComeOut != null)
            {
                participant.CanComeOutOfHiding = canComeOut.Value;
            }
            
            ImGui.TableNextColumn();
            var insideContact = ImGuiExtensions.Input("Inside Contact?", participant.IsInsideContact);
            if (insideContact != null)
            {
                participant.IsInsideContact = insideContact.Value;
                participant.Unknown2 = (participant.Unknown2 & 0xFE) | (insideContact.Value ? 0x01 : 0x00);
            }
            
            ImGui.EndTable();
        }


        if (ImGui.BeginTable("p3", 2))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var newRank = ImGuiExtensions.Input("Rank", participant.Rank);
            if (newRank != null)
            {
                participant.Rank = newRank.Value;
            }

            ImGui.TableNextColumn();
            var newU2 = ImGuiExtensions.Input("Unknown 2", $"{participant.Unknown2:B8}", 10);
            if (newU2 != null && int.TryParse(newU2, NumberStyles.BinaryNumber, null, out var u2Parsed))
            {
                participant.Unknown2 = u2Parsed;
                participant.IsInsideContact = (u2Parsed & 0x01) == 0x01;
            }
            
            ImGui.EndTable();
        }
        
        ImGui.EndChild();
    }

    private void DrawEvent(PackageModel model, CrimeModel intermediateCrime, int i)
    {
        var windowSize = ImGui.GetContentRegionAvail();
        
        var ev = intermediateCrime.Events[i];
        ImGui.BeginChild($"Event {i}", new Vector2(windowSize.X, 250.0f), true);

        ImGui.Text($"Event {i + 1}");
        
        var participantList = intermediateCrime.Participants
            .Select((x, idx) => string.IsNullOrEmpty(x.Role) ? $"Participant {idx + 1}" : x.Role)
            .ToArray();
        
        ImGui.SetNextItemWidth(200.0f);
        var participantIndex = ev.MainParticipantId;
        var origParticipantIndex = participantIndex;
        ImGui.Combo("Main", ref participantIndex, participantList, participantList.Length);
        if (participantIndex != origParticipantIndex)
        {
            //TODO: change
        }
        
        if (ev.SecondaryParticipantId != null)
        {
            ImGui.SameLine();
            ImGui.Text("  from  ");
            ImGui.SameLine();

            ImGui.SetNextItemWidth(200.0f);
            var secondaryParticipantIndex = ev.SecondaryParticipantId.Value;
            var origSecondaryParticipantIndex = secondaryParticipantIndex;
            ImGui.Combo("Secondary", ref secondaryParticipantIndex, participantList, participantList.Length);
            if (secondaryParticipantIndex != origSecondaryParticipantIndex)
            {
                //TODO: change
            }
            
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100.0f);
            var itemsToSecondary = ev.ItemsToSecondary;
            var origItemsToSecondary = itemsToSecondary;
            ImGui.Checkbox("Items to Secondary", ref itemsToSecondary);
            if (itemsToSecondary != origItemsToSecondary)
            {
                //TODO: change
            }
        }

        ImGui.SetNextItemWidth(200.0f);
        var receiveDescription = ev.ReceiveDescription;
        var origReceiveDescription = receiveDescription;
        ImGui.InputText(ev.IsPairedEvent ? "Receive Description" : "Description", ref receiveDescription, 32);
        if (receiveDescription != origReceiveDescription)
        {
            //TODO: change
        }

        if (ev.IsPairedEvent)
        {
            ImGui.SameLine();
            
            ImGui.SetNextItemWidth(200.0f);
            var sendDescription = ev.SendDescription;
            var origSendDescription = sendDescription;
            ImGui.InputText("Send Description", ref sendDescription, 32);
            if (sendDescription != origSendDescription)
            {
                //TODO: change
            }
        }

        ImGui.SetNextItemWidth(200.0f);
        var isMessage = ev.IsMessage;
        var origIsMessage = isMessage;
        ImGui.Checkbox("Message?", ref isMessage);
        if (isMessage != origIsMessage)
        {
            //TODO: change
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200.0f);
        var isPackage = ev.IsPackage;
        var origIsPackage = isPackage;
        ImGui.Checkbox("Package?", ref isPackage);
        if (isPackage != origIsPackage)
        {
            //TODO: change
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200.0f);
        var isMeeting = ev.IsMeeting;
        var origIsMeeting = isMeeting;
        ImGui.Checkbox("Meeting?", ref isMeeting);
        if (isMeeting != origIsMeeting)
        {
            //TODO: change
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200.0f);
        var isBulletin = ev.IsBulletin;
        var origIsBulletin = isBulletin;
        ImGui.Checkbox("Bulletin?", ref isBulletin);
        if (isBulletin != origIsBulletin)
        {
            //TODO: change
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200.0f);
        var isU1 = ev.Unknown1;
        var origIsU1 = isU1;
        ImGui.Checkbox("Unknown 1?", ref isU1);
        if (isU1 != origIsU1)
        {
            //TODO: change
        }
        
        ImGui.SameLine();
        ImGui.Text(" ");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(200.0f);
        var score = ev.Score;
        var origScore = score;
        ImGui.InputInt("Score", ref score);
        if (score != origScore)
        {
            //TODO: change
        }

        if (intermediateCrime.Objects.Count > 0)
        {
            ImGui.Text("Received: ");
            ImGui.SameLine();
            ImGui.Text("");
            ImGui.SameLine();
            for (var objId = 0; objId < intermediateCrime.Objects.Count; objId++)
            {
                var isChecked = ev.ReceivedObjectIds.Contains(objId);
                var origIsChecked = isChecked;
                ImGui.SetNextItemWidth(100.0f);
                ImGui.Checkbox($"{intermediateCrime.Objects[objId].Name}", ref isChecked);
                ImGui.SameLine();
                if (isChecked != origIsChecked)
                {
                    //TODO: change
                }
            }

            ImGui.Text("");
            ImGui.Text("Destroyed:");
            ImGui.SameLine();
            ImGui.Text("");
            ImGui.SameLine();
            for (var objId = 0; objId < intermediateCrime.Objects.Count; objId++)
            {
                var isChecked = ev.DestroyedObjectIds.Contains(objId);
                var origIsChecked = isChecked;
                ImGui.SetNextItemWidth(100.0f);
                ImGui.Checkbox($"{intermediateCrime.Objects[objId].Name}", ref isChecked);
                ImGui.SameLine();
                if (isChecked != origIsChecked)
                {
                    //TODO: change
                }
            }

            ImGui.Text("");
        }
        
        ImGui.SetNextItemWidth(100.0f);
        var messageId = ev.MessageId;
        var origMessageId = messageId;
        ImGui.InputInt("Message ID", ref messageId);
        if (messageId != origMessageId)
        {
            ev.MessageId = messageId;
        }

        ImGui.BeginChild($"Message text {messageId}", new Vector2(ImGui.GetContentRegionAvail().X, 70.0f), true);
        var text = model.Texts.Values.FirstOrDefault(x => x.CrimeId == intermediateCrime.Id && x.Id == messageId);
        if (text != null)
        {
            ImGui.Text(text.Message);
        }
        else
        {
            ImGui.Text($"ERROR: Failed to find MSG{intermediateCrime.Id:D2}{messageId:D2}");
        }

        ImGui.EndChild();
        
        //TODO: received/destroyed items
        
        ImGui.EndChild();
    }

    private void DrawObject(PackageModel model, CrimeModel intermediateCrime, int i)
    {
        var windowSize = ImGui.GetContentRegionAvail();
        var obj = intermediateCrime.Objects[i];
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