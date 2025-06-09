using System.Linq.Expressions;
using System.Reflection;
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
}