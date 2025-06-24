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
    private readonly FontPreviewState _fontPreviewState;
    private readonly PendingEditorFontState _pendingState;

    public SelectedFontWindow(ILogger<SelectedFontWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow, FontPreviewState fontPreviewState, PendingEditorFontState pendingState)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _renderWindow = renderWindow;
        _fontPreviewState = fontPreviewState;
        _pendingState = pendingState;
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
        ImGui.Begin("Font",
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);
        
        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            
            DrawFontWindow(model, fontId);
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawFontWindow(PackageModel model, int fontId)
    {
        var fonts = ImGuiExtensions.PendingSaveChanges(_pendingState, "id",
            () => model.Fonts.Clone(),
            (data) =>
            {
                model.Fonts = data;
                _mainEditorState.RecordChange();
                if (!model.Index.FontChanges)
                {
                    model.Index.FontChanges = true;
                    model.Index.FontIncluded = true;
                }
            });
        
        var newSampleString = ImGuiExtensions.Input("Sample text", _fontPreviewState.SampleString, 64);
        if (newSampleString != null)
        {
            _fontPreviewState.SampleString = newSampleString;
        }
        
        var font = fonts.Fonts[fontId];
        var fontMetadata = fonts.Data.Fonts[fontId];

        DrawFontSample(_fontPreviewState.SampleString, font, fontMetadata, fontId);
        
        ImGui.Text("");
        
        DrawFontSheet(font, fontMetadata, fontId);
    }

    private void DrawFontSample(string text, FontsModel.Font font, FontsModel.FontMetadata fontMetadata, int fontId)
    {
        var pos = ImGui.GetCursorPos();
        var x = 0;
        var y = 0;
        foreach (var c in text)
        {
            var charToUse = c;
            if (!font.CharacterImages.ContainsKey(c))
            {
                charToUse = ' ';
            }
            
            var ox = x + fontMetadata.HorizontalPadding;
            var oy = y + fontMetadata.VerticalPadding;
            var image = font.CharacterImages[charToUse];
            var width = fontMetadata.CharacterWidths[charToUse];
            var height = fontMetadata.CharHeight;

            var imageBytes = image.VgaImageData;
            
            ImGui.SetCursorPos(pos + new Vector2(ox, oy));
            var id = $"font_{fontId}_{(byte)charToUse}";
            var texture = _renderWindow.RenderImage(RenderWindow.RenderType.Image, id, width, height, imageBytes);
            ImGui.Image(texture, new Vector2(width, height));

            x += width + fontMetadata.HorizontalPadding;
        }
    }

    private void DrawFontSheet(FontsModel.Font font, FontsModel.FontMetadata fontMetadata, int fontId)
    {
        var pos = ImGui.GetCursorPos();
        var x = 0;
        var y = 0;
        var i = 0;
        for (var code = fontMetadata.FirstAsciiValue; code <= fontMetadata.LastAsciiValue; code++)
        {
            var ox = x + fontMetadata.HorizontalPadding;
            var oy = y + fontMetadata.VerticalPadding;
            var image = font.CharacterImages[(char)code];
            var width = fontMetadata.CharacterWidths[(char)code];
            var height = fontMetadata.CharHeight;

            var imageBytes = image.VgaImageData;
            
            ImGui.SetCursorPos(pos + new Vector2(ox, oy));
            var id = $"font_{fontId}_{code}";
            var texture = _renderWindow.RenderImage(RenderWindow.RenderType.Image, id, width, height, imageBytes);
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