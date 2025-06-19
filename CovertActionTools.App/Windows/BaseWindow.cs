using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;

namespace CovertActionTools.App.Windows;

public abstract class BaseWindow
{
    public abstract void Draw();

    protected void DrawSharedMetadataEditor(SharedMetadata metadata, Action? onChange = null)
    {
        var contentSize = ImGui.GetContentRegionAvail();
        ImGui.SetNextItemWidth(contentSize.X);
        var newName = ImGuiExtensions.Input("Name", metadata.Name, 256);
        if (newName != null)
        {
            metadata.Name = newName;
            if (onChange != null)
            {
                onChange();
            }
        }

        var comment = metadata.Comment;
        ImGui.InputTextMultiline("Comment", ref comment, 4096, new Vector2(contentSize.X, 150.0f));
        if (comment != metadata.Comment)
        {
            metadata.Comment = comment;
            if (onChange != null)
            {
                onChange();
            }
        }
    }
}