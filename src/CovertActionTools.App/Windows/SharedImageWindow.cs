using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;

namespace CovertActionTools.App.Windows;

public abstract class SharedImageWindow : BaseWindow
{
    protected readonly RenderWindow RenderWindow;
    protected readonly ImageEditorState EditorState;

    protected SharedImageWindow(RenderWindow renderWindow, ImageEditorState editorState)
    {
        RenderWindow = renderWindow;
        EditorState = editorState;
    }

    protected void DrawImageTabs(string key, SharedImageModel image, Action recordChange, Action<bool>? toggleSpriteSheet, SimpleImageModel.SpriteSheetData? spriteSheet = null)
    {
        ImGui.BeginTabBar("ImageTabs", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton);

        if (ImGui.BeginTabItem("VGA"))
        {
            DrawVgaImageTab(key, image, recordChange, toggleSpriteSheet, spriteSheet);
            
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

    private void DrawVgaImageTab(string key, SharedImageModel image, 
        Action recordChange, Action<bool>? toggleSpriteSheet,
        SimpleImageModel.SpriteSheetData? spriteSheet = null)
    {
        if (ImGui.BeginTable("vga", 2))
        {
            ImGui.TableSetupColumn("image", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("menu", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawVgaImage(key, image, recordChange, spriteSheet);
            
            ImGui.TableNextColumn();
            if (toggleSpriteSheet != null)
            {
                var hasSpriteSheet = spriteSheet != null;
                var newHasSpriteSheet = ImGuiExtensions.Input("Is Sprite Sheet?", hasSpriteSheet);
                if (newHasSpriteSheet != null)
                {
                    toggleSpriteSheet(newHasSpriteSheet.Value);
                }
                else
                {
                    if (spriteSheet != null)
                    {
                        if (ImGui.Button("Add Sprite"))
                        {
                            spriteSheet.Sprites.Add("", new SimpleImageModel.Sprite());
                            recordChange();
                        }

                        var newShowSprites = ImGuiExtensions.Input("Show sprites?", EditorState.ShowSprites);
                        if (newShowSprites != null)
                        {
                            EditorState.ShowSprites = newShowSprites.Value;
                        }

                        var newShowSpriteNames =
                            ImGuiExtensions.Input("Show sprite names?", EditorState.ShowSpriteNames);
                        if (newShowSpriteNames != null)
                        {
                            EditorState.ShowSpriteNames = newShowSpriteNames.Value;
                        }

                        if (ImGui.BeginTable("sprites", 6))
                        {
                            var spriteKeys = spriteSheet.Sprites.Keys.ToList();
                            foreach (var spriteKey in spriteKeys)
                            {
                                var sprite = spriteSheet.Sprites[spriteKey];
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();

                                var newSpriteKey = ImGuiExtensions.Input("Key", spriteKey, 64);
                                if (!string.IsNullOrEmpty(newSpriteKey))
                                {
                                    spriteSheet.Sprites[newSpriteKey] = sprite;
                                    spriteSheet.Sprites.Remove(spriteKey);
                                    recordChange();
                                }

                                ImGui.TableNextColumn();
                                var newX = ImGuiExtensions.Input("X", sprite.X);
                                if (newX != null)
                                {
                                    sprite.X = newX.Value;
                                    recordChange();
                                }
                                
                                ImGui.TableNextColumn();
                                var newY = ImGuiExtensions.Input("Y", sprite.Y);
                                if (newY != null)
                                {
                                    sprite.Y = newY.Value;
                                    recordChange();
                                }
                                
                                ImGui.TableNextColumn();
                                var newW = ImGuiExtensions.Input("W", sprite.Width);
                                if (newW != null)
                                {
                                    sprite.Width = newW.Value;
                                    recordChange();
                                }
                                
                                ImGui.TableNextColumn();
                                var newH = ImGuiExtensions.Input("H", sprite.Height);
                                if (newH != null)
                                {
                                    sprite.Height = newH.Value;
                                    recordChange();
                                }
                                
                                ImGui.TableNextColumn();
                                if (ImGui.Button("Remove"))
                                {
                                    spriteSheet.Sprites.Remove(spriteKey);
                                    recordChange();
                                }
                            }
                            
                            ImGui.EndTable();
                        }
                    }
                }
            }
            
            ImGui.EndTable();
        }
    }

    private void DrawVgaImage(string key, SharedImageModel image, Action recordChange, SimpleImageModel.SpriteSheetData? spriteSheet = null)
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
        var texture = RenderWindow.RenderImage(RenderWindow.RenderType.Image, id, width, height, rawPixels);
        ImGui.Image(texture, new Vector2(width, height));
    
        if (spriteSheet != null && EditorState.ShowSprites)
        {
            foreach (var pair in spriteSheet.Sprites)
            {
                var spriteKey = pair.Key;
                var sprite = pair.Value;
            
                ImGui.SetCursorPos(pos + new Vector2(sprite.X - 1, sprite.Y - 1));
                var spriteOverlay = RenderWindow.RenderOutlineRectangle(1, sprite.Width + 2, sprite.Height + 2, (128, 255, 255, 255));
                ImGui.Image(spriteOverlay, new Vector2(sprite.Width + 2, sprite.Height + 2));

                if (EditorState.ShowSpriteNames)
                {
                    ImGui.SetCursorPos(pos + new Vector2(sprite.X, sprite.Y + sprite.Height - ImGui.GetTextLineHeight()));
                    ImGui.Text(spriteKey);
                }
            }
        }
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