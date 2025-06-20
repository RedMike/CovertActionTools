using System.Numerics;
using CovertActionTools.Core.Models;
using ImGuiNET;

namespace CovertActionTools.App.Windows;

public abstract class SharedImageWindow : BaseWindow
{
    protected readonly RenderWindow RenderWindow;

    protected SharedImageWindow(RenderWindow renderWindow)
    {
        RenderWindow = renderWindow;
    }

    protected void DrawImageTabs(string key, SharedImageModel image, Action recordChange)
    {
        ImGui.BeginTabBar("ImageTabs", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton);

        if (ImGui.BeginTabItem("VGA"))
        {
            DrawVgaImageTab(key, image, recordChange);
            
            ImGui.EndTabItem();
        }
        
        if (ImGui.BeginTabItem("Data"))
        {
            DrawDataTab(key, image, recordChange);
            
            ImGui.EndTabItem();
        }
        
        if (ImGui.BeginTabItem("CGA"))
        {
            DrawCgaImageTab(key, image, recordChange);
            
            ImGui.EndTabItem();
        }
        
        ImGui.EndTabBar();
    }

    private void DrawDataTab(string key, SharedImageModel image, Action recordChange)
    {
        if (ImGui.BeginTable("data", 3))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var newType = ImGuiExtensions.InputEnum("Type", image.Data.Type, true, SharedImageModel.ImageType.Unknown);
            if (newType != null)
            {
                image.Data.Type = newType.Value;
                recordChange();
            }
            
            ImGui.TableNextColumn();
            var newWidth = ImGuiExtensions.Input("Width", image.Data.Width);
            if (newWidth != null)
            {
                //TODO: resize? confirmation dialog?
                recordChange();
            }

            ImGui.TableNextColumn();
            var newHeight = ImGuiExtensions.Input("Height", image.Data.Height);
            if (newHeight != null)
            {
                //TODO: resize? confirmation dialog?
                recordChange();
            }
            
            ImGui.EndTable();
        }
    }

    private void DrawVgaImageTab(string key, SharedImageModel image, Action recordChange)
    {
        var width = image.Data.Width;
        var height = image.Data.Height;
        var rawPixels = image.VgaImageData;

        var pos = ImGui.GetCursorPos();
        var bgTexture = RenderWindow.RenderCheckerboardRectangle(25, width, height,
            (40, 30, 40, 255), (50, 40, 50, 255));
        ImGui.Image(bgTexture, new Vector2(width, height));

        ImGui.SetCursorPos(pos);
        var id = $"image_vga_{key}";
        //TODO: cache?
        var texture = RenderWindow.RenderImage(RenderWindow.RenderType.Image, id, width, height, rawPixels);
        
        ImGui.Image(texture, new Vector2(width, height));
    }
    
    private void DrawCgaImageTab(string key, SharedImageModel image, Action recordChange)
    {
        if (image.Data.LegacyColorMappings == null)
        {
            ImGui.Text("No CGA mapping, TODO");
            return;
        }
        
        var width = image.Data.Width;
        var height = image.Data.Height;
        var rawPixels = image.CgaImageData;

        var pos = ImGui.GetCursorPos();
        var bgTexture = RenderWindow.RenderCheckerboardRectangle(25, width, height,
            (40, 30, 40, 255), (50, 40, 50, 255));
        ImGui.Image(bgTexture, new Vector2(width, height));

        ImGui.SetCursorPos(pos);
        var id = $"image_cga_{key}";
        //TODO: cache?
        var texture = RenderWindow.RenderImage(RenderWindow.RenderType.Image, id, width, height, rawPixels);
        
        ImGui.Image(texture, new Vector2(width, height));
        
        //TODO: allow changing CGA mapping
        //TODO: split CGA mapping into separate colour choices for each pixel
        for (byte i = 0; i < 8; i++)
        {
            var m = (int)image.Data.LegacyColorMappings[i];
            ImGui.SetNextItemWidth(100.0f);
            ImGui.InputInt($"Color {i:00}", ref m);

            ImGui.SameLine();
            
            var j = (byte)(i + 8);
            var m2 = (int)image.Data.LegacyColorMappings[j];
            ImGui.SetNextItemWidth(100.0f);
            ImGui.InputInt($"Color {j:00}", ref m2);
        }
    }
}