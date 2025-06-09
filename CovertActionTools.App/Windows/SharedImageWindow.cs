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

    protected void DrawImageTabs(SimpleImageModel image, Action recordChange)
    {
        ImGui.BeginTabBar("ImageTabs", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton);

        if (image.ModernImageData != null && image.ModernImageData.Length > 0)
        {
            if (ImGui.BeginTabItem("Modern"))
            {
                DrawModernImageTab(image, recordChange);

                ImGui.EndTabItem();
            }
        }

        if (ImGui.BeginTabItem("Legacy VGA"))
        {
            DrawVgaImageTab(image, recordChange);
            
            ImGui.EndTabItem();
        }
        
        if (ImGui.BeginTabItem("Legacy CGA"))
        {
            DrawCgaImageTab(image, recordChange);
            
            ImGui.EndTabItem();
        }
        
        ImGui.EndTabBar();
    }

    private void DrawVgaImageTab(SimpleImageModel image, Action recordChange)
    {
        var width = image.ExtraData.LegacyWidth;
        var height = image.ExtraData.LegacyHeight;
        var rawPixels = image.VgaImageData;

        var pos = ImGui.GetCursorPos();
        var bgTexture = RenderWindow.RenderCheckerboardRectangle(25, width, height,
            (40, 30, 40, 255), (50, 40, 50, 255));
        ImGui.Image(bgTexture, new Vector2(width, height));

        ImGui.SetCursorPos(pos);
        var id = $"image_vga_{image.Key}";
        //TODO: cache?
        var texture = RenderWindow.RenderImage(RenderWindow.RenderType.Image, id, width, height, rawPixels);
        
        ImGui.Image(texture, new Vector2(width, height));
    }
    
    private void DrawCgaImageTab(SimpleImageModel image, Action recordChange)
    {
        if (image.ExtraData.LegacyColorMappings == null)
        {
            ImGui.Text("No CGA mapping, TODO");
            return;
        }
        
        var width = image.ExtraData.LegacyWidth;
        var height = image.ExtraData.LegacyHeight;
        var rawPixels = image.CgaImageData;

        var pos = ImGui.GetCursorPos();
        var bgTexture = RenderWindow.RenderCheckerboardRectangle(25, width, height,
            (40, 30, 40, 255), (50, 40, 50, 255));
        ImGui.Image(bgTexture, new Vector2(width, height));

        ImGui.SetCursorPos(pos);
        var id = $"image_cga_{image.Key}";
        //TODO: cache?
        var texture = RenderWindow.RenderImage(RenderWindow.RenderType.Image, id, width, height, rawPixels);
        
        ImGui.Image(texture, new Vector2(width, height));
        
        //TODO: allow changing CGA mapping
        //TODO: split CGA mapping into separate colour choices for each pixel
        for (byte i = 0; i < 8; i++)
        {
            var m = (int)image.ExtraData.LegacyColorMappings[i];
            ImGui.SetNextItemWidth(100.0f);
            ImGui.InputInt($"Color {i:00}", ref m);

            ImGui.SameLine();
            
            var j = (byte)(i + 8);
            var m2 = (int)image.ExtraData.LegacyColorMappings[j];
            ImGui.SetNextItemWidth(100.0f);
            ImGui.InputInt($"Color {j:00}", ref m2);
        }
    }
    
    private void DrawModernImageTab(SimpleImageModel image, Action recordChange)
    {
        ImGui.Text("Note: this image will not be shown in the original game engine.");
        var width = image.ExtraData.Width;
        var origWidth = width;
        ImGui.SetNextItemWidth(100.0f);
        ImGui.InputInt("Width", ref width);
        if (width != origWidth)
        {
            //TODO: resize? confirmation dialog?
        }
        
        ImGui.SameLine();
        
        var height = image.ExtraData.Height;
        var origHeight = height;
        ImGui.SetNextItemWidth(100.0f);
        ImGui.InputInt("Height", ref height);
        if (height != origHeight)
        {
            //TODO: resize? confirmation dialog?
        }

        ImGui.Text("");

        DrawModernImage(image, recordChange);
        
        //TODO: 'generate VGA from this' button?
    }

    private void DrawModernImage(SimpleImageModel image, Action recordChange)
    {
        var width = image.ExtraData.Width;
        var height = image.ExtraData.Height;
        
        var pos = ImGui.GetCursorPos();
        var bgTexture = RenderWindow.RenderCheckerboardRectangle(25, width, height,
            (40, 30, 40, 255), (50, 40, 50, 255));
        ImGui.Image(bgTexture, new Vector2(width, height));

        ImGui.SetCursorPos(pos);
        
        var id = $"image_{image.Key}";
        var rawPixels = image.ModernImageData;
        //TODO: cache?
        var texture = RenderWindow.RenderImage(RenderWindow.RenderType.Image, id, width, height, rawPixels);
        
        ImGui.Image(texture, new Vector2(width, height));
    }
}