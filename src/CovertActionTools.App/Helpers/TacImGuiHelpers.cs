using System.Collections.Generic;
using System.Linq;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models.Executables.Records.Shared;
using CovertActionTools.Core.Models.Executables.Records.Tac;
using CovertActionTools.Core.Models.Executables.Sections;
using CovertActionTools.Core.Models.Executables.Sections.Shared;
using CovertActionTools.Core.Models.Executables.Sections.Tac;
using ImGuiNET;

namespace CovertActionTools.App.Helpers
{
    internal static class TacImGuiHelpers
    {
        #region VGA palette combo data

        private static readonly List<int> VgaPaletteValues =
            System.Enum.GetValues<VgaPaletteIndex>().Select(c => (int)c).ToList();

        private static readonly List<string> VgaPaletteLabels =
            System.Enum.GetValues<VgaPaletteIndex>()
                .Select(c => $"{(int)c}: {c}")
                .ToList();

        #endregion

        #region Direction labels

        private static readonly string[] CompassLabels9 =
            { "Stationary", "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        private static readonly string[] CardinalLabels4 =
            { "N", "E", "S", "W" };

        #endregion

        #region Size constraint combo data

        private static readonly List<int> SizeConstraintValues =
            new List<int> { 1, 2, 3, 4, 5, 6, 7 };

        private static readonly List<string> SizeConstraintLabels =
            new List<string> { "Small", "Medium", "Small+Medium", "Large", "Small+Large", "Medium+Large", "All" };

        #endregion

        #region Section helpers

        public static void DrawTacHeaderFilenamesSection(TacHeaderFilenamesSection section)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("Header Filenames")) return;

            for (var i = 0; i < section.Filenames.Count; i++)
            {
                ImGui.Text($"{i}: {section.Filenames[i]}");
            }
        }

