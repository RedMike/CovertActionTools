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
    private readonly PendingEditorCrimeState _pendingState;

    private string _idError = "";

    public SelectedCrimeWindow(ILogger<SelectedCrimeWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow, ICrimeTimelineProcessor crimeTimelineProcessor, PendingEditorCrimeState pendingState)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _renderWindow = renderWindow;
        _crimeTimelineProcessor = crimeTimelineProcessor;
        _pendingState = pendingState;
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
        ImGui.Begin("Crime",
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);
        
        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            DrawCrimeWindow(model, key);
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawCrimeWindow(PackageModel model, int key)
    {
        if (!model.Crimes.ContainsKey(key))
        {
            ImGui.Text("Something went wrong, missing crime");
            return;
        }
        var crime = ImGuiExtensions.PendingSaveChanges(_pendingState, key.ToString(),
            () => model.Crimes[key].Clone(),
            (data) =>
            {
                model.Crimes[key] = data;
                _mainEditorState.RecordChange();
                if (model.Index.CrimeChanges.Add(key))
                {
                    model.Index.CrimeIncluded.Add(key);
                }
            });

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
                _pendingState.RecordChange();
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
            
            for (var i = 0; i < crime.Events.Count; i++)
            {
                ImGui.PushID($"Event_{i}");
                DrawEvent(model, crime, i);
                ImGui.Text("");
                ImGui.PopID();
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
            _pendingState.RecordChange();
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
                _pendingState.RecordChange();
            }
        
            ImGui.TableNextColumn();
            var newExposure = ImGuiExtensions.Input("Exposure", participant.Exposure, width: 150);
            if (newExposure != null)
            {
                participant.Exposure = newExposure.Value;
                _pendingState.RecordChange();
            }
        
            ImGui.TableNextColumn();
            var newClueType = ImGuiExtensions.InputEnum("Clue Type", participant.ClueType, false, ClueType.Unknown, width: 150);
            if (newClueType != null)
            {
                participant.ClueType = newClueType.Value;
                _pendingState.RecordChange();
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
                _pendingState.RecordChange();
            }
        
            
            ImGui.TableNextColumn();
            var newFemale = ImGuiExtensions.Input("Force Female?", participant.ForceFemale);
            if (newFemale != null)
            {
                participant.ForceFemale = newFemale.Value;
                _pendingState.RecordChange();
            }
            
            ImGui.TableNextColumn();
            var canComeOut = ImGuiExtensions.Input("Back from Hiding?", participant.CanComeOutOfHiding);
            if (canComeOut != null)
            {
                participant.CanComeOutOfHiding = canComeOut.Value;
                _pendingState.RecordChange();
            }
            
            ImGui.TableNextColumn();
            var insideContact = ImGuiExtensions.Input("Inside Contact?", participant.IsInsideContact);
            if (insideContact != null)
            {
                participant.IsInsideContact = insideContact.Value;
                participant.Unknown2 = (participant.Unknown2 & 0xFE) | (insideContact.Value ? 0x01 : 0x00);
                _pendingState.RecordChange();
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
                _pendingState.RecordChange();
            }

            ImGui.TableNextColumn();
            var newU2 = ImGuiExtensions.Input("Unknown 2", $"{participant.Unknown2:B8}", 10);
            if (newU2 != null && int.TryParse(newU2, NumberStyles.BinaryNumber, null, out var u2Parsed))
            {
                participant.Unknown2 = u2Parsed;
                participant.IsInsideContact = (u2Parsed & 0x01) == 0x01;
                _pendingState.RecordChange();
            }
            
            ImGui.EndTable();
        }
        
        ImGui.EndChild();
    }

    private void DrawEvent(PackageModel model, CrimeModel crime, int i)
    {
        var participantList = crime.Participants
            .Select((x, idx) => string.IsNullOrEmpty(x.Role) ? $"Participant {idx + 1}" : x.Role)
            .ToList();
        var windowSize = ImGui.GetContentRegionAvail();
        
        var ev = crime.Events[i];
        ImGui.BeginChild($"Event {i}", new Vector2(windowSize.X, 250.0f), true);

        var cursorPos = ImGui.GetCursorPos();
        ImGui.Text($"Event {i + 1}");
        var nextCursorPos = ImGui.GetCursorPos();
        
        //now move to the right to make the delete button
        var o = ImGui.CalcTextSize(" Remove ");
        ImGui.SetCursorPos(new Vector2(windowSize.X - o.X, cursorPos.Y));
        if (ImGui.Button("Remove"))
        {
            crime.Participants.RemoveAt(i);
            _pendingState.RecordChange();
            return;
        }
        
        ImGui.SetCursorPos(nextCursorPos);

        if (ImGui.BeginTable("e1", 3))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var newParticipant = ImGuiExtensions.Input("Main", participantList[ev.MainParticipantId], participantList);
            if (newParticipant != null)
            {
                ev.MainParticipantId = participantList.FindIndex(x => x == newParticipant);
                _pendingState.RecordChange();
            }

            ImGui.TableNextColumn();
            if (ev.IsPairedEvent)
            {
                var secondParticipant = ImGuiExtensions.Input("Secondary", participantList[ev.SecondaryParticipantId ?? 0], participantList);
                if (secondParticipant != null)
                {
                    ev.SecondaryParticipantId = participantList.FindIndex(x => x == secondParticipant);
                    _pendingState.RecordChange();
                }
                
                ImGui.TableNextColumn();
                var itemsToSecondary = ImGuiExtensions.Input("Items to Secondary?", ev.ItemsToSecondary);
                if (itemsToSecondary != null)
                {
                    ev.ItemsToSecondary = itemsToSecondary.Value;
                    _pendingState.RecordChange();
                }
            }
            else
            {
                ImGui.Text("No secondary participant");
                ImGui.TableNextColumn();
            }
            
            ImGui.EndTable();
        }

        if (ImGui.BeginTable("e2", 2))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var newDescription = ImGuiExtensions.Input(ev.IsPairedEvent ? "Receive Description" : "Description",
                ev.ReceiveDescription, 32);
            if (newDescription != null)
            {
                ev.ReceiveDescription = newDescription;
                if (!ev.IsPairedEvent)
                {
                    ev.SendDescription = newDescription;
                }
                _pendingState.RecordChange();
            }

            ImGui.TableNextColumn();
            if (ev.IsPairedEvent)
            {
                var newSendDescription = ImGuiExtensions.Input("Send Description", ev.SendDescription, 32);
                if (newSendDescription != null)
                {
                    ev.SendDescription = newSendDescription;
                    _pendingState.RecordChange();
                }
            }

            ImGui.EndTable();
        }

        if (ImGui.BeginTable("e3", 6))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var isMessage = ImGuiExtensions.Input("Message?", ev.IsMessage);
            if (isMessage != null)
            {
                ev.IsMessage = isMessage.Value;
                _pendingState.RecordChange();
            }
            
            ImGui.TableNextColumn();
            var isPackage = ImGuiExtensions.Input("Package?", ev.IsPackage);
            if (isPackage != null)
            {
                ev.IsPackage = isPackage.Value;
                _pendingState.RecordChange();
            }
            
            ImGui.TableNextColumn();
            var isMeeting = ImGuiExtensions.Input("Meeting?", ev.IsMeeting);
            if (isMeeting != null)
            {
                ev.IsMeeting = isMeeting.Value;
                _pendingState.RecordChange();
            }
            
            ImGui.TableNextColumn();
            var isBulletin = ImGuiExtensions.Input("Bulletin?", ev.IsBulletin);
            if (isBulletin != null)
            {
                ev.IsBulletin = isBulletin.Value;
                _pendingState.RecordChange();
            }
            
            ImGui.TableNextColumn();
            var isU1 = ImGuiExtensions.Input("Unknown 1?", ev.Unknown1);
            if (isU1 != null)
            {
                ev.Unknown1 = isU1.Value;
                _pendingState.RecordChange();
            }
            
            ImGui.TableNextColumn();
            var score = ImGuiExtensions.Input("Score", ev.Score);
            if (score != null)
            {
                ev.Score = score.Value;
                _pendingState.RecordChange();
            }
            
            ImGui.EndTable();
        }

        if (ImGui.BeginTable("e4", 5))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Received:");

            for (var objId = 0; objId < 4; objId++)
            {
                ImGui.TableNextColumn();
                if (crime.Objects.Count > objId)
                {
                    var obj = crime.Objects[objId];
                    var objIsReceived = ImGuiExtensions.Input($"{obj.Name}", ev.ReceivedObjectIds.Contains(objId));
                    if (objIsReceived != null)
                    {
                        if (objIsReceived.Value)
                        {
                            ev.ReceivedObjectIds.Add(objId);
                        }
                        else
                        {
                            ev.ReceivedObjectIds.Remove(objId);
                        }
                        _pendingState.RecordChange();
                    }
                }
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            
            ImGui.Text("Destroyed:");

            for (var objId = 0; objId < 4; objId++)
            {
                ImGui.TableNextColumn();
                if (crime.Objects.Count > objId)
                {
                    var obj = crime.Objects[objId];
                    var objIsReceived = ImGuiExtensions.Input($"{obj.Name}", ev.DestroyedObjectIds.Contains(objId));
                    if (objIsReceived != null)
                    {
                        if (objIsReceived.Value)
                        {
                            ev.DestroyedObjectIds.Add(objId);
                        }
                        else
                        {
                            ev.DestroyedObjectIds.Remove(objId);
                        }
                        _pendingState.RecordChange();
                    }
                }
            }

            ImGui.EndTable();
        }

        var messageId = ImGuiExtensions.Input("Message ID", ev.MessageId, width: 100);
        if (messageId != null)
        {
            ev.MessageId = messageId.Value;
            _pendingState.RecordChange();
        }

        ImGui.BeginChild($"Message text {messageId}", new Vector2(ImGui.GetContentRegionAvail().X, 50.0f), true, ImGuiWindowFlags.NoScrollbar);
        var text = model.Texts.Values.FirstOrDefault(x => x.CrimeId == crime.Id && x.Id == ev.MessageId);
        if (text != null)
        {
            ImGui.Text(text.Message);
        }
        else
        {
            ImGui.Text($"ERROR: Failed to find MSG{crime.Id:D2}{messageId:D2}");
        }
        ImGui.EndChild();
        
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
            _pendingState.RecordChange();
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
            _pendingState.RecordChange();
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
                    iconBytes.Add(iconImage.Image.VgaImageData[(y * iconImage.Image.Data.Width + ox + x) * 4 + 0]);
                    iconBytes.Add(iconImage.Image.VgaImageData[(y * iconImage.Image.Data.Width + ox + x) * 4 + 1]);
                    iconBytes.Add(iconImage.Image.VgaImageData[(y * iconImage.Image.Data.Width + ox + x) * 4 + 2]);
                    iconBytes.Add(iconImage.Image.VgaImageData[(y * iconImage.Image.Data.Width + ox + x) * 4 + 3]);
                }
            }

            var texture = _renderWindow.RenderImage(RenderWindow.RenderType.Icon, iconImageId, 16, 16, iconBytes.ToArray());

            ImGui.Image(texture, new Vector2(16, 16));
        }
        ImGui.EndChild();
    }
}