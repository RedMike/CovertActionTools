using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Conversion;
using CovertActionTools.Core.Models;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace CovertActionTools.App.Windows;

public class SelectedFontWindow : BaseWindow
{
    private readonly ILogger<SelectedFontWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly RenderWindow _renderWindow;

    public SelectedFontWindow(ILogger<SelectedFontWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _renderWindow = renderWindow;
    }

    public override void Draw()
    {
        if (!_mainEditorState.IsPackageLoaded)
        {
            return;
        }

        if (_mainEditorState.SelectedItem == null ||
            _mainEditorState.SelectedItem.Value.type != MainEditorState.ItemType.Font)
        {
            return;
        }

        var fontId = int.Parse(_mainEditorState.SelectedItem.Value.id);
        
        var screenSize = ImGui.GetMainViewport().Size;
        var initialPos = new Vector2(300.0f, 20.0f);
        var initialSize = new Vector2(screenSize.X - 300.0f, screenSize.Y - 200.0f);
        ImGui.SetNextWindowSize(initialSize);
        ImGui.SetNextWindowPos(initialPos);
        ImGui.Begin($"Font", //TODO: change label but not ID to prevent unfocusing
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);
        
        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            
            DrawFontWindow(model, model.Fonts, fontId);
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawFontWindow(PackageModel model, FontsModel fonts, int fontId)
    {
        //TODO: keep a pending model and have a save button?

        var font = fonts.Fonts[fontId];
        var fontMetadata = fonts.ExtraData.Fonts[fontId];

        var pos = ImGui.GetCursorPos();
        var x = 0;
        var y = 0;
        var i = 0;
        for (var code = fontMetadata.FirstAsciiValue; code < fontMetadata.LastAsciiValue; code++)
        {
            var ox = x + fontMetadata.HorizontalPadding;
            var oy = y + fontMetadata.VerticalPadding;
            var imageBytes = font.CharacterImages[(char)code];
            var width = fontMetadata.CharacterWidths[(char)code];
            var height = fontMetadata.CharHeight;

            using var skBitmap = SKBitmap.Decode(imageBytes, new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var image = skBitmap.Bytes.ToArray();
            
            ImGui.SetCursorPos(pos + new Vector2(ox, oy));
            var id = $"font_{fontId}_{code}";
            //TODO: cache?
            var texture = _renderWindow.RenderImage(RenderWindow.RenderType.Image, id, width, height, image);
            ImGui.Image(texture, new Vector2(width, height));

            i++;
            if (i % 16 == 0)
            {
                x = 0;
                y += fontMetadata.VerticalPadding + height;
            }
            else
            {
                x += fontMetadata.HorizontalPadding + width;
            }
        }
    }
}