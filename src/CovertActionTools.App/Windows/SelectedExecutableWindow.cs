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
            ("ClueCategoryData", tac.ClueCategoryData.Length),
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

                    ImGui.Text("Org Alliances:");
                    ImGui.SameLine();
                    var orgMask = (int)ms.OrgTypeMask;
                    var a1 = (orgMask & 0x01) != 0;
                    var a2 = (orgMask & 0x02) != 0;
                    var a3 = (orgMask & 0x04) != 0;
                    var a4 = (orgMask & 0x08) != 0;
                    if (ImGui.Checkbox("1##org", ref a1)) { ms.OrgTypeMask = (byte)((orgMask & ~0x01) | (a1 ? 0x01 : 0)); _pendingState.RecordChange(); }
                    ImGui.SameLine();
                    if (ImGui.Checkbox("2##org", ref a2)) { ms.OrgTypeMask = (byte)((orgMask & ~0x02) | (a2 ? 0x02 : 0)); _pendingState.RecordChange(); }
                    ImGui.SameLine();
                    if (ImGui.Checkbox("3##org", ref a3)) { ms.OrgTypeMask = (byte)((orgMask & ~0x04) | (a3 ? 0x04 : 0)); _pendingState.RecordChange(); }
                    ImGui.SameLine();
                    if (ImGui.Checkbox("4##org", ref a4)) { ms.OrgTypeMask = (byte)((orgMask & ~0x08) | (a4 ? 0x08 : 0)); _pendingState.RecordChange(); }

                    // TODO: The Crime editor window needs to be able to load these crime type
                    // names from here instead of using hardcoded names.
                    DrawMissionSetCrimeSlot("Crime 1", ms, 0, final.CrimeTypeNames);
                    DrawMissionSetCrimeSlot("Crime 2", ms, 1, final.CrimeTypeNames);
                    DrawMissionSetCrimeSlot("Crime 3", ms, 2, final.CrimeTypeNames);
                    DrawMissionSetCrimeSlot("Crime 4", ms, 3, final.CrimeTypeNames);
                    DrawMissionSetCrimeSlot("Crime 5", ms, 4, final.CrimeTypeNames);
                    DrawMissionSetCrimeSlot("Crime 6", ms, 5, final.CrimeTypeNames);

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

        if (ImGui.CollapsingHeader("Initial Game Strings"))
        {
            ImGui.TextWrapped("Startup strings: env.sve sentinels, joystick prompts, main menu, file references.");
            DrawStringArray(final.InitialGameStrings, "InitGame", final.InitialGameStringSizes);
        }

        if (ImGui.CollapsingHeader("CGA Animation Data"))
        {
            ImGui.TextWrapped($"2bpp CGA sprite data + CGA-to-VGA palette ({final.CgaAnimationData.Length} bytes). Shared across FINAL/TAC/GAME.");
            if (final.CgaAnimationData.Length > 0)
            {
                var hexLines = new System.Text.StringBuilder();
                for (var i = 0; i < final.CgaAnimationData.Length; i += 16)
                {
                    var lineLen = Math.Min(16, final.CgaAnimationData.Length - i);
                    hexLines.Append($"{i:X4}: ");
                    for (var j = 0; j < lineLen; j++)
                        hexLines.Append($"{final.CgaAnimationData[i + j]:X2} ");
                    for (var j = lineLen; j < 16; j++)
                        hexLines.Append("   ");
                    hexLines.Append(" ");
                    for (var j = 0; j < lineLen; j++)
                    {
                        var b = final.CgaAnimationData[i + j];
                        hexLines.Append(b >= 0x20 && b <= 0x7E ? (char)b : '.');
                    }
                    hexLines.AppendLine();
                }
                var hexText = hexLines.ToString();
                ImGui.InputTextMultiline("##CgaHex", ref hexText, (uint)hexText.Length + 1,
                    new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X - 20, 200.0f),
                    ImGuiInputTextFlags.ReadOnly);
            }
        }

        if (ImGui.CollapsingHeader("Text Lookup Tag Pairs"))
        {
            ImGui.TextWrapped("Tag+filename pairs for text.dta lookups. Tags have digits patched at runtime (e.g. *SLOC00 -> *SLOC03).");
            DrawStringArray(final.TextLookupTagPairs, "TagPair", final.TextLookupTagPairSizes);
        }

        if (ImGui.CollapsingHeader("Crime Type Names"))
        {
            DrawStringArray(final.CrimeTypeNames, "CrimeType", final.CrimeTypeNameByteSizes);
        }

        if (ImGui.CollapsingHeader("Organisation Names"))
        {
            DrawStringArray(final.OrganisationNames, "OrgName", final.OrganisationNameByteSizes);
        }

        if (ImGui.CollapsingHeader("Copyright Org Head Appearances"))
        {
            for (var i = 0; i < final.CopyrightOrgHeads.Length; i++)
            {
                ImGui.PushID($"OrgApp_{i}");
                var oa = final.CopyrightOrgHeads[i];
                var orgName = i < final.OrganisationNames.Length ? final.OrganisationNames[i] : $"Org {i}";
                if (ImGui.CollapsingHeader($"Org {i}: {orgName}"))
                {
                    if (ImGui.BeginTable($"OrgAppFields", 4))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        var g = ImGuiExtensions.Input("Gender", (int)oa.Gender, width: 60);
                        if (g != null) { oa.Gender = (ushort)g.Value; _pendingState.RecordChange(); }
                        ImGui.TableNextColumn();
                        var sk = ImGuiExtensions.Input("Skin Colour", (int)oa.SkinColour, width: 60);
                        if (sk != null) { oa.SkinColour = (ushort)sk.Value; _pendingState.RecordChange(); }
                        ImGui.TableNextColumn();
                        var cl = ImGuiExtensions.Input("Clothing", (int)oa.ClothingSprite, width: 60);
                        if (cl != null) { oa.ClothingSprite = (ushort)cl.Value; _pendingState.RecordChange(); }
                        ImGui.TableNextColumn();
                        var hc = ImGuiExtensions.Input("Hair Colour", (int)oa.HairColour, width: 60);
                        if (hc != null) { oa.HairColour = (ushort)hc.Value; _pendingState.RecordChange(); }
                        ImGui.EndTable();
                    }
                    if (ImGui.BeginTable($"OrgAppFace", 4))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        var mo = ImGuiExtensions.Input("Mouth", (int)oa.Mouth, width: 60);
                        if (mo != null) { oa.Mouth = (ushort)mo.Value; _pendingState.RecordChange(); }
                        ImGui.TableNextColumn();
                        var no = ImGuiExtensions.Input("Nose", (int)oa.Nose, width: 60);
                        if (no != null) { oa.Nose = (ushort)no.Value; _pendingState.RecordChange(); }
                        ImGui.TableNextColumn();
                        var ey = ImGuiExtensions.Input("Eyes", (int)oa.Eyes, width: 60);
                        if (ey != null) { oa.Eyes = (ushort)ey.Value; _pendingState.RecordChange(); }
                        ImGui.TableNextColumn();
                        var ha = ImGuiExtensions.Input("Hair", (int)oa.Hair, width: 60);
                        if (ha != null) { oa.Hair = (ushort)ha.Value; _pendingState.RecordChange(); }
                        ImGui.EndTable();
                    }
                }
                ImGui.PopID();
            }
        }
        if (ImGui.CollapsingHeader("Plot File Strings (tentative)"))
        {
            ImGui.TextWrapped("Mission setup plot file references and hardcoded slot 7 strings. TODO: investigate *PL000A/*PL000a alternate plot ref logic.");
            DrawStringArray(final.PlotFileStrings, "PlotStr");
        }

        if (ImGui.CollapsingHeader("Character Creation Strings"))
        {
            ImGui.TextWrapped("Character setup: gender image, name/difficulty selection menus (\\n = line separator, leading space = selectable item).");
            DrawStringArray(final.CharacterSetupStrings, "CharSetup", final.CharacterSetupStringSizes);
            ImGui.Separator();
            ImGui.TextWrapped("Code name prompt and skill selection (from plot/briefing data):");
            DrawStringArray(final.CharacterCreationStrings, "CharCreate");
        }

        if (ImGui.CollapsingHeader("Skill Names"))
        {
            for (var i = 0; i < final.SkillNames.Length; i++)
            {
                ImGui.PushID($"Skill_{i}");
                var isStamina = i == 4;
                if (isStamina)
                {
                    ImGui.BeginDisabled();
                    ImGui.InputText($"[{i}] (unused in game)", ref final.SkillNames[i], 64);
                    ImGui.EndDisabled();
                }
                else
                {
                    var contentSize = ImGui.GetContentRegionAvail();
                    var newVal = ImGuiExtensions.Input($"[{i}]", final.SkillNames[i], 256, width: (int)contentSize.X - 80);
                    if (newVal != null)
                    {
                        final.SkillNames[i] = newVal;
                        _pendingState.RecordChange();
                    }
                }
                ImGui.PopID();
            }
        }

        if (ImGui.CollapsingHeader("Training Screen Strings"))
        {
            DrawStringArray(final.TrainingScreenStrings, "TrainStr");
        }

        if (ImGui.CollapsingHeader("Training Screen Data"))
        {
            ImGui.Text("Skill bar color indices (one per display slot):");
            for (var i = 0; i < final.TrainingScreenColorWords.Length; i++)
            {
                var v = ImGuiExtensions.Input($"Color [{i}]", (int)final.TrainingScreenColorWords[i], width: 60);
                if (v != null) { final.TrainingScreenColorWords[i] = (ushort)v.Value; _pendingState.RecordChange(); }
            }

            ImGui.Separator();
            ImGui.Text("Column X positions (one per skill bar column):");
            for (var i = 0; i < final.TrainingScreenColumnXCoords.Length; i++)
            {
                if (i > 0) ImGui.SameLine();
                ImGui.SetNextItemWidth(60.0f);
                var val = (int)final.TrainingScreenColumnXCoords[i];
                if (ImGui.InputInt($"##ColX{i}", ref val))
                {
                    final.TrainingScreenColumnXCoords[i] = (byte)Math.Clamp(val, 0, 255);
                    _pendingState.RecordChange();
                }
            }

            ImGui.Separator();
            ImGui.Text("VGA palette remap table (16 entries, index -> color):");
            for (var i = 0; i < final.TrainingScreenPaletteRemap.Length; i++)
            {
                if (i > 0 && i % 8 != 0) ImGui.SameLine();
                ImGui.SetNextItemWidth(60.0f);
                var val = (int)final.TrainingScreenPaletteRemap[i];
                if (ImGui.InputInt($"[{i}]##{i}", ref val))
                {
                    final.TrainingScreenPaletteRemap[i] = (byte)val;
                    _pendingState.RecordChange();
                }
            }
        }

        if (ImGui.CollapsingHeader("Copyright Protection Strings"))
        {
            DrawStringArray(final.CopyrightProtectionStrings, "CopyProt");
        }

        if (ImGui.CollapsingHeader("Game Progress Strings"))
        {
            ImGui.TextWrapped("Briefing templates, case wrap-up, promotion, retirement, continue/save/end menus. TODO: some strings contain multiple menu options as one newline-separated string.");
            DrawStringArray(final.GameProgressStrings, "GameProg");
        }

        if (ImGui.CollapsingHeader("RastPort Display Context (tentative)"))
        {
            ImGui.TextWrapped("20-byte graphics context descriptor for the briefing panel. 320x200 VGA.");
            if (ImGui.BeginTable("RastPort", 2))
            {
                ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.Text("Data Offset");
                ImGui.TableNextColumn();
                var rdo = ImGuiExtensions.Input("##RPDO", (int)final.RastPortDataOffset, width: 80);
                if (rdo != null) { final.RastPortDataOffset = (ushort)rdo.Value; _pendingState.RecordChange(); }

                ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.Text("Page");
                ImGui.TableNextColumn();
                var rp = ImGuiExtensions.Input("##RPPage", (int)final.RastPortPage, width: 80);
                if (rp != null) { final.RastPortPage = (ushort)rp.Value; _pendingState.RecordChange(); }

                ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.Text("Flag");
                ImGui.TableNextColumn();
                var rf = ImGuiExtensions.Input("##RPFlag", (int)final.RastPortFlag, width: 80);
                if (rf != null) { final.RastPortFlag = (ushort)rf.Value; _pendingState.RecordChange(); }

                ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.Text("Extent X");
                ImGui.TableNextColumn(); ImGui.Text($"{final.RastPortExtentX} (read-only)");

                ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.Text("Extent Y");
                ImGui.TableNextColumn(); ImGui.Text($"{final.RastPortExtentY} (read-only)");

                ImGui.EndTable();
            }
        }

        if (ImGui.CollapsingHeader("Chronology Format Strings (tentative)"))
        {
            ImGui.TextWrapped("Format tokens and event phrases for building case chronology text. Empty entries are intentional format placeholders.");
            DrawStringArray(final.ChronologyFormatStrings, "ChronStr");
        }

        if (ImGui.CollapsingHeader("Time Template (tentative)"))
        {
            ImGui.TextWrapped("Time/date display template. Digits and month are overwritten at runtime. Only month+day portion is shown in-game (e.g. 'Jan 08'). Fixed separator characters (:, spaces, M) are preserved.");
            ImGui.Text("Template (HH:MM AM Mon DD):");
            var tmpl = final.TimeTemplateBuffer;
            var contentSize = ImGui.GetContentRegionAvail();
            var newTmpl = ImGuiExtensions.Input("##TimeTemplate", tmpl, 32, width: (int)contentSize.X - 80);
            if (newTmpl != null)
            {
                final.TimeTemplateBuffer = newTmpl;
                _pendingState.RecordChange();
            }
        }

        if (ImGui.CollapsingHeader("Efficiency Report Strings (tentative)"))
        {
            ImGui.TextWrapped("Efficiency report display strings. Contains 0x89 bytes whose purpose is unconfirmed — the text renderer stops at bytes >= 0x80 but the full call chain is not yet traced. See scratch docs for investigation notes.");
            for (var i = 0; i < final.EfficiencyReportStrings.Length; i++)
            {
                ImGui.PushID($"EffRpt_{i}");
                var s = final.EfficiencyReportStrings[i];
                // Display with 0x89 shown as \x89 for readability
                var display = s.Replace("\x89", "\\x89");
                ImGui.TextDisabled($"[{i}]");
                ImGui.SameLine();
                ImGui.Text(display);
                ImGui.PopID();
            }
        }

        if (ImGui.CollapsingHeader("Character Names"))
        {
            DrawStringArray(final.CharacterNames, "CharName");
        }

        if (ImGui.CollapsingHeader("Career Review Strings"))
        {
            DrawStringArray(final.CareerReviewStrings, "CareerRev", final.CareerReviewStringSizes);
        }

        if (ImGui.CollapsingHeader("Mission End Strings"))
        {
            ImGui.TextWrapped("gender.pic, character name defaults, scene codes (lau/off/bch/cas), flavour texts, file refs, filename fragments (dude/babe/.pic).");
            DrawStringArray(final.MissionEndStrings, "MissionEnd", final.MissionEndStringSizes);
        }

        if (ImGui.CollapsingHeader("Mission End Scene Table"))
        {
            ImGui.TextWrapped("21 records (4 scenes x 5 score variations + 1 all-masterminds). Word[0]=scene index (0-3: lau/off/bch/cas), words[1-4]=sub-image numbers (-1=unused).");
            if (ImGui.BeginTable("SceneRecords", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("#");
                ImGui.TableSetupColumn("Scene");
                ImGui.TableSetupColumn("Img 1");
                ImGui.TableSetupColumn("Img 2");
                ImGui.TableSetupColumn("Img 3");
                ImGui.TableSetupColumn("Img 4");
                ImGui.TableHeadersRow();

                for (var r = 0; r < 21; r++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text($"{r}");
                    for (var w = 0; w < 5; w++)
                    {
                        ImGui.TableNextColumn();
                        var idx = r * 5 + w;
                        if (idx < final.MissionEndSceneRecords.Length)
                        {
                            var val = (int)(short)final.MissionEndSceneRecords[idx];
                            ImGui.SetNextItemWidth(80.0f);
                            if (ImGui.InputInt($"##Scene_{r}_{w}", ref val))
                            {
                                final.MissionEndSceneRecords[idx] = (ushort)val;
                                _pendingState.RecordChange();
                            }
                        }
                    }
                }
                ImGui.EndTable();
            }
        }

        if (ImGui.CollapsingHeader("Briefing Strings"))
        {
            ImGui.TextWrapped("Briefing intro, region descriptions, mission text, practice prompt, file refs (briefing.pan, 10.dta, crime0.dta, world0.dta).");
            DrawStringArray(final.BriefingStrings, "BriefStr", final.BriefingStringSizes);
        }

        if (ImGui.CollapsingHeader("Hall of Fame Strings"))
        {
            ImGui.TextWrapped("fame.dta file refs, display titles, score formatting labels. May contain 0x80+ control bytes.");
            DrawStringArray(final.HallOfFameStrings, "HofStr", final.HallOfFameStringSizes);
        }

        if (ImGui.CollapsingHeader("Clue Relationship Phrases"))
        {
            ImGui.TextWrapped("40 clue relationship phrases (shared across TAC/GAME/BUG EXEs).");
            DrawStringArray(final.ClueRelationshipPhrases, "CluePhrase", final.CluePhraseSizes);
        }

        if (ImGui.CollapsingHeader("Month Abbreviations"))
        {
            DrawStringArray(final.MonthAbbreviations, "Month", final.MonthSizes);
        }

        if (ImGui.CollapsingHeader("Intel Headers"))
        {
            DrawStringArray(final.IntelHeaders, "IntelHdr", final.IntelHeaderSizes);
        }

        if (ImGui.CollapsingHeader("Clue Category Data"))
        {
            ImGui.TextWrapped("48-byte clue category and popcount lookup table (identical across FINAL/TAC/GAME). Bytes 0-15: category bit flags. Bytes 16-47: four 8-entry popcount lookup sub-tables with offsets +0, +1, +1, +2.");
            for (var i = 0; i < final.ClueCategoryData.Length; i++)
            {
                if (i > 0 && i % 8 != 0) ImGui.SameLine();
                if (i == 0) ImGui.Text("Bit flags:");
                if (i == 16) ImGui.Text("Popcount +0:");
                if (i == 24) ImGui.Text("Popcount +1:");
                if (i == 32) ImGui.Text("Popcount +1:");
                if (i == 40) ImGui.Text("Popcount +2:");
                ImGui.SetNextItemWidth(60.0f);
                var val = (int)final.ClueCategoryData[i];
                if (ImGui.InputInt($"[{i}]##ClueCat{i}", ref val))
                {
                    final.ClueCategoryData[i] = (byte)Math.Clamp(val, 0, 255);
                    _pendingState.RecordChange();
                }
            }
        }

        if (ImGui.CollapsingHeader("Intel Report Texts"))
        {
            ImGui.TextWrapped("Agent identification templates (shared with TAC).");
            DrawStringArray(final.IntelReportTexts, "IntelTxt", final.IntelReportTextSizes);
        }

        if (ImGui.CollapsingHeader("Rank Names"))
        {
            DrawStringArray(final.RankNames, "Rank", final.RankNameSizes);
        }

        if (ImGui.CollapsingHeader("Evidence Type Abbreviations"))
        {
            DrawStringArray(final.EvidenceTypeAbbreviations, "EvType", final.EvidenceTypeSizes);
        }

        if (ImGui.CollapsingHeader("Evidence Item Names"))
        {
            ImGui.TextWrapped("Vehicles(8), weapons(8), streets(8), airlines(8), telecom(8), money(16), passports(8).");
            DrawStringArray(final.EvidenceItemNames, "EvItem", final.EvidenceItemSizes);
        }

        if (ImGui.CollapsingHeader("Investigation Methods"))
        {
            DrawStringArray(final.InvestigationMethods, "InvMethod", final.InvestigationMethodSizes);
        }

        if (ImGui.CollapsingHeader("Clue Not Found Message"))
        {
            ImGui.TextWrapped("Displayed when a text file lookup fails to find the requested header.");
            var contentSize = ImGui.GetContentRegionAvail();
            var msg = final.ClueNotFoundMessage;
            var newVal = ImGuiExtensions.Input("##ClueNotFound", msg, 256, width: (int)contentSize.X - 80);
            if (newVal != null)
            {
                final.ClueNotFoundMessage = newVal;
                _pendingState.RecordChange();
            }
        }

        if (ImGui.CollapsingHeader("Status Labels"))
        {
            ImGui.TextWrapped("Master Plan, status indicators, UI labels used by personnel/intel screens.");
            DrawStringArray(final.GameStateStatusLabels, "StatusLabel");
        }

        if (ImGui.CollapsingHeader("Chronology Strings"))
        {
            DrawStringArray(final.GameStateChronologyStrings, "Chronology");
        }

        if (ImGui.CollapsingHeader("Loading Message"))
        {
            var contentSize = ImGui.GetContentRegionAvail();
            var msg = final.GameStateLoadingMessage;
            var newVal = ImGuiExtensions.Input("##LoadingMsg", msg, 256, width: (int)contentSize.X - 80);
            if (newVal != null) { final.GameStateLoadingMessage = newVal; _pendingState.RecordChange(); }
        }

        if (ImGui.CollapsingHeader("Quit Menu"))
        {
            ImGui.TextWrapped("Menu format: \\n = line separator, leading space = selectable item.");
            var contentSize = ImGui.GetContentRegionAvail();
            var val = final.GameStateQuitMenu;
            var origVal = val;
            ImGui.InputTextMultiline("##QuitMenu", ref val, 512,
                new System.Numerics.Vector2(contentSize.X - 80, 80.0f));
            if (val != origVal) { final.GameStateQuitMenu = val; _pendingState.RecordChange(); }
        }

        if (ImGui.CollapsingHeader("Scene Init Palettes"))
        {
            ImGui.TextWrapped("Scene initialisation: display flags and two palette remap tables.");
            if (final.GameStateSceneInitFlags.Length >= 4)
            {
                var width = BitConverter.ToUInt16(final.GameStateSceneInitFlags, 0);
                var flag = BitConverter.ToUInt16(final.GameStateSceneInitFlags, 2);
                var w = ImGuiExtensions.Input("Width", (int)width, width: 120);
                if (w != null) { BitConverter.GetBytes((ushort)w.Value).CopyTo(final.GameStateSceneInitFlags, 0); _pendingState.RecordChange(); }
                ImGui.SameLine();
                var fl = ImGuiExtensions.Input("Flag", (int)flag, width: 120);
                if (fl != null) { BitConverter.GetBytes((ushort)fl.Value).CopyTo(final.GameStateSceneInitFlags, 2); _pendingState.RecordChange(); }
            }
            ImGui.Text("Palette Remap 1:");
            DrawByteArrayEditable(final.GameStatePaletteRemap1, "PalRemap1");
            ImGui.Text("Palette Remap 2:");
            DrawByteArrayEditable(final.GameStatePaletteRemap2, "PalRemap2");
        }

        if (ImGui.CollapsingHeader("OK String"))
        {
            var contentSize = ImGui.GetContentRegionAvail();
            var val = final.GameStateOkString;
            var newVal = ImGuiExtensions.Input("##OkStr", val, 256, width: (int)contentSize.X - 80);
            if (newVal != null) { final.GameStateOkString = newVal; _pendingState.RecordChange(); }
        }

        if (ImGui.CollapsingHeader("Save/Load UI Strings"))
        {
            ImGui.TextWrapped("Save/load prompts, rank templates, difficulty suffixes, error messages, disk swap prompts.");
            DrawStringArray(final.GameStateSaveLoadStrings, "SaveLoad");
        }

        if (ImGui.CollapsingHeader("RastPort Blocks (Game State)"))
        {
            ImGui.TextWrapped("6 display configuration blocks. Each: DataOffset, Page, OriginX/Y, ExtentX/Y, Flag, MaxColor, BPP, Reserved.");
            DrawRastPortBlocks(final.GameStateRastPortData, "GsRastPort");
        }
    }

    private void DrawMissionSetCrimeSlot(string label, FinalMissionSetRecord ms, int slotIndex, string[] crimeTypeNames)
    {
        var crimeId = slotIndex switch
        {
            0 => ms.Crime1Id,
            1 => ms.Crime2Id,
            2 => ms.Crime3Id,
            3 => ms.Crime4Id,
            4 => ms.Crime5Id,
            5 => ms.Crime6Id,
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
            case 3: ms.Crime4Id = value; break;
            case 4: ms.Crime5Id = value; break;
            case 5: ms.Crime6Id = value; break;
        }
    }

    #endregion

    #region GAME

    private void DrawGameData(GameDataSegment game)
    {
        if (ImGui.CollapsingHeader("Initial Game Strings"))
        {
            DrawStringArray(game.InitialGameStrings, "InitStr", game.InitialGameStringSizes);
        }

        if (ImGui.CollapsingHeader("CGA Animation Data"))
        {
            ImGui.TextWrapped($"2bpp CGA sprite data + CGA-to-VGA palette ({game.CgaAnimationData.Length} bytes).");
            if (game.CgaAnimationData.Length > 0)
            {
                var hexLines = new System.Text.StringBuilder();
                for (var i = 0; i < game.CgaAnimationData.Length; i += 16)
                {
                    var lineLen = Math.Min(16, game.CgaAnimationData.Length - i);
                    hexLines.Append($"{i:X4}: ");
                    for (var j = 0; j < lineLen; j++)
                        hexLines.Append($"{game.CgaAnimationData[i + j]:X2} ");
                    for (var j = lineLen; j < 16; j++)
                        hexLines.Append("   ");
                    hexLines.Append(" ");
                    for (var j = 0; j < lineLen; j++)
                    {
                        var b = game.CgaAnimationData[i + j];
                        hexLines.Append(b >= 0x20 && b <= 0x7E ? (char)b : '.');
                    }
                    hexLines.AppendLine();
                }
                var hexText = hexLines.ToString();
                ImGui.InputTextMultiline("##GameCgaHex", ref hexText, (uint)hexText.Length + 1,
                    new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X - 20, 200.0f),
                    ImGuiInputTextFlags.ReadOnly);
            }
        }

        if (ImGui.CollapsingHeader("HQ Display Strings"))
        {
            DrawStringArray(game.HqDisplayStrings, "HqStr", game.HqDisplayStringSizes);
        }

        if (ImGui.CollapsingHeader("Character Names"))
        {
            DrawStringArray(game.CharacterNames, "CharName");
        }

        if (ImGui.CollapsingHeader("Game Status Labels"))
        {
            DrawStringArray(game.GameStatusLabels, "StatusLbl", game.GameStatusLabelSizes);
        }

        if (ImGui.CollapsingHeader("Screen Layout Data"))
        {
            ImGui.TextWrapped($"Draw commands for the city/HQ screen UI ({game.ScreenLayoutData.Length} bytes). Flag: 0=line, 1=filled rect, 2=special, 0xFFFF=terminator.");
            if (game.ScreenLayoutData.Length >= 12 && ImGui.BeginTable("ScreenLayout", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new System.Numerics.Vector2(0, 300)))
            {
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 30);
                ImGui.TableSetupColumn("Flag", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("X1", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Y1", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("X2", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Y2", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Color", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableHeadersRow();

                var recordCount = game.ScreenLayoutData.Length / 12;
                for (var i = 0; i < recordCount; i++)
                {
                    ImGui.PushID($"SL_{i}");
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text($"{i}");

                    for (var f = 0; f < 6; f++)
                    {
                        ImGui.TableNextColumn();
                        var off = i * 12 + f * 2;
                        var val = (int)BitConverter.ToUInt16(game.ScreenLayoutData, off);
                        var newVal = ImGuiExtensions.Input($"##{f}", val, width: 75);
                        if (newVal != null)
                        {
                            BitConverter.GetBytes((ushort)newVal.Value).CopyTo(game.ScreenLayoutData, off);
                            _pendingState.RecordChange();
                        }
                    }
                    ImGui.PopID();
                }
                ImGui.EndTable();
            }
        }

        if (ImGui.CollapsingHeader("Gameplay Event Strings"))
        {
            DrawStringArray(game.GameplayEventStrings, "GE1", game.GameplayEventStringSizes);
        }

        if (ImGui.CollapsingHeader("Gameplay Binary Lookup"))
        {
            ImGui.TextWrapped($"{game.GameplayBinaryLookup.Length} bytes — between travel menu and guard alertness strings.");
            DrawByteArrayEditable(game.GameplayBinaryLookup, "GameBinLookup");
        }

        if (ImGui.CollapsingHeader("Gameplay Event Strings (Part 2)"))
        {
            DrawStringArray(game.GameplayEventStrings2, "GE2", game.GameplayEventString2Sizes);
        }

        if (ImGui.CollapsingHeader("Clue Relationship Phrases"))
        {
            DrawStringArray(game.ClueRelationshipPhrases, "CluePhr");
        }

        if (ImGui.CollapsingHeader("Month Names"))
        {
            DrawStringArray(game.MonthNames, "Month");
        }

        if (ImGui.CollapsingHeader("Intel Headers"))
        {
            DrawStringArray(game.IntelHeaders, "IntelHdr", game.IntelHeaderSizes);
        }

        if (ImGui.CollapsingHeader("Clue Category Data"))
        {
            ImGui.TextWrapped("48-byte clue category and popcount lookup table (identical across FINAL/TAC/GAME). Bytes 0-15: category bit flags. Bytes 16-47: four 8-entry popcount lookup sub-tables with offsets +0, +1, +1, +2.");
            for (var i = 0; i < game.ClueCategoryData.Length; i++)
            {
                if (i > 0 && i % 8 != 0) ImGui.SameLine();
                if (i == 0) ImGui.Text("Bit flags:");
                if (i == 16) ImGui.Text("Popcount +0:");
                if (i == 24) ImGui.Text("Popcount +1:");
                if (i == 32) ImGui.Text("Popcount +1:");
                if (i == 40) ImGui.Text("Popcount +2:");
                ImGui.SetNextItemWidth(60.0f);
                var val = (int)game.ClueCategoryData[i];
                if (ImGui.InputInt($"[{i}]##GameClueCat{i}", ref val))
                {
                    game.ClueCategoryData[i] = (byte)Math.Clamp(val, 0, 255);
                    _pendingState.RecordChange();
                }
            }
        }

        if (ImGui.CollapsingHeader("Intel Report Texts"))
        {
            DrawStringArray(game.IntelReportTexts, "IntelTxt", game.IntelReportTextSizes);
        }

        if (ImGui.CollapsingHeader("Rank Names"))
        {
            DrawStringArray(game.RankNames, "Rank", game.RankNameSizes);
        }

        if (ImGui.CollapsingHeader("Evidence Type Abbreviations"))
        {
            DrawStringArray(game.EvidenceTypeAbbreviations, "EvType", game.EvidenceTypeSizes);
        }

        if (ImGui.CollapsingHeader("Evidence Item Names"))
        {
            DrawStringArray(game.EvidenceItemNames, "EvItem", game.EvidenceItemSizes);
        }

        if (ImGui.CollapsingHeader("Investigation Methods"))
        {
            DrawStringArray(game.InvestigationMethods, "InvMeth", game.InvestigationMethodSizes);
        }

        if (ImGui.CollapsingHeader("RastPort Blocks (Pre-String)"))
        {
            DrawRastPortBlocks(game.PreStringTableRastPortData, "GamePreRP");
        }

        DrawRawSectionSizes("Raw Sections", new[]
        {
            ("PostCharNameData", game.PostCharNameData.Length),
            ("RemainingTrailingData", game.RemainingTrailingData.Length)
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

    private void DrawRastPortBlocks(byte[] data, string idPrefix)
    {
        // RastPort blocks are 20 bytes each, optionally followed by a 2-byte config pointer.
        // Scan for the signature: OriginX=0, OriginY=0, ExtentX=319 (0x013F), ExtentY=199 (0x00C7)
        // at offset +4 within each block. The block starts 4 bytes before the signature.
        var blockStarts = new List<int>();
        var sig = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x3F, 0x01, 0xC7, 0x00 };
        for (var i = 0; i <= data.Length - 8; i++)
        {
            var match = true;
            for (var j = 0; j < sig.Length; j++)
            {
                if (data[i + j] != sig[j]) { match = false; break; }
            }
            if (match && i >= 4) blockStarts.Add(i - 4);
        }

        for (var bi = 0; bi < blockStarts.Count; bi++)
        {
            var off = blockStarts[bi];
            if (off + 20 > data.Length) continue;
            ImGui.PushID($"{idPrefix}_{bi}");

            var dataOffset = BitConverter.ToUInt16(data, off);
            var page = BitConverter.ToUInt16(data, off + 2);
            var flag = BitConverter.ToUInt16(data, off + 12);
            var maxColor = BitConverter.ToUInt16(data, off + 14);
            var bpp = BitConverter.ToUInt16(data, off + 16);
            var reserved = BitConverter.ToUInt16(data, off + 18);

            var doLabel = dataOffset == 0xFFFF ? "uninit" : dataOffset == 0 ? "none" : $"0x{dataOffset:X4}";
            if (ImGui.CollapsingHeader($"Block {bi}: Page={page}, Flag={flag}, DO={doLabel}"))
            {
                if (ImGui.BeginTable($"fields", 4))
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    // TODO: replace with a dropdown of what to point to
                    ImGui.TextDisabled($"DataOffset: {doLabel}");
                    ImGui.TableNextColumn();
                    var pv = ImGuiExtensions.Input("Page", (int)page, width: 80);
                    if (pv != null) { BitConverter.GetBytes((ushort)pv.Value).CopyTo(data, off + 2); _pendingState.RecordChange(); }
                    ImGui.TableNextColumn();
                    var fv = ImGuiExtensions.Input("Flag", (int)flag, width: 80);
                    if (fv != null) { BitConverter.GetBytes((ushort)fv.Value).CopyTo(data, off + 12); _pendingState.RecordChange(); }
                    ImGui.TableNextColumn();
                    var mv = ImGuiExtensions.Input("MaxColor", (int)maxColor, width: 80);
                    if (mv != null) { BitConverter.GetBytes((ushort)mv.Value).CopyTo(data, off + 14); _pendingState.RecordChange(); }
                    ImGui.EndTable();
                }
                if (ImGui.BeginTable($"fields2", 4))
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    var bv = ImGuiExtensions.Input("BPP", (int)bpp, width: 80);
                    if (bv != null) { BitConverter.GetBytes((ushort)bv.Value).CopyTo(data, off + 16); _pendingState.RecordChange(); }
                    ImGui.TableNextColumn();
                    var rv = ImGuiExtensions.Input("Reserved", (int)reserved, width: 80);
                    if (rv != null) { BitConverter.GetBytes((ushort)rv.Value).CopyTo(data, off + 18); _pendingState.RecordChange(); }
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                    ImGui.EndTable();
                }
            }
            ImGui.PopID();
        }

    }

    private void DrawByteArrayEditable(byte[] data, string idPrefix)
    {
        if (data.Length == 0) return;
        var columns = Math.Min(16, data.Length);
        if (ImGui.BeginTable($"{idPrefix}_table", columns, ImGuiTableFlags.Borders))
        {
            for (var i = 0; i < columns; i++)
                ImGui.TableSetupColumn($"{i}", ImGuiTableColumnFlags.WidthFixed, 30);
            ImGui.TableHeadersRow();

            ImGui.TableNextRow();
            for (var i = 0; i < data.Length; i++)
            {
                if (i > 0 && i % columns == 0) ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.PushID($"{idPrefix}_{i}");
                var val = (int)data[i];
                ImGui.SetNextItemWidth(30);
                if (ImGui.InputInt("", ref val, 0, 0) && val >= 0 && val <= 255)
                {
                    data[i] = (byte)val;
                    _pendingState.RecordChange();
                }
                ImGui.PopID();
            }
            ImGui.EndTable();
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