        public static void DrawRoomTypeSection(RoomTypeSection section, PendingEditorExecutableState pending)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("Room Types")) return;

            var editable = section.Editable();
            if (!editable) ImGui.BeginDisabled();

            if (ImGui.BeginTable("RoomTypes", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Surv. Quality");
                ImGui.TableSetupColumn("Size Constraint");
                ImGui.TableSetupColumn("Enabled");
                ImGui.TableHeadersRow();

                for (var i = 0; i < section.RoomTypes.Count; i++)
                {
                    DrawRoomTypeRow(i, section.RoomTypes[i], pending);
                }

                ImGui.EndTable();
            }

            if (!editable) ImGui.EndDisabled();
        }

        public static void DrawMapObjectTypeSection(MapObjectTypeSection section, PendingEditorExecutableState pending, IReadOnlyList<string> roomTypeNames)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("Objects")) return;

            var editable = section.Editable();

            for (var i = 0; i < section.MapObjectTypes.Count; i++)
            {
                ImGui.PushID($"Object_{i}");
                var obj = section.MapObjectTypes[i];
                var label = string.IsNullOrEmpty(obj.Name) ? $"Object {i}" : $"Object {i}: {obj.Name}";
                if (ImGui.CollapsingHeader(label))
                {
                    ImGui.Indent();
                    DrawMapObjectTypeRecord(i, obj, editable, pending, roomTypeNames);
                    ImGui.Unindent();
                }
                ImGui.PopID();
            }
        }

        public static void DrawMovementSection(MovementSection section, PendingEditorExecutableState pending)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("Movement")) return;

            var editable = section.Editable();
            DrawFullDirectionRecord("Walking Pixels", section.Movement, editable, pending);
            DrawFullDirectionRecord("Jumping Tiles", section.Jumping, editable, pending);
            DrawCardinalDirectionRecord("Tile Adjacency", section.TileAdjacency, editable, pending);
        }

        public static void DrawCgaColorRemapSection(CgaColorRemapSection section, PendingEditorExecutableState pending)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("CGA Color Remap")) return;

            ImGui.TextWrapped("5 directly-referenced CGA dither-remap records from the 14-record table. " +
                              "Each VGA palette index maps to a (low, high) CGA color pair (0-3) that " +
                              "alternate on adjacent pixels to approximate the VGA color.");

            var editable = section.Editable();
            if (!editable) ImGui.BeginDisabled();

            DrawCgaRemapRecord("Menu Highlight", section.MenuHighlight, pending);
            DrawCgaRemapRecord("Menu Cursor", section.MenuCursor, pending);
            DrawCgaRemapRecord("Entity Card", section.EntityCard, pending);
            DrawCgaRemapRecord("Dialog Box", section.DialogBox, pending);
            DrawCgaRemapRecord("Mission Setup", section.MissionSetup, pending);

            if (!editable) ImGui.EndDisabled();
        }

        private static void DrawCgaRemapRecord(string label, CgaColorRemapRecord record, PendingEditorExecutableState pending)
        {
            if (!ImGui.TreeNode(label)) return;

            if (ImGui.BeginTable($"CgaRemap_{label}", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Index");
                ImGui.TableSetupColumn("Color");
                ImGui.TableSetupColumn("Low");
                ImGui.TableSetupColumn("High");
                ImGui.TableHeadersRow();

                for (byte i = 0; i < CgaColorRemapRecord.EntryCount; i++)
                {
                    var pair = record.ColorMap[i];
                    ImGui.PushID($"Cga_{label}_{i}");
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text($"{i}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{(VgaPaletteIndex)i}");

                    ImGui.TableNextColumn();
                    var newLow = ImGuiExtensions.Input("##lo", pair.Low, width: 80);
                    if (newLow.HasValue && newLow.Value >= 0 && newLow.Value <= 3)
                    {
                        pair.Low = (byte)newLow.Value;
                        pending.RecordChange();
                    }

                    ImGui.TableNextColumn();
                    var newHigh = ImGuiExtensions.Input("##hi", pair.High, width: 80);
                    if (newHigh.HasValue && newHigh.Value >= 0 && newHigh.Value <= 3)
                    {
                        pair.High = (byte)newHigh.Value;
                        pending.RecordChange();
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            ImGui.TreePop();
        }

        public static void DrawDoorEntryStringsSection(DoorEntryStringsSection section, PendingEditorExecutableState pending)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("Door Prompt Strings")) return;

            var editable = section.Editable();
            if (!editable) ImGui.BeginDisabled();

            var multilineSize = new System.Numerics.Vector2(220, 48);

            var newHeader = ImGuiExtensions.InputMultiline(
                "Prompt Header", section.DoorPromptHeader, 64, multilineSize);
            if (newHeader != null) { section.DoorPromptHeader = newHeader; pending.RecordChange(); }

            var newLabel = ImGuiExtensions.InputMultiline(
                "Door Label", section.DoorLabel, 64, multilineSize);
            if (newLabel != null) { section.DoorLabel = newLabel; pending.RecordChange(); }

            var newSep = ImGuiExtensions.InputMultiline(
                "Door Separator", section.DoorSeparator, 64, multilineSize);
            if (newSep != null) { section.DoorSeparator = newSep; pending.RecordChange(); }

            if (!editable) ImGui.EndDisabled();
        }

        public static void DrawVgaPaletteRemapSection(VgaPaletteRemapSection section, PendingEditorExecutableState pending)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("VGA Palette Remap")) return;

            var editable = section.Editable();
            if (!editable) ImGui.BeginDisabled();

            var colCount = section.Records.Count + 2;
            if (ImGui.BeginTable("VgaPalette", colCount, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Index");
                ImGui.TableSetupColumn("Color");
                for (var r = 0; r < section.Records.Count; r++)
                    ImGui.TableSetupColumn($"Record {r}");
                ImGui.TableHeadersRow();

                for (var b = 0; b < VgaPaletteRemapRecord.PaletteLength; b++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text($"{b}");
                    ImGui.TableNextColumn();
                    ImGui.Text($"{(VgaPaletteIndex)b}");

                    for (var r = 0; r < section.Records.Count; r++)
                    {
                        ImGui.TableNextColumn();
                        ImGui.PushID($"Vga_{r}_{b}");
                        var val = (int)section.Records[r].Palette[(byte)b];
                        var newVal = ImGuiExtensions.Input("##v", val, width: 80);
                        if (newVal.HasValue && newVal.Value >= 0 && newVal.Value <= 15)
                        {
                            section.Records[r].Palette[(byte)b] = (byte)newVal.Value;
                            pending.RecordChange();
                        }
                        ImGui.PopID();
                    }
                }

                ImGui.EndTable();
            }

            if (!editable) ImGui.EndDisabled();
        }

        public static void DrawTacMenuStringsSection(TacMenuStringsSection section, PendingEditorExecutableState pending)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("In-Game Menu Strings")) return;

            var editable = section.Editable();
            if (!editable) ImGui.BeginDisabled();

            var newBanner = ImGuiExtensions.Input(
                "Pause Banner", section.PauseBanner, 64, width: 220);
            if (newBanner != null) { section.PauseBanner = newBanner; pending.RecordChange(); }

            var editorSize = new System.Numerics.Vector2(300, 48);
            ImGui.Text("Quit Dialog");
            ImGuiExtensions.DrawMenuStringRecord("QuitDialog", section.QuitDialog, editorSize, () => pending.RecordChange());

            if (!editable) ImGui.EndDisabled();
        }

        private static readonly List<int> ScanCodeValues =
            System.Enum.GetValues<BiosKeyboardScanCode>().Select(c => (int)c).ToList();

        private static readonly List<string> ScanCodeLabels =
            System.Enum.GetValues<BiosKeyboardScanCode>()
                .Select(c => c.ToString())
                .ToList();

        public static void DrawTacInputConfigSection(TacInputConfigSection section, PendingEditorExecutableState pending)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("Direction Scan Codes")) return;

            var editable = section.Editable();
            if (!editable) ImGui.BeginDisabled();

            DrawScanCodeCombo("North", section.NorthKey, v => section.NorthKey = v, pending);
            DrawScanCodeCombo("North-East", section.NorthEastKey, v => section.NorthEastKey = v, pending);
            DrawScanCodeCombo("East", section.EastKey, v => section.EastKey = v, pending);
            DrawScanCodeCombo("South-East", section.SouthEastKey, v => section.SouthEastKey = v, pending);
            DrawScanCodeCombo("South", section.SouthKey, v => section.SouthKey = v, pending);
            DrawScanCodeCombo("South-West", section.SouthWestKey, v => section.SouthWestKey = v, pending);
            DrawScanCodeCombo("West", section.WestKey, v => section.WestKey = v, pending);
            DrawScanCodeCombo("North-West", section.NorthWestKey, v => section.NorthWestKey = v, pending);

            if (!editable) ImGui.EndDisabled();
        }

        private static void DrawScanCodeCombo(
            string label, BiosKeyboardScanCode current,
            System.Action<BiosKeyboardScanCode> setter, PendingEditorExecutableState pending)
        {
            var result = ImGuiExtensions.Input(label, (int)current, ScanCodeValues, ScanCodeLabels, width: 200);
            if (result != null) { setter((BiosKeyboardScanCode)result.Value); pending.RecordChange(); }
        }

        public static void DrawTargetReticleColorsSection(TargetReticleColorsSection section, PendingEditorExecutableState pending)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("Target Reticle Colors")) return;

            var editable = section.Editable();
            if (!editable) ImGui.BeginDisabled();

            DrawReticleColorField("Stage 0", () => section.Stage0, v => { section.Stage0 = v; pending.RecordChange(); });
            DrawReticleColorField("Stage 1", () => section.Stage1, v => { section.Stage1 = v; pending.RecordChange(); });
            DrawReticleColorField("Stage 2", () => section.Stage2, v => { section.Stage2 = v; pending.RecordChange(); });
            DrawReticleColorField("Stage 3", () => section.Stage3, v => { section.Stage3 = v; pending.RecordChange(); });
            DrawReticleColorField("Stage 4", () => section.Stage4, v => { section.Stage4 = v; pending.RecordChange(); });

            if (!editable) ImGui.EndDisabled();
        }

        private static void DrawReticleColorField(string label, System.Func<byte> getter, System.Action<byte> setter)
        {
            var result = ImGuiExtensions.Input(label, (int)getter(), VgaPaletteValues, VgaPaletteLabels, width: 200);
            if (result != null) { setter((byte)result.Value); }
        }

        public static void DrawGameplayActionMenusSection(GameplayActionMenusSection section, PendingEditorExecutableState pending)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("Action Menus")) return;

            var editable = section.Editable();
            if (!editable) ImGui.BeginDisabled();

            var editorSize = new System.Numerics.Vector2(300, 48);

            if (ImGui.TreeNode("Set Trap Menu"))
            {
                ImGuiExtensions.DrawMenuStringRecord("SetTrap", section.SetTrapMenuStrings, editorSize, () => pending.RecordChange());
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Arrest Menu"))
            {
                var newArrestHeader = ImGuiExtensions.InputMultiline(
                    "Arrest Header", section.ArrestHeaderString, 128, editorSize, id: "arrest_hdr");
                if (newArrestHeader != null) { section.ArrestHeaderString = newArrestHeader; pending.RecordChange(); }

                ImGuiExtensions.DrawMenuStringRecord("Arrest", section.ArrestMenuStrings, editorSize, () => pending.RecordChange());
                ImGui.TreePop();
            }

            if (!editable) ImGui.EndDisabled();
        }

        private static void DrawFixedSizeStringTableSection(
            string headerLabel, string idPrefix, ExactCountFixedSizeStringTableSection section, PendingEditorExecutableState pending)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader(headerLabel)) return;

            var editable = section.Editable();
            if (!editable) ImGui.BeginDisabled();

            var editorSize = new System.Numerics.Vector2(300, 48);
            for (var i = 0; i < section.Strings.Count; i++)
            {
                var newVal = ImGuiExtensions.InputMultiline(
                    $"[{i}]", section.Strings[i], 256, editorSize, id: $"{idPrefix}_{i}");
                if (newVal != null) { section.Strings[i] = newVal; pending.RecordChange(); }
            }

            if (!editable) ImGui.EndDisabled();
        }

        public static void DrawStatusLineActionStringsSection(StatusLineActionStringsSection section, PendingEditorExecutableState pending)
        {
            DrawFixedSizeStringTableSection("Status Line Action Strings", "slact", section, pending);
        }

        public static void DrawStatusLineStatusStringsSection(StatusLineStatusStringsSection section, PendingEditorExecutableState pending)
        {
            DrawFixedSizeStringTableSection("Status Line Status Strings", "slstat", section, pending);
        }

        public static void DrawGameplayEndingStringsSection(GameplayEndingStringsSection section, PendingEditorExecutableState pending)
        {
            DrawFixedSizeStringTableSection("Gameplay Ending Strings", "gpend", section, pending);
        }

        public static void DrawGameplayPopupStringsSection(GameplayPopupStringsSection section, PendingEditorExecutableState pending)
        {
            DrawFixedSizeStringTableSection("Gameplay Popup Strings", "gppop", section, pending);
        }

        public static void DrawFloorSafeInventoryItemRewardSection(
            FloorSafeInventoryItemRewardSection section, PendingEditorExecutableState pending,
            string[] equipmentNames)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("Floor Safe Inventory Item Rewards")) return;

            var editable = section.Editable();
            if (!editable) ImGui.BeginDisabled();

            var itemValues = new List<int>();
            var itemLabels = new List<string>();
            for (var i = 0; i < equipmentNames.Length; i++)
            {
                itemValues.Add(i);
                itemLabels.Add(string.IsNullOrEmpty(equipmentNames[i])
                    ? $"{i}"
                    : $"{i}: {equipmentNames[i]}");
            }

            if (ImGui.BeginTable("FloorSafeRewards", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Slot");
                ImGui.TableSetupColumn("Inventory Item");
                ImGui.TableHeadersRow();

                for (var i = 0; i < section.Records.Count; i++)
                {
                    ImGui.PushID($"FloorSafe_{i}");
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text($"{i}");
                    ImGui.TableNextColumn();
                    var result = ImGuiExtensions.Input("##item", section.Records[i].InventoryItemIndex,
                        itemValues, itemLabels, width: 250);
                    if (result != null)
                    {
                        section.Records[i].InventoryItemIndex = (ushort)result.Value;
                        pending.RecordChange();
                    }
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            if (!editable) ImGui.EndDisabled();
        }

        public static void DrawRenderingSection(RenderingSection section, PendingEditorExecutableState pending)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("Rendering")) return;

            var editable = section.Editable();

            DrawBlobRecord("Padding 1", section.Padding1, editable, pending);
            DrawBlobRecord("Padding 2", section.Padding2, editable, pending);

            ImGui.Text($"Pointer 1: 0x{section.Pointer1.Pointer:X4}  " +
                       $"Pointer 2: 0x{section.Pointer2.Pointer:X4}  " +
                       $"Pointer 3: 0x{section.Pointer3.Pointer:X4}  " +
                       $"Pointer 4: 0x{section.Pointer4.Pointer:X4}  " +
                       $"Pointer 5: 0x{section.Pointer5.Pointer:X4}");

            // EnvironmentTransfer is intentionally not shown — always zeroed, not viewable or editable
            if (!editable) ImGui.BeginDisabled();
            if (ImGui.BeginTable("RastPorts", 9, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("Page");
                ImGui.TableSetupColumn("Origin X");
                ImGui.TableSetupColumn("Origin Y");
                ImGui.TableSetupColumn("Width - 1");
                ImGui.TableSetupColumn("Height - 1");
                ImGui.TableSetupColumn("Flag");
                ImGui.TableSetupColumn("Max Color");
                ImGui.TableSetupColumn("Bytes/px");
                ImGui.TableHeadersRow();

                DrawRastPortRow("RastPort 1", section.Record1, pending);
                DrawRastPortRow("RastPort 2", section.Record2, pending);
                DrawRastPortRow("RastPort 3", section.Record3, pending);
                DrawRastPortRow("RastPort 4", section.Record4, pending);
                DrawRastPortRow("RastPort 5", section.Record5, pending);

                ImGui.EndTable();
            }
            if (!editable) ImGui.EndDisabled();
        }

        #endregion

        #region Record helpers

        private static void DrawRoomTypeRow(int index, RoomTypeRecord record, PendingEditorExecutableState pending)
        {
            ImGui.PushID($"RoomType_{index}");
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var newName = ImGuiExtensions.Input("##Name", record.Name, 16 /*NameSize*/, width: 150);
            if (newName != null) { record.Name = newName; pending.RecordChange(); }

            ImGui.TableNextColumn();
            var newSurvQuality = ImGuiExtensions.Input("##SurvQuality", record.SurveillanceQuality, width: 80);
            if (newSurvQuality != null) { record.SurveillanceQuality = newSurvQuality.Value; pending.RecordChange(); }

            ImGui.TableNextColumn();
            var newConstraint = ImGuiExtensions.Input("##Size", (int)record.SizeConstraint, SizeConstraintValues, SizeConstraintLabels, width: 130);
            if (newConstraint != null) { record.SizeConstraint = (RoomSizeConstraint)newConstraint.Value; pending.RecordChange(); }

            ImGui.TableNextColumn();
            var enabled = record.Enabled;
            if (ImGui.Checkbox("##Enabled", ref enabled)) { record.Enabled = enabled; pending.RecordChange(); }

            ImGui.PopID();
        }

        private static void DrawMapObjectTypeRecord(int index, MapObjectTypeRecord record, bool editable, PendingEditorExecutableState pending, IReadOnlyList<string> roomTypeNames)
        {
            if (!editable) ImGui.BeginDisabled();

            // TODO: Sprite X/Y should come from/go to the sprite sheet on the GUYS2/GUYS3 images
            if (ImGui.BeginTable($"ObjBasic_{index}", 3))
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                var newName = ImGuiExtensions.Input("Name", record.Name, 12 /*NameSize*/, width: 120);
                if (newName != null) { record.Name = newName; pending.RecordChange(); }

                ImGui.TableNextColumn();
                var newX = ImGuiExtensions.Input("Sprite X", record.SpriteSheetOffsetX, width: 80);
                if (newX != null) { record.SpriteSheetOffsetX = newX.Value; pending.RecordChange(); }

                ImGui.TableNextColumn();
                var newY = ImGuiExtensions.Input("Sprite Y", record.SpriteSheetOffsetY, width: 80);
                if (newY != null) { record.SpriteSheetOffsetY = newY.Value; pending.RecordChange(); }

                ImGui.EndTable();
            }

            if (!editable) ImGui.EndDisabled();

            DrawBehaviourFlags(record, editable, pending);
            DrawRoomPlacement(record, editable, pending, roomTypeNames);
        }

        private static void DrawBehaviourFlags(MapObjectTypeRecord record, bool editable, PendingEditorExecutableState pending)
        {
            ImGui.Text("Behaviour Flags:");
            if (!editable) ImGui.BeginDisabled();

            var flags = (int)record.Behavior;
            var flagDefs = new (MapObjectBehavior flag, string label)[]
            {
                (MapObjectBehavior.BlocksMovement, "Blocks Movement"),
                (MapObjectBehavior.Openable, "Openable"),
                (MapObjectBehavior.Buggable, "Buggable"),
                (MapObjectBehavior.Photographable, "Photographable"),
                (MapObjectBehavior.IsDoor, "Is Door"),
                (MapObjectBehavior.BlocksLineOfSight, "Blocks LOS"),
                (MapObjectBehavior.MultiTileHorizontal, "Multi-Tile"),
                (MapObjectBehavior.Unused, "Unused"),
                (MapObjectBehavior.WallAdjacent, "Wall Adjacent"),
                (MapObjectBehavior.PasswordTerminal, "Password Terminal"),
            };

            if (ImGui.BeginTable("BehaviourFlags", 5))
            {
                for (var b = 0; b < flagDefs.Length; b++)
                {
                    if (b % 5 == 0) ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    var (flag, flagLabel) = flagDefs[b];
                    var val = (flags & (int)flag) != 0;
                    if (ImGui.Checkbox(flagLabel, ref val))
                    {
                        if (val) flags |= (int)flag;
                        else flags &= ~(int)flag;
                        record.Behavior = (MapObjectBehavior)flags;
                        pending.RecordChange();
                    }
                }

                ImGui.EndTable();
            }

            if (!editable) ImGui.EndDisabled();
        }

        private static void DrawRoomPlacement(MapObjectTypeRecord record, bool editable, PendingEditorExecutableState pending, IReadOnlyList<string> roomTypeNames)
        {
            ImGui.Text("Room Placement:");
            if (!editable) ImGui.BeginDisabled();

            var flags = (int)record.RoomSizeConstraint;
            var columns = Math.Min(roomTypeNames.Count, 6);
            if (ImGui.BeginTable("RoomPlace", columns))
            {
                for (var b = 0; b < roomTypeNames.Count; b++)
                {
                    if (b % columns == 0) ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    var val = (flags & (1 << b)) != 0;
                    if (ImGui.Checkbox(roomTypeNames[b], ref val))
                    {
                        if (val) flags |= (1 << b);
                        else flags &= ~(1 << b);
                        record.RoomSizeConstraint = (RoomSizeConstraint)flags;
                        pending.RecordChange();
                    }
                }

                ImGui.EndTable();
            }

            var usedBits = (1 << roomTypeNames.Count) - 1;
            var highBits = flags & ~usedBits;
            if (highBits != 0)
            {
                ImGui.Text($"  Unknown placement bits: 0x{highBits:X}");
            }

            if (!editable) ImGui.EndDisabled();
        }

        private static void DrawFullDirectionRecord(string subLabel, FullDirectionRecord record, bool editable, PendingEditorExecutableState pending)
        {
            if (!ImGui.TreeNode(subLabel)) return;
            if (!editable) ImGui.BeginDisabled();

            if (ImGui.BeginTable($"dir_{subLabel}", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Direction");
                ImGui.TableSetupColumn("DX");
                ImGui.TableSetupColumn("DY");
                ImGui.TableHeadersRow();

                DrawDirectionRow(CompassLabels9[0], 0, record.StationaryDx, record.StationaryDy,
                    (dx, dy) => { record.StationaryDx = dx; record.StationaryDy = dy; }, pending);
                DrawDirectionRow(CompassLabels9[1], 1, record.NorthDx, record.NorthDy,
                    (dx, dy) => { record.NorthDx = dx; record.NorthDy = dy; }, pending);
                DrawDirectionRow(CompassLabels9[2], 2, record.NorthEastDx, record.NorthEastDy,
                    (dx, dy) => { record.NorthEastDx = dx; record.NorthEastDy = dy; }, pending);
                DrawDirectionRow(CompassLabels9[3], 3, record.EastDx, record.EastDy,
                    (dx, dy) => { record.EastDx = dx; record.EastDy = dy; }, pending);
                DrawDirectionRow(CompassLabels9[4], 4, record.SouthEastDx, record.SouthEastDy,
                    (dx, dy) => { record.SouthEastDx = dx; record.SouthEastDy = dy; }, pending);
                DrawDirectionRow(CompassLabels9[5], 5, record.SouthDx, record.SouthDy,
                    (dx, dy) => { record.SouthDx = dx; record.SouthDy = dy; }, pending);
                DrawDirectionRow(CompassLabels9[6], 6, record.SouthWestDx, record.SouthWestDy,
                    (dx, dy) => { record.SouthWestDx = dx; record.SouthWestDy = dy; }, pending);
                DrawDirectionRow(CompassLabels9[7], 7, record.WestDx, record.WestDy,
                    (dx, dy) => { record.WestDx = dx; record.WestDy = dy; }, pending);
                DrawDirectionRow(CompassLabels9[8], 8, record.NorthWestDx, record.NorthWestDy,
                    (dx, dy) => { record.NorthWestDx = dx; record.NorthWestDy = dy; }, pending);

                ImGui.EndTable();
            }

            if (!editable) ImGui.EndDisabled();
            ImGui.TreePop();
        }

        private static void DrawCardinalDirectionRecord(string subLabel, CardinalDirectionRecord record, bool editable, PendingEditorExecutableState pending)
        {
            if (!ImGui.TreeNode(subLabel)) return;
            if (!editable) ImGui.BeginDisabled();

            if (ImGui.BeginTable($"dir_{subLabel}", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Direction");
                ImGui.TableSetupColumn("DX");
                ImGui.TableSetupColumn("DY");
                ImGui.TableHeadersRow();

                DrawDirectionRow(CardinalLabels4[0], 0, record.NorthDx, record.NorthDy,
                    (dx, dy) => { record.NorthDx = dx; record.NorthDy = dy; }, pending);
                DrawDirectionRow(CardinalLabels4[1], 1, record.EastDx, record.EastDy,
                    (dx, dy) => { record.EastDx = dx; record.EastDy = dy; }, pending);
                DrawDirectionRow(CardinalLabels4[2], 2, record.SouthDx, record.SouthDy,
                    (dx, dy) => { record.SouthDx = dx; record.SouthDy = dy; }, pending);
                DrawDirectionRow(CardinalLabels4[3], 3, record.WestDx, record.WestDy,
                    (dx, dy) => { record.WestDx = dx; record.WestDy = dy; }, pending);

                ImGui.EndTable();
            }

            if (!editable) ImGui.EndDisabled();
            ImGui.TreePop();
        }

        private static void DrawDirectionRow(string direction, int rowId, int dx, int dy, Action<int, int> set, PendingEditorExecutableState pending)
        {
            ImGui.PushID($"dir_{rowId}");
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(direction);
            ImGui.TableNextColumn();
            var newDx = ImGuiExtensions.Input("##dx", dx, width: 80);
            ImGui.TableNextColumn();
            var newDy = ImGuiExtensions.Input("##dy", dy, width: 80);
            if (newDx != null || newDy != null)
            {
                set(newDx ?? dx, newDy ?? dy);
                pending.RecordChange();
            }
            ImGui.PopID();
        }

        private static void DrawRastPortRow(string name, RastPortRecord record, PendingEditorExecutableState pending)
        {
            ImGui.PushID(name);
            ImGui.TableNextRow();
            ImGui.TableNextColumn(); ImGui.Text(name);
            ImGui.TableNextColumn();
            var newPage = ImGuiExtensions.Input("##Page", record.Page, width: 80);
            if (newPage != null) { record.Page = newPage.Value; pending.RecordChange(); }
            ImGui.TableNextColumn();
            var newOx = ImGuiExtensions.Input("##OriginX", record.OriginX, width: 80);
            if (newOx != null) { record.OriginX = newOx.Value; pending.RecordChange(); }
            ImGui.TableNextColumn();
            var newOy = ImGuiExtensions.Input("##OriginY", record.OriginY, width: 80);
            if (newOy != null) { record.OriginY = newOy.Value; pending.RecordChange(); }
            ImGui.TableNextColumn();
            var newW = ImGuiExtensions.Input("##Width", record.WidthMinusOne, width: 80);
            if (newW != null) { record.WidthMinusOne = newW.Value; pending.RecordChange(); }
            ImGui.TableNextColumn();
            var newH = ImGuiExtensions.Input("##Height", record.HeightMinusOne, width: 80);
            if (newH != null) { record.HeightMinusOne = newH.Value; pending.RecordChange(); }
            ImGui.TableNextColumn();
            var newFlag = ImGuiExtensions.Input("##Flag", record.Flag, width: 80);
            if (newFlag != null) { record.Flag = newFlag.Value; pending.RecordChange(); }
            ImGui.TableNextColumn();
            var newMc = ImGuiExtensions.Input("##MaxColor", record.MaxColor, width: 80);
            if (newMc != null) { record.MaxColor = newMc.Value; pending.RecordChange(); }
            ImGui.TableNextColumn();
            var newBpp = ImGuiExtensions.Input("##Bpp", record.BytesPerPixel, width: 80);
            if (newBpp != null) { record.BytesPerPixel = newBpp.Value; pending.RecordChange(); }
            ImGui.PopID();
        }

        private static void DrawBlobRecord(string subLabel, BlobRecord record, bool editable, PendingEditorExecutableState pending)
        {
            if (!ImGui.TreeNode(subLabel)) return;
            if (!editable) ImGui.BeginDisabled();

            for (var i = 0; i < record.Data.Length; i++)
            {
                if (i > 0) ImGui.SameLine();
                ImGui.PushID($"blob_{i}");
                var val = (int)record.Data[i];
                var newVal = ImGuiExtensions.Input($"[{i}]", val, width: 70);
                if (newVal.HasValue && newVal.Value >= 0 && newVal.Value <= 255)
                {
                    record.Data[i] = (byte)newVal.Value;
                    pending.RecordChange();
                }
                ImGui.PopID();
            }

            if (!editable) ImGui.EndDisabled();
            ImGui.TreePop();
        }

        #endregion
    }
}
