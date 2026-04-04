using System.Numerics;
using System.Text;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using CovertActionTools.Core.Models.Executables;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedExecutableWindow : BaseWindow
{
    private readonly ILogger<SelectedExecutableWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly PendingEditorExecutableState _pendingState;

    public SelectedExecutableWindow(ILogger<SelectedExecutableWindow> logger, MainEditorState mainEditorState, PendingEditorExecutableState pendingState)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _pendingState = pendingState;
    }

    public override void Draw()
    {
        if (!_mainEditorState.IsPackageLoaded)
        {
            return;
        }

        if (_mainEditorState.SelectedItem == null ||
            _mainEditorState.SelectedItem.Value.type != MainEditorState.ItemType.Executable)
        {
            return;
        }

        var key = _mainEditorState.SelectedItem.Value.id;

        var screenSize = ImGui.GetMainViewport().Size;
        var initialPos = new Vector2(300.0f, 20.0f);
        var initialSize = new Vector2(screenSize.X - 300.0f, screenSize.Y - 200.0f);
        ImGui.SetNextWindowSize(initialSize);
        ImGui.SetNextWindowPos(initialPos);
        ImGui.Begin("Executable",
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoCollapse);

        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            DrawExecutableWindow(model, key);
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawExecutableWindow(PackageModel model, string key)
    {
        if (!model.Executables.ContainsKey(key))
        {
            ImGui.Text("Something went wrong, missing executable");
            return;
        }

        var exe = ImGuiExtensions.PendingSaveChanges(_pendingState, key,
            () => model.Executables[key].Clone(),
            (data) =>
            {
                model.Executables[key] = data;
                _mainEditorState.RecordChange();
                if (model.Index.ExecutableChanges.Add(key))
                {
                    model.Index.ExecutableIncluded.Add(key);
                }
            });

        ImGui.Text($"Executable: {key}");
        ImGui.Separator();

        if (exe.TacData != null) DrawTacData(exe.TacData);
        else if (exe.FinalData != null) DrawFinalData(exe.FinalData);
        else if (exe.GameData != null) DrawGameData(exe.GameData);
        else if (exe.BugData != null) DrawBugData(exe.BugData);
        else if (exe.ChaseData != null) DrawChaseData(exe.ChaseData);
        else if (exe.CodeData != null) DrawCodeData(exe.CodeData);
        else ImGui.Text("No data segment loaded for this executable.");
    }

    #region TAC

    private void DrawTacData(TacDataSegment tac)
    {
        if (ImGui.CollapsingHeader("Room Types"))
        {
            if (ImGui.BeginTable("RoomTypes", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Surv. Quality");
                ImGui.TableSetupColumn("Size Constraint");
                ImGui.TableSetupColumn("Enabled");
                ImGui.TableHeadersRow();

                for (var i = 0; i < tac.RoomTypes.Length; i++)
                {
                    ImGui.PushID($"RoomType_{i}");
                    var room = tac.RoomTypes[i];
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    var newName = ImGuiExtensions.Input("##Name", room.Name, TacRoomTypeRecord.NameLength, width: 150);
                    if (newName != null) { room.Name = newName; _pendingState.RecordChange(); }

                    ImGui.TableNextColumn();
                    var newSurvQuality = ImGuiExtensions.Input("##SurvQuality", (int)room.SurveillanceQuality, width: 80);
                    if (newSurvQuality != null) { room.SurveillanceQuality = (ushort)newSurvQuality.Value; _pendingState.RecordChange(); }

                    ImGui.TableNextColumn();
                    var sizeIdx = room.SizeConstraint == 1 ? 0 : room.SizeConstraint == 2 ? 1 :
                        room.SizeConstraint == 4 ? 2 : room.SizeConstraint == 3 ? 3 :
                        room.SizeConstraint == 5 ? 4 : room.SizeConstraint == 6 ? 5 :
                        room.SizeConstraint == 7 ? 6 : 0;
                    ImGui.SetNextItemWidth(120);
                    if (ImGui.Combo("##Size", ref sizeIdx, "Small\0Medium\0Large\0Small+Medium\0Small+Large\0Medium+Large\0All\0"))
                    {
                        var sizeValues = new ushort[] { 1, 2, 4, 3, 5, 6, 7 };
                        room.SizeConstraint = sizeValues[sizeIdx];
                        _pendingState.RecordChange();
                    }

                    ImGui.TableNextColumn();
                    var enabled = room.Enabled != 0;
                    if (ImGui.Checkbox("##Enabled", ref enabled))
                    {
                        room.Enabled = (ushort)(enabled ? 7 : 0);
                        _pendingState.RecordChange();
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }

        if (ImGui.CollapsingHeader("Objects"))
        {
            // Build room type names for placement checkboxes (from the room type records + Target Room)
            var roomTypeNames = tac.RoomTypes.Select(r => r.Name).ToList();
            roomTypeNames.Add("Target Room");

            for (var i = 0; i < tac.Objects.Length; i++)
            {
                ImGui.PushID($"Object_{i}");
                var obj = tac.Objects[i];
                var label = string.IsNullOrEmpty(obj.Name) ? $"Object {i}" : $"Object {i}: {obj.Name}";
                if (ImGui.CollapsingHeader(label))
                {
                    // TODO: Sprite X/Y should come from/go to the sprite sheet on the GUYS2/GUYS3 images
                    if (ImGui.BeginTable($"ObjBasic_{i}", 3))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        var newName = ImGuiExtensions.Input("Name", obj.Name, TacObjectRecord.NameLength, width: 120);
                        if (newName != null) { obj.Name = newName; _pendingState.RecordChange(); }

                        ImGui.TableNextColumn();
                        var newX = ImGuiExtensions.Input("Sprite X", (int)obj.SpriteOffset, width: 80);
                        if (newX != null) { obj.SpriteOffset = (ushort)newX.Value; _pendingState.RecordChange(); }

                        ImGui.TableNextColumn();
                        var newY = ImGuiExtensions.Input("Sprite Y", (int)obj.SpritePage, width: 80);
                        if (newY != null) { obj.SpritePage = (ushort)newY.Value; _pendingState.RecordChange(); }

                        ImGui.EndTable();
                    }

                    ImGui.Text("Behaviour Flags:");
                    DrawBehaviourFlags(obj);

                    ImGui.Text("Room Placement:");
                    DrawRoomPlacement(obj, roomTypeNames);
                }
                ImGui.PopID();
            }
        }

        if (ImGui.CollapsingHeader("Equipment Names"))
        {
            DrawStringArray(tac.EquipmentNames, "EquipName");
        }

        if (ImGui.CollapsingHeader("Equipment Nav Table"))
        {
            ImGui.Text("Cursor navigation grid: 12 equipment items x 4 directions");
            if (ImGui.BeginTable("EquipNav", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Item");
                ImGui.TableSetupColumn("Up");
                ImGui.TableSetupColumn("Down");
                ImGui.TableSetupColumn("Left");
                ImGui.TableSetupColumn("Right");
                ImGui.TableHeadersRow();

                for (var row = 0; row < 12 && row * 4 + 3 < tac.EquipmentNavTable.Length; row++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    var equipIdx = row + 1;
                    ImGui.Text(equipIdx < tac.EquipmentNames.Length && !string.IsNullOrEmpty(tac.EquipmentNames[equipIdx])
                        ? tac.EquipmentNames[equipIdx] : $"Item {equipIdx}");

                    for (var col = 0; col < 4; col++)
                    {
                        var idx = row * 4 + col;
                        ImGui.TableNextColumn();
                        ImGui.PushID($"EquipNav_{idx}");
                        var newVal = ImGuiExtensions.Input("##v", (int)tac.EquipmentNavTable[idx], width: 80);
                        if (newVal != null) { tac.EquipmentNavTable[idx] = (ushort)newVal.Value; _pendingState.RecordChange(); }
                        ImGui.PopID();
                    }
                }

                ImGui.EndTable();
            }
        }

        // Resolve equipment names from the pointer table for labelling ragdoll items
        var equipNames = tac.EquipmentNames;
        // 13 dest points (entries 0-12), 15 src rect pairs (entries 13-42), entry 43 is (0,0) terminator (hidden)
        var destCount = 13;
        var srcCount = 15;

        // TODO: Ragdoll dest points, source rects, and equipment slot rects map to the
        // spritesheets on EQUIP1/EQUIP1M and EQUIP2. The parser will need to generate those
        // spritesheets from this data and read them from there instead.
        if (ImGui.CollapsingHeader("Ragdoll Destination Points"))
        {
            if (ImGui.BeginTable("RagdollDest", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("#");
                ImGui.TableSetupColumn("Item");
                ImGui.TableSetupColumn("X");
                ImGui.TableSetupColumn("Y");
                ImGui.TableHeadersRow();

                for (var i = 0; i < destCount && i < tac.RagdollCoordinates.Length; i++)
                {
                    ImGui.PushID($"RagdollDest_{i}");
                    var coord = tac.RagdollCoordinates[i];
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text($"{i}");

                    ImGui.TableNextColumn();
                    ImGui.Text(GetRagdollDestLabel(i, equipNames));

                    ImGui.TableNextColumn();
                    var newX = ImGuiExtensions.Input("##X", (int)coord.X, width: 80);
                    if (newX != null) { coord.X = (ushort)newX.Value; _pendingState.RecordChange(); }

                    ImGui.TableNextColumn();
                    var newY = ImGuiExtensions.Input("##Y", (int)coord.Y, width: 80);
                    if (newY != null) { coord.Y = (ushort)newY.Value; _pendingState.RecordChange(); }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }

        if (ImGui.CollapsingHeader("Ragdoll Source Rects"))
        {
            if (ImGui.BeginTable("RagdollSrc", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("#");
                ImGui.TableSetupColumn("Item");
                ImGui.TableSetupColumn("X");
                ImGui.TableSetupColumn("Y");
                ImGui.TableSetupColumn("W");
                ImGui.TableSetupColumn("H");
                ImGui.TableHeadersRow();

                for (var i = 0; i < srcCount; i++)
                {
                    var tlIdx = destCount + i * 2;
                    var brIdx = tlIdx + 1;
                    if (brIdx >= tac.RagdollCoordinates.Length) break;

                    ImGui.PushID($"RagdollSrc_{i}");
                    var tl = tac.RagdollCoordinates[tlIdx];
                    var br = tac.RagdollCoordinates[brIdx];
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text($"{i}");

                    ImGui.TableNextColumn();
                    ImGui.Text(GetRagdollSrcLabel(i, equipNames));

                    ImGui.TableNextColumn();
                    var newX = ImGuiExtensions.Input("##X", (int)tl.X, width: 80);
                    if (newX != null) { tl.X = (ushort)newX.Value; _pendingState.RecordChange(); }

                    ImGui.TableNextColumn();
                    var newY = ImGuiExtensions.Input("##Y", (int)tl.Y, width: 80);
                    if (newY != null) { tl.Y = (ushort)newY.Value; _pendingState.RecordChange(); }

                    ImGui.TableNextColumn();
                    var w = br.X - tl.X;
                    var newW = ImGuiExtensions.Input("##W", w, width: 80);
                    if (newW != null) { br.X = (ushort)(tl.X + newW.Value); _pendingState.RecordChange(); }

                    ImGui.TableNextColumn();
                    var h = br.Y - tl.Y;
                    var newH = ImGuiExtensions.Input("##H", h, width: 80);
                    if (newH != null) { br.Y = (ushort)(tl.Y + newH.Value); _pendingState.RecordChange(); }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }

        if (ImGui.CollapsingHeader("Equipment Slot Rects"))
        {
            if (ImGui.BeginTable("EquipRects", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("#");
                ImGui.TableSetupColumn("Item");
                ImGui.TableSetupColumn("X");
                ImGui.TableSetupColumn("Y");
                ImGui.TableSetupColumn("W");
                ImGui.TableSetupColumn("H");
                ImGui.TableHeadersRow();

                for (var i = 0; i < tac.EquipmentSlotRects.Length; i++)
                {
                    ImGui.PushID($"EquipRect_{i}");
                    var rect = tac.EquipmentSlotRects[i];
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text($"{i}");

                    // Slot 0 = Uzi (equipment index 1), no Pistol in slot list
                    ImGui.TableNextColumn();
                    var equipIdx = i + 1;
                    ImGui.Text(equipIdx < equipNames.Length && !string.IsNullOrEmpty(equipNames[equipIdx])
                        ? equipNames[equipIdx] : $"Item {equipIdx}");

                    ImGui.TableNextColumn();
                    var newX = ImGuiExtensions.Input("##X", (int)rect.X1, width: 80);
                    if (newX != null) { rect.X1 = (ushort)newX.Value; _pendingState.RecordChange(); }

                    ImGui.TableNextColumn();
                    var newY = ImGuiExtensions.Input("##Y", (int)rect.Y1, width: 80);
                    if (newY != null) { rect.Y1 = (ushort)newY.Value; _pendingState.RecordChange(); }

                    ImGui.TableNextColumn();
                    var w = rect.X2 - rect.X1;
                    var newW = ImGuiExtensions.Input("##W", (int)w, width: 80);
                    if (newW != null) { rect.X2 = (ushort)(rect.X1 + newW.Value); _pendingState.RecordChange(); }

                    ImGui.TableNextColumn();
                    var h = rect.Y2 - rect.Y1;
                    var newH = ImGuiExtensions.Input("##H", (int)h, width: 80);
                    if (newH != null) { rect.Y2 = (ushort)(rect.Y1 + newH.Value); _pendingState.RecordChange(); }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }

        if (ImGui.CollapsingHeader("Character Names"))
        {
            DrawStringArray(tac.CharacterNames, "CharName");
        }

        if (ImGui.CollapsingHeader("Movement Pixel DX/DY"))
        {
            DrawDirectionTable("MvPx", CompassLabels9, tac.MovementPixelDX, tac.MovementPixelDY, "Walking pixel offsets per direction");
        }

        if (ImGui.CollapsingHeader("Jumping Tile DX/DY"))
        {
            DrawDirectionTable("JpTl", CompassLabels9, tac.JumpingTileDX, tac.JumpingTileDY, "Jumping tile offsets per direction");
        }

        if (ImGui.CollapsingHeader("Tile Adjacency DX/DY"))
        {
            DrawDirectionTable("TlAd", CardinalLabels4, tac.TileAdjacencyDX, tac.TileAdjacencyDY, "Cardinal tile adjacency for map generation and doors");
        }

        if (ImGui.CollapsingHeader("Clue Relationship Phrases"))
        {
            DrawStringArray(tac.ClueRelationshipPhrases, "CluePhrase", tac.CluePhraseSizes);
        }

        if (ImGui.CollapsingHeader("Month Abbreviations"))
        {
            DrawStringArray(tac.MonthAbbreviations, "Month", tac.MonthSizes);
        }

        if (ImGui.CollapsingHeader("Intel Headers"))
        {
            DrawStringArray(tac.IntelHeaders, "IntelHdr", tac.IntelHeaderSizes);
        }

        if (ImGui.CollapsingHeader("Intel Report Texts"))
        {
            DrawStringArray(tac.IntelReportTexts, "IntelTxt", tac.IntelReportTextSizes);
        }

        if (ImGui.CollapsingHeader("Rank Names"))
        {
            DrawStringArray(tac.RankNames, "Rank", tac.RankNameSizes);
        }

        if (ImGui.CollapsingHeader("Evidence Type Abbreviations"))
        {
            DrawStringArray(tac.EvidenceTypeAbbreviations, "EvType", tac.EvidenceTypeSizes);
        }

        if (ImGui.CollapsingHeader("Evidence Item Names"))
        {
            DrawStringArray(tac.EvidenceItemNames, "EvItem", tac.EvidenceItemSizes);
        }

        if (ImGui.CollapsingHeader("Investigation Methods"))
        {
            DrawStringArray(tac.InvestigationMethods, "InvMethod", tac.InvestigationMethodSizes);
        }

        DrawRawSectionSizes("Raw Sections", new[]
        {
            ("PreRoomData", tac.PreRoomData.Length),
            ("SpriteSheetConfigs", tac.SpriteSheetConfigs.Length),
            ("BssBlock", tac.BssBlock.Length),
            ("GameplayData", tac.GameplayData.Length),
            ("MidSectionPostEquipNames", tac.MidSectionPostEquipNames.Length),
            ("RagdollRectPadding", tac.RagdollRectPadding.Length),
            ("CluePhrasePointerTable", tac.CluePhrasePointerTable.Length),
            ("ItemCountData", tac.ItemCountData.Length),
            ("MonthPointerTable", tac.MonthPointerTable.Length),
            ("EvidenceRankPointerTable", tac.EvidenceRankPointerTable.Length),
            ("ClueSystemData", tac.ClueSystemData.Length),
            ("PostCharNameData", tac.PostCharNameData.Length),
            ("TrailingData", tac.TrailingData.Length)
        });
    }

    private void DrawBehaviourFlags(TacObjectRecord obj)
    {
        var flags = (int)obj.BehaviourFlags;
        if (ImGui.BeginTable("BehavFlags", 5))
        {
            ImGui.TableNextRow();
            DrawFlagCheckbox("Blocks Movement", ref flags, 0, obj);
            // Openable objects use Sprite Y+1 (the row below) as the open sprite
            DrawFlagCheckbox("Openable", ref flags, 1, obj);
            DrawFlagCheckbox("Buggable", ref flags, 2, obj);
            DrawFlagCheckbox("Photographable", ref flags, 3, obj);
            DrawFlagCheckbox("Is Door", ref flags, 4, obj);

            ImGui.TableNextRow();
            DrawFlagCheckbox("Blocks LOS", ref flags, 5, obj);
            DrawFlagCheckbox("Multi-tile", ref flags, 6, obj);
            DrawFlagCheckbox("Wall-Adjacent", ref flags, 8, obj);
            DrawFlagCheckbox("Password Terminal", ref flags, 9, obj);

            ImGui.EndTable();
        }

        // Show remaining high bits (10-15) as raw value if any are set
        var highBits = flags >> 10;
        if (highBits != 0)
        {
            ImGui.Text($"  Unknown high bits: 0x{highBits:X}");
        }
    }

    private void DrawFlagCheckbox(string label, ref int flags, int bit, TacObjectRecord obj)
    {
        ImGui.TableNextColumn();
        var val = (flags & (1 << bit)) != 0;
        var origVal = val;
        ImGui.Checkbox(label, ref val);
        if (val != origVal)
        {
            if (val) flags |= (1 << bit);
            else flags &= ~(1 << bit);
            obj.BehaviourFlags = (ushort)flags;
            _pendingState.RecordChange();
        }
    }

    private void DrawRoomPlacement(TacObjectRecord obj, List<string> roomTypeNames)
    {
        var flags = (int)obj.RoomPlacement;
        // Room placement bitfield: bit index = room type index
        var columns = Math.Min(roomTypeNames.Count, 6);
        if (ImGui.BeginTable("RoomPlace", columns))
        {
            for (var b = 0; b < roomTypeNames.Count; b++)
            {
                if (b % columns == 0) ImGui.TableNextRow();
                ImGui.TableNextColumn();
                var val = (flags & (1 << b)) != 0;
                var origVal = val;
                ImGui.Checkbox(roomTypeNames[b], ref val);
                if (val != origVal)
                {
                    if (val) flags |= (1 << b);
                    else flags &= ~(1 << b);
                    obj.RoomPlacement = (ushort)flags;
                    _pendingState.RecordChange();
                }
            }

            ImGui.EndTable();
        }

        // Show remaining high bits as raw value if any are set
        var usedBits = (1 << roomTypeNames.Count) - 1;
        var highBits = flags & ~usedBits;
        if (highBits != 0)
        {
            ImGui.Text($"  Unknown placement bits: 0x{highBits:X}");
        }
    }

    // TODO: Are the hardcoded ones just hardcoded from game logic or is the entire list
    // hardcoded? To investigate later.
    private static string GetRagdollDestLabel(int index, string[] equipNames)
    {
        // 13 dest points: first 11 from equipment names, last 2 are ammo types
        if (index == 11) return "Ammo Bullet";
        if (index == 12) return "Ammo Magazine";
        if (index < equipNames.Length && !string.IsNullOrEmpty(equipNames[index])) return equipNames[index];
        return $"Item {index}";
    }

    private static string GetRagdollSrcLabel(int index, string[] equipNames)
    {
        // 15 src rects: first 11 from equipment names, then 4 hardcoded
        if (index == 11) return "Ammo Bullet";
        if (index == 12) return "Ammo Magazine";
        if (index == 13) return "Wound";
        if (index == 14) return "Target";
        if (index < equipNames.Length && !string.IsNullOrEmpty(equipNames[index])) return equipNames[index];
        return $"Item {index}";
    }


    #endregion

    #region FINAL

    private void DrawFinalData(FinalDataSegment final)
    {
        if (ImGui.CollapsingHeader("Mission Sets"))
        {
            for (var i = 0; i < final.MissionSets.Length; i++)
            {
                ImGui.PushID($"MissionSet_{i}");
                var ms = final.MissionSets[i];
                var label = string.IsNullOrEmpty(ms.Name) ? $"Mission Set {i}" : $"Mission Set {i}: {ms.Name}";
                if (ImGui.CollapsingHeader(label))
                {
                    var newName = ImGuiExtensions.Input("Name", ms.Name, FinalMissionSetRecord.NameLength, width: 200);
                    if (newName != null) { ms.Name = newName; _pendingState.RecordChange(); }

                    if (ImGui.BeginTable($"MSFields", 2))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        var newU1 = ImGuiExtensions.Input("Unknown1", (int)ms.Unknown1, width: 80);
                        if (newU1 != null) { ms.Unknown1 = (byte)newU1.Value; _pendingState.RecordChange(); }

                        ImGui.TableNextColumn();
                        var newFlag = ImGuiExtensions.Input("Flag Word", (int)ms.FlagWord, width: 80);
                        if (newFlag != null) { ms.FlagWord = (ushort)newFlag.Value; _pendingState.RecordChange(); }

                        ImGui.EndTable();
                    }

                    // TODO: The Crime editor window needs to be able to load these crime type
                    // names from here instead of using hardcoded names.
                    DrawMissionSetCrimeSlot("Crime 1", ms, 0, final.CrimeTypeNames);
                    DrawMissionSetCrimeSlot("Crime 2", ms, 1, final.CrimeTypeNames);
                    DrawMissionSetCrimeSlot("Crime 3", ms, 2, final.CrimeTypeNames);

                    ImGui.Text($"Unused Crime Slots: {ms.UnusedCrimeSlots.Length} bytes (always 0xFF)");

                    if (ImGui.CollapsingHeader("Plot Strings"))
                    {
                        for (var slot = 0; slot < 7 && slot * 2 + 1 < ms.SlotStrings.Length; slot++)
                        {
                            var victim = ms.SlotStrings[slot * 2];
                            var item = ms.SlotStrings[slot * 2 + 1];
                            if (string.IsNullOrEmpty(victim) && string.IsNullOrEmpty(item)) continue;
                            ImGui.Text($"  Slot {slot} Victim: {(string.IsNullOrEmpty(victim) ? "(empty)" : victim)}");
                            ImGui.Text($"  Slot {slot} Item:   {(string.IsNullOrEmpty(item) ? "(empty)" : item)}");
                        }
                    }
                }

                ImGui.PopID();
            }
        }

        if (ImGui.CollapsingHeader("Crime Type Names"))
        {
            DrawStringArray(final.CrimeTypeNames, "CrimeType", final.CrimeTypeNameByteSizes);
        }

        if (ImGui.CollapsingHeader("Organisation Names"))
        {
            DrawStringArray(final.OrganisationNames, "OrgName", final.OrganisationNameByteSizes);
        }

        DrawReadOnlyInfo("Mission Set Parameters", $"{final.MissionSetParameters.Length} bytes (read-only)");
        if (ImGui.CollapsingHeader("Character Names"))
        {
            DrawStringArray(final.CharacterNames, "CharName");
        }

        DrawRawSectionSizes("Raw Sections", new[]
        {
            ("PreStringTableData", final.PreStringTableData.Length),
            ("PostStringTableData", final.PostStringTableData.Length),
            ("Unknown1", final.Unknown1.Length),
            ("PostMissionPreCrimeData", final.PostMissionPreCrimeData.Length),
            ("Unknown2", final.Unknown2.Length),
            ("PostOrgPreCharNameData", final.PostOrgPreCharNameData.Length),
            ("PostCharNameData", final.PostCharNameData.Length),
            ("TrailingData", final.TrailingData.Length)
        });
    }

    private void DrawMissionSetCrimeSlot(string label, FinalMissionSetRecord ms, int slotIndex, string[] crimeTypeNames)
    {
        var crimeId = slotIndex switch
        {
            0 => ms.Crime1Id,
            1 => ms.Crime2Id,
            2 => ms.Crime3Id,
            _ => (ushort)0xFFFF
        };

        var enabled = crimeId != 0xFFFF;
        var origEnabled = enabled;
        ImGui.Checkbox($"{label} Enabled", ref enabled);
        if (enabled != origEnabled)
        {
            var newId = enabled ? (ushort)0 : (ushort)0xFFFF;
            SetMissionSetCrimeId(ms, slotIndex, newId);
            _pendingState.RecordChange();
            crimeId = newId;
        }

        if (enabled)
        {
            ImGui.SameLine();
            // Build dropdown from crime type names
            var ids = Enumerable.Range(0, crimeTypeNames.Length).ToList();
            var labels = ids.Select(id => $"{id}: {crimeTypeNames[id]}").ToList();
            var currentIdx = ids.FindIndex(x => x == crimeId);
            if (currentIdx < 0) currentIdx = 0;
            var origIdx = currentIdx;
            ImGui.SetNextItemWidth(200.0f);
            ImGui.Combo($"##{label}", ref currentIdx, labels.ToArray(), labels.Count);
            if (currentIdx != origIdx)
            {
                SetMissionSetCrimeId(ms, slotIndex, (ushort)ids[currentIdx]);
                _pendingState.RecordChange();
            }
        }
    }

    private static void SetMissionSetCrimeId(FinalMissionSetRecord ms, int slotIndex, ushort value)
    {
        switch (slotIndex)
        {
            case 0: ms.Crime1Id = value; break;
            case 1: ms.Crime2Id = value; break;
            case 2: ms.Crime3Id = value; break;
        }
    }

    #endregion

    #region GAME

    private void DrawGameData(GameDataSegment game)
    {
        if (ImGui.CollapsingHeader("Character Names"))
        {
            DrawStringArray(game.CharacterNames, "CharName");
        }

        if (ImGui.CollapsingHeader("Clue Relationship Phrases"))
        {
            DrawStringArray(game.ClueRelationshipPhrases, "CluePhr");
        }

        DrawReadOnlyInfo("Unknown Lookup Table", $"{game.UnknownLookupTable.Length} bytes (read-only)");

        if (ImGui.CollapsingHeader("Month Names"))
        {
            DrawStringArray(game.MonthNames, "Month");
        }

        DrawRawSectionSizes("Raw Sections", new[]
        {
            ("PreCharNameData", game.PreCharNameData.Length),
            ("PostCharNameData", game.PostCharNameData.Length),
            ("MidSectionPreClue", game.MidSectionPreClue.Length),
            ("MidSectionPostMonth", game.MidSectionPostMonth.Length),
            ("TrailingData", game.TrailingData.Length)
        });
    }

    #endregion

    #region BUG

    private void DrawBugData(BugDataSegment bug)
    {
        if (ImGui.CollapsingHeader("Clue Relationship Phrases"))
        {
            DrawStringArray(bug.ClueRelationshipPhrases, "CluePhr");
        }

        if (ImGui.CollapsingHeader("Character Names"))
        {
            DrawStringArray(bug.CharacterNames, "CharName");
        }

        // TODO: Identify where record # comes from and if there is a name for each record.
        if (ImGui.CollapsingHeader("Rect Draw Records"))
        {
            if (ImGui.BeginTable("RectDraw", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("#");
                ImGui.TableSetupColumn("X");
                ImGui.TableSetupColumn("Y");
                ImGui.TableSetupColumn("W");
                ImGui.TableSetupColumn("H");
                ImGui.TableHeadersRow();

                for (var i = 0; i < bug.RectDrawRecords.Length; i++)
                {
                    ImGui.PushID($"Rect_{i}");
                    var rec = bug.RectDrawRecords[i];

                    // Row 1: X/Y/W/H
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text($"{i}");

                    ImGui.TableNextColumn();
                    var newX = ImGuiExtensions.Input("##X", (int)rec.X1, width: 100);
                    if (newX != null) { rec.X1 = (ushort)newX.Value; _pendingState.RecordChange(); }

                    ImGui.TableNextColumn();
                    var newY = ImGuiExtensions.Input("##Y", (int)rec.Y1, width: 100);
                    if (newY != null) { rec.Y1 = (ushort)newY.Value; _pendingState.RecordChange(); }

                    ImGui.TableNextColumn();
                    var w = rec.X2 - rec.X1;
                    var newW = ImGuiExtensions.Input("##W", (int)w, width: 100);
                    if (newW != null) { rec.X2 = (ushort)(rec.X1 + newW.Value); _pendingState.RecordChange(); }

                    ImGui.TableNextColumn();
                    var h = rec.Y2 - rec.Y1;
                    var newH = ImGuiExtensions.Input("##H", (int)h, width: 100);
                    if (newH != null) { rec.Y2 = (ushort)(rec.Y1 + newH.Value); _pendingState.RecordChange(); }

                    // Row 2: Flag/Colour
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    // empty # column

                    ImGui.TableNextColumn();
                    var newFlag = ImGuiExtensions.Input("Flag", (int)rec.Flag, width: 100);
                    if (newFlag != null) { rec.Flag = (byte)newFlag.Value; _pendingState.RecordChange(); }

                    ImGui.TableNextColumn();
                    var newCol = ImGuiExtensions.Input("Colour", (int)rec.Colour, width: 100);
                    if (newCol != null) { rec.Colour = (ushort)newCol.Value; _pendingState.RecordChange(); }

                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }

        DrawRawSectionSizes("Raw Sections", new[]
        {
            ("PreCluePhraseData", bug.PreCluePhraseData.Length),
            ("PostCluePhraseData", bug.PostCluePhraseData.Length),
            ("MidSectionPreCharNames", bug.MidSectionPreCharNames.Length),
            ("PostCharNameData", bug.PostCharNameData.Length),
            ("PostCharNamePtrData", bug.PostCharNamePtrData.Length),
            ("RectDrawTrailer", bug.RectDrawTrailer.Length),
            ("TrailingData", bug.TrailingData.Length)
        });
    }

    #endregion

    #region CHASE

    private void DrawChaseData(ChaseDataSegment chase)
    {
        if (ImGui.CollapsingHeader("Chase Narrative Strings"))
        {
            DrawStringArray(chase.ChaseNarrativeStrings, "Narrative", chase.ChaseNarrativeByteSizes);
        }

        // TODO: First entry (cars.pic) is likely a file reference that belongs in a separate
        // section, not a gameplay string. Investigate and split it out.
        if (ImGui.CollapsingHeader("Chase Gameplay Strings"))
        {
            DrawStringArray(chase.ChaseGameplayStrings, "Gameplay", chase.ChaseGameplayByteSizes);
        }

        DrawRawSectionSizes("Raw Sections", new[]
        {
            ("PreNarrativeData", chase.PreNarrativeData.Length),
            ("MidSection", chase.MidSection.Length),
            ("TrailingData", chase.TrailingData.Length)
        });
    }

    #endregion

    #region CODE

    private void DrawCodeData(CodeExeDataSegment code)
    {
        if (ImGui.CollapsingHeader("Graphics Library Docs"))
        {
            DrawStringArray(code.GraphicsLibraryDocs, "Doc");
        }

        DrawReadOnlyInfo("Nibble Sprite Data", $"{code.NibbleSpriteData.Length} bytes (read-only)");
        DrawReadOnlyInfo("Crypto Screen Params", $"{code.CryptoScreenParams.Length} bytes (read-only)");

        // TODO: Crypto alphabet data appears wrong/weird when parsed as strings — investigate
        // whether this region is actually null-terminated strings or a different structure.
        if (ImGui.CollapsingHeader("Crypto Alphabet Data"))
        {
            DrawStringArray(code.CryptoAlphabetData, "Alphabet", code.CryptoAlphabetByteSizes);
        }

        // TODO: Crypto UI strings appear to be mis-split — e.g. "No" and "Yes" show as
        // separate entries but are part of the quit dialog string. Investigate whether the
        // null-terminated string splitting is correct for this region.
        if (ImGui.CollapsingHeader("Crypto UI Strings"))
        {
            DrawStringArray(code.CryptoUiStrings, "CryptoUI", code.CryptoUiStringsByteSizes);
        }

        DrawRawSectionSizes("Raw Sections", new[]
        {
            ("PreDocData", code.PreDocData.Length),
            ("Unknown1", code.Unknown1.Length),
            ("TrailingData", code.TrailingData.Length)
        });
    }

    #endregion

    private static readonly string[] CompassLabels9 = { "None", "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    private static readonly string[] CardinalLabels4 = { "N", "E", "S", "W" };

    #region Shared Drawing Helpers

    // TODO: Allow different string lengths after pointer recalculation is implemented.
    // Currently each string is fixed to its original byte size to prevent pointer drift.
    private void DrawStringArray(string[] strings, string idPrefix, int[]? byteSizes = null)
    {
        for (var i = 0; i < strings.Length; i++)
        {
            ImGui.PushID($"{idPrefix}_{i}");
            var contentSize = ImGui.GetContentRegionAvail();
            // Max editable length = original byte size minus null terminator
            var maxLen = byteSizes != null && i < byteSizes.Length ? byteSizes[i] - 1 : 256;
            if (maxLen < 1) maxLen = 1;

            if (strings[i].Contains('\n'))
            {
                // Multiline for strings with newlines
                var val = strings[i];
                var origVal = val;
                ImGui.InputTextMultiline($"[{i}]", ref val, (uint)maxLen + 1,
                    new Vector2(contentSize.X - 80, 80.0f));
                if (val != origVal && val.Length <= maxLen)
                {
                    strings[i] = val;
                    _pendingState.RecordChange();
                }
            }
            else
            {
                var newVal = ImGuiExtensions.Input($"[{i}]", strings[i], maxLen, width: (int)contentSize.X - 80);
                if (newVal != null)
                {
                    strings[i] = newVal;
                    _pendingState.RecordChange();
                }
            }
            ImGui.PopID();
        }
    }

    private void DrawUShortArray(ushort[] values, string idPrefix, int columns)
    {
        if (ImGui.BeginTable($"{idPrefix}_table", columns + 1, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("#");
            for (var c = 0; c < columns; c++)
            {
                ImGui.TableSetupColumn($"+{c}");
            }
            ImGui.TableHeadersRow();

            for (var row = 0; row < values.Length; row += columns)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text($"{row}");

                for (var c = 0; c < columns && row + c < values.Length; c++)
                {
                    var idx = row + c;
                    ImGui.TableNextColumn();
                    ImGui.PushID($"{idPrefix}_{idx}");
                    var newVal = ImGuiExtensions.Input("##v", (int)values[idx], width: 100);
                    if (newVal != null) { values[idx] = (ushort)newVal.Value; _pendingState.RecordChange(); }
                    ImGui.PopID();
                }
            }

            ImGui.EndTable();
        }
    }

    private void DrawDirectionTable(string idPrefix, string[] labels, short[] dx, short[] dy, string description)
    {
        ImGui.Text(description);
        if (ImGui.BeginTable($"{idPrefix}_table", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Direction");
            ImGui.TableSetupColumn("DX");
            ImGui.TableSetupColumn("DY");
            ImGui.TableHeadersRow();

            var count = Math.Min(labels.Length, Math.Min(dx.Length, dy.Length));
            for (var i = 0; i < count; i++)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(labels[i]);

                ImGui.TableNextColumn();
                ImGui.PushID($"{idPrefix}_dx_{i}");
                var newDx = ImGuiExtensions.Input("##v", (int)dx[i], width: 80);
                if (newDx != null) { dx[i] = (short)newDx.Value; _pendingState.RecordChange(); }
                ImGui.PopID();

                ImGui.TableNextColumn();
                ImGui.PushID($"{idPrefix}_dy_{i}");
                var newDy = ImGuiExtensions.Input("##v", (int)dy[i], width: 80);
                if (newDy != null) { dy[i] = (short)newDy.Value; _pendingState.RecordChange(); }
                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }

    private void DrawShortArray(short[] values, string idPrefix, int columns)
    {
        if (ImGui.BeginTable($"{idPrefix}_table", columns + 1, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("#");
            for (var c = 0; c < columns; c++)
            {
                ImGui.TableSetupColumn($"+{c}");
            }
            ImGui.TableHeadersRow();

            for (var row = 0; row < values.Length; row += columns)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text($"{row}");

                for (var c = 0; c < columns && row + c < values.Length; c++)
                {
                    var idx = row + c;
                    ImGui.TableNextColumn();
                    ImGui.PushID($"{idPrefix}_{idx}");
                    var newVal = ImGuiExtensions.Input("##v", (int)values[idx], width: 100);
                    if (newVal != null) { values[idx] = (short)newVal.Value; _pendingState.RecordChange(); }
                    ImGui.PopID();
                }
            }

            ImGui.EndTable();
        }
    }

    private void DrawReadOnlyInfo(string label, string value)
    {
        ImGui.Text($"{label}: {value}");
    }

    private void DrawRawSectionSizes(string header, (string name, int size)[] sections)
    {
        if (ImGui.CollapsingHeader(header))
        {
            foreach (var (name, size) in sections)
            {
                ImGui.Text($"  {name}: {size} bytes");
            }
        }
    }

    #endregion
}
