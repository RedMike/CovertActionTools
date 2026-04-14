using System;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;

namespace CovertActionTools.App;

public static class ImGuiExtensions
{
    public static void SameLineSpace()
    {
        ImGui.SameLine();
        ImGui.Text("");
        ImGui.SameLine();
    }
    
    public static int? Input(string label, int value, string? id = null, int? width = null)
    {
        if (width != null)
        {
            ImGui.SetNextItemWidth((float)width);
        }

        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PushID(id);
        }
        var origValue = value;
        ImGui.InputInt(label, ref value);
        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PopID();
        }

        if (value == origValue)
        {
            return null;
        }

        return value;
    }
    
    public static int? Input(string label, int value, List<int> validValues, List<string>? valueStrings = null, string? id = null, int? width = null)
    {
        if (width != null)
        {
            ImGui.SetNextItemWidth((float)width);
        }

        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PushID(id);
        }

        if (valueStrings == null)
        {
            valueStrings = validValues.Select(x => $"{x}").ToList();
        }
        var origIndex = validValues.FindIndex(x => x == value);
        var index = origIndex;
        ImGui.Combo(label, ref index, valueStrings.ToArray(), validValues.Count);
        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PopID();
        }

        if (index == origIndex)
        {
            return null;
        }

        return validValues[index];
    }
    
    public static string? Input(string label, string value, List<string> validValues, List<string>? valueStrings, string? id = null, int? width = null)
    {
        if (width != null)
        {
            ImGui.SetNextItemWidth((float)width);
        }

        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PushID(id);
        }

        if (valueStrings == null)
        {
            valueStrings = validValues.Select(x => $"{x}").ToList();
        }
        var origIndex = validValues.FindIndex(x => x == value);
        var index = origIndex;
        ImGui.Combo(label, ref index, valueStrings.ToArray(), validValues.Count);
        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PopID();
        }

        if (index == origIndex)
        {
            return null;
        }

        return validValues[index];
    }
    
    public static string? Input(string label, string value, int maxLength, string? id = null, int? width = null, bool readOnly = false)
    {
        if (width != null)
        {
            ImGui.SetNextItemWidth((float)width);
        }

        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PushID(id);
        }
        var origValue = value;
        var flags = ImGuiInputTextFlags.None;
        if (readOnly)
        {
            flags |= ImGuiInputTextFlags.ReadOnly;
        }
        ImGui.InputText(label, ref value, (uint)maxLength, flags);
        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PopID();
        }

        if (value == origValue)
        {
            return null;
        }

        return value;
    }
    
    public static string? InputMultiline(string label, string value, int maxLength, Vector2 size, string? id = null, bool readOnly = false)
    {
        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PushID(id);
        }
        var origValue = value;
        var flags = ImGuiInputTextFlags.None;
        if (readOnly)
        {
            flags |= ImGuiInputTextFlags.ReadOnly;
        }
        ImGui.InputTextMultiline(label, ref value, (uint)maxLength, size, flags);
        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PopID();
        }

        // ImGui.InputTextMultiline strips trailing newlines from the ref string.
        // Restore them so that strings containing meaningful trailing \n roundtrip
        // correctly through the editor without data corruption.
        if (origValue.EndsWith("\n") && !value.EndsWith("\n"))
        {
            var stripped = origValue.TrimEnd('\n');
            if (value == stripped)
            {
                return null;
            }
            value += origValue.Substring(stripped.Length);
        }

        if (value == origValue)
        {
            return null;
        }

        return value;
    }

    public static bool DrawMenuStringRecord(
        string label, CovertActionTools.Core.Models.Executables.Records.Shared.MenuStringRecord record,
        Vector2 editorSize, Action onChanged)
    {
        var changed = false;
        ImGui.PushID(label);

        var newHeader = InputMultiline("Header", record.Header, 256, editorSize, id: $"{label}_hdr");
        if (newHeader != null) { record.Header = newHeader; onChanged(); changed = true; }

        for (var i = 0; i < record.Options.Length; i++)
        {
            var newOpt = InputMultiline($"Option {i + 1}", record.Options[i], 256, editorSize, id: $"{label}_opt{i}");
            if (newOpt != null) { record.Options[i] = newOpt; onChanged(); changed = true; }
        }

        ImGui.PopID();
        return changed;
    }

    public static bool? Input(string label, bool value, string? id = null, int? width = null)
    {
        if (width != null)
        {
            ImGui.SetNextItemWidth((float)width);
        }

        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PushID(id);
        }
        var origValue = value;
        ImGui.Checkbox(label, ref value);
        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PopID();
        }

        if (value == origValue)
        {
            return null;
        }

        return value;
    }
    
    public static string? Input(string label, string value, List<string> values, string? id = null, int? width = null)
    {
        if (width != null)
        {
            ImGui.SetNextItemWidth((float)width);
        }

        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PushID(id);
        }

        var strings = values.ToArray();
        var valueIndex = values.FindIndex(x => x.Equals(value));
        var origValueIndex = valueIndex;
        ImGui.Combo(label, ref valueIndex, strings, strings.Length);
        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PopID();
        }

        if (valueIndex == origValueIndex)
        {
            return null;
        }

        return values[valueIndex];
    }
    
    public static TEnum? InputEnum<TEnum>(string label, TEnum value, bool includeUnknown, TEnum unknown = default, string? id = null, int? width = null)
        where TEnum : struct, Enum
    {
        if (width != null)
        {
            ImGui.SetNextItemWidth((float)width);
        }

        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PushID(id);
        }
        var values = Enum.GetValues<TEnum>()
            .Where(x => !x.Equals(unknown))
            .ToList();
        var strings = values.Select(x => $"{x}").ToArray();
        var valueIndex = values.FindIndex(x => x.Equals(value));
        var origValueIndex = valueIndex;
        ImGui.Combo(label, ref valueIndex, strings, strings.Length);
        if (!string.IsNullOrEmpty(id))
        {
            ImGui.PopID();
        }

        if (valueIndex == origValueIndex)
        {
            return null;
        }

        return values[valueIndex];
    }

    public static TData PendingSaveChanges<TData>(PendingEditorState<TData> pendingState, string selectedIndex, Func<TData> get, Action<TData> onSave)
    {
        TData data;
        if (string.IsNullOrEmpty(pendingState.Id) || pendingState.Id != selectedIndex)
        {
            data = get();
            pendingState.Reset(selectedIndex, data);
        }
        else
        {
            if (pendingState.PendingData == null)
            {
                throw new Exception("Missing pending data");
            }
            data = pendingState.PendingData;
        }
        var windowSize = ImGui.GetContentRegionAvail();
        if (pendingState.HasChanges && pendingState.PendingData != null)
        {
            if (ImGui.Button("Save Changes", new Vector2(windowSize.X, 30.0f)))
            {
                onSave(pendingState.PendingData);
                pendingState.Reset(selectedIndex, get());
            }
            ImGui.NewLine();
        }

        return data;
    }
}