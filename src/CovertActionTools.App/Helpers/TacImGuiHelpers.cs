using System.Collections.Generic;
using System.Linq;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models.Executables.Records.Shared;
using System;
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

        #region Size constraint combo data

        private static readonly List<int> SizeConstraintValues =
            new List<int> { 1, 2, 3, 4, 5, 6, 7 };

        private static readonly List<string> SizeConstraintLabels =
            new List<string> { "Small", "Medium", "Small+Medium", "Large", "Small+Large", "Medium+Large", "All" };

        #endregion

        #region Section helpers

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

        public static void DrawPasswordDialogTextsSection(PasswordDialogTextsSection section, PendingEditorExecutableState pending)
        {
            DrawFixedSizeStringTableSection("Password Dialog Texts", "pwdlg", section, pending);
        }

        public static void DrawInventoryItemNamesSection(InventoryItemNamesSection section, PendingEditorExecutableState pending)
        {
            DrawFixedSizeStringTableSection("Inventory Item Names", "invitem", section, pending);
        }

        public static void DrawInventoryItemRagdollCoordinatesSection(
            InventoryItemRagdollCoordinatesSection section, PendingEditorExecutableState pending)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("Inventory Item Ragdoll Coordinates")) return;

            var editable = section.Editable();
            if (!editable) ImGui.BeginDisabled();

            if (ImGui.BeginTable("InvRagdoll", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("#");
                ImGui.TableSetupColumn("X");
                ImGui.TableSetupColumn("Y");
                ImGui.TableHeadersRow();

                for (var i = 0; i < section.Coordinates.Length; i++)
                {
                    var coord = section.Coordinates[i];
                    ImGui.PushID($"InvRagdoll_{i}");
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text($"{i}");

                    ImGui.TableNextColumn();
                    var newX = ImGuiExtensions.Input("##X", (int)coord.X, width: 80);
                    if (newX != null) { coord.X = (ushort)newX.Value; pending.RecordChange(); }

                    ImGui.TableNextColumn();
                    var newY = ImGuiExtensions.Input("##Y", (int)coord.Y, width: 80);
                    if (newY != null) { coord.Y = (ushort)newY.Value; pending.RecordChange(); }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            if (!editable) ImGui.EndDisabled();
        }

        public static void DrawInventoryItemSelectionRectanglesSection(
            InventoryItemSelectionRectanglesSection section,
            PendingEditorExecutableState pending,
            IList<string> inventoryItemNames)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("Inventory Item Selection Rectangles")) return;

            var editable = section.Editable();
            if (!editable) ImGui.BeginDisabled();

            if (ImGui.BeginTable("InvSelRects", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Slot");
                ImGui.TableSetupColumn("X");
                ImGui.TableSetupColumn("Y");
                ImGui.TableSetupColumn("W");
                ImGui.TableSetupColumn("H");
                ImGui.TableSetupColumn("");
                ImGui.TableHeadersRow();

                for (var i = 0; i < section.Rectangles.Length; i++)
                {
                    var rect = section.Rectangles[i];
                    ImGui.PushID($"InvSelRect_{i}");
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    var label = i < inventoryItemNames.Count && !string.IsNullOrEmpty(inventoryItemNames[i])
                        ? $"{i}: {inventoryItemNames[i]}"
                        : $"{i}";
                    ImGui.Text(label);

                    ImGui.TableNextColumn();
                    var newX = ImGuiExtensions.Input("##X", (int)rect.X1, width: 80);
                    if (newX != null) { rect.X1 = (ushort)newX.Value; pending.RecordChange(); }

                    ImGui.TableNextColumn();
                    var newY = ImGuiExtensions.Input("##Y", (int)rect.Y1, width: 80);
                    if (newY != null) { rect.Y1 = (ushort)newY.Value; pending.RecordChange(); }

                    ImGui.TableNextColumn();
                    var w = rect.X2 - rect.X1;
                    var newW = ImGuiExtensions.Input("##W", w, width: 80);
                    if (newW != null) { rect.X2 = (ushort)(rect.X1 + newW.Value); pending.RecordChange(); }

                    ImGui.TableNextColumn();
                    var h = rect.Y2 - rect.Y1;
                    var newH = ImGuiExtensions.Input("##H", h, width: 80);
                    if (newH != null) { rect.Y2 = (ushort)(rect.Y1 + newH.Value); pending.RecordChange(); }

                    ImGui.TableNextColumn();

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            if (!editable) ImGui.EndDisabled();
        }

        public static void DrawInventoryItemSelectionNavigationSection(
            InventoryItemSelectionNavigationSection section,
            PendingEditorExecutableState pending,
            IList<string> inventoryItemNames)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("Inventory Item Selection Navigation")) return;

            var editable = section.Editable();
            if (!editable) ImGui.BeginDisabled();

            ImGui.Text("Target slot index reached when an arrow key is pressed from each inventory slot.");

            if (ImGui.BeginTable("InvItemNav", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Slot");
                foreach (var dir in Enum.GetValues<MenuNavigationDirection>())
                {
                    ImGui.TableSetupColumn(dir.ToString());
                }
                ImGui.TableHeadersRow();

                var rowKeys = section.Entries.Keys.OrderBy(k => k).ToList();
                foreach (var row in rowKeys)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    var label = row < inventoryItemNames.Count && !string.IsNullOrEmpty(inventoryItemNames[row])
                        ? $"{row}: {inventoryItemNames[row]}"
                        : $"{row}";
                    ImGui.Text(label);

                    foreach (var dir in Enum.GetValues<MenuNavigationDirection>())
                    {
                        ImGui.TableNextColumn();
                        ImGui.PushID($"InvItemNav_{row}_{dir}");
                        section.Entries[row].TryGetValue(dir, out var current);
                        var newVal = ImGuiExtensions.Input("##v", current, width: 80);
                        if (newVal != null)
                        {
                            section.Entries[row][dir] = newVal.Value;
                            pending.RecordChange();
                        }
                        ImGui.PopID();
                    }
                }

                ImGui.EndTable();
            }

            if (!editable) ImGui.EndDisabled();
        }

        public static void DrawFloorSafeInventoryItemRewardSection(
            FloorSafeInventoryItemRewardSection section, PendingEditorExecutableState pending,
            IList<string> equipmentNames)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("Floor Safe Inventory Item Rewards")) return;

            var editable = section.Editable();
            if (!editable) ImGui.BeginDisabled();

            var itemValues = new List<int>();
            var itemLabels = new List<string>();
            for (var i = 0; i < equipmentNames.Count; i++)
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

        public static void DrawWallTileDirectionSpriteSection(
            WallTileDirectionSpriteSection section, PendingEditorExecutableState pending)
        {
            if (!section.Viewable()) return;
            if (!ImGui.CollapsingHeader("Wall Tile Direction Sprites")) return;

            var editable = section.Editable();
            if (!editable) ImGui.BeginDisabled();

            var spriteValues = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
            var spriteLabels = new List<string>
            {
                "None",
                "Wall 1", "Wall 2", "Wall 3", "Wall 4",
                "Wall 5", "Wall 6", "Wall 7"
            };

            if (ImGui.BeginTable("WallDirSprites", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
                ImGui.TableSetupColumn("Direction");
                ImGui.TableSetupColumn("Sprite");
                ImGui.TableHeadersRow();

                for (var i = 0; i < 16; i++)
                {
                    var dir = (WallDirection)i;
                    ImGui.PushID($"WallDir_{i}");
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text($"{i}");

                    ImGui.TableNextColumn();
                    ImGui.Text(FormatWallDirection(dir));

                    ImGui.TableNextColumn();
                    if (section.Sprites.TryGetValue(dir, out var current))
                    {
                        var result = ImGuiExtensions.Input("##sprite", current, spriteValues, spriteLabels, width: 120);
                        if (result != null)
                        {
                            section.Sprites[dir] = (byte)result.Value;
                            pending.RecordChange();
                        }
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            if (!editable) ImGui.EndDisabled();
        }

        private static string FormatWallDirection(WallDirection dir)
        {
            if (dir == WallDirection.Unknown) return "(none)";
            var parts = new List<string>();
            if ((dir & WallDirection.North) != 0) parts.Add("North");
            if ((dir & WallDirection.South) != 0) parts.Add("South");
            if ((dir & WallDirection.West) != 0) parts.Add("West");
            if ((dir & WallDirection.East) != 0) parts.Add("East");
            return string.Join(" + ", parts);
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

        #endregion
    }
}
