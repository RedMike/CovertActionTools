using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedTextWindow : BaseWindow
{
    private readonly ILogger<SelectedTextWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly RenderWindow _renderWindow;

    public SelectedTextWindow(ILogger<SelectedTextWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow)
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
            _mainEditorState.SelectedItem.Value.type != MainEditorState.ItemType.Text)
        {
            return;
        }

        var textType = TextModel.StringType.Unknown;
        int? crimeId = null;
        if (_mainEditorState.SelectedItem.Value.id.Contains(" "))
        {
            var parts = _mainEditorState.SelectedItem.Value.id.Split(" ");
            textType = Enum.Parse<TextModel.StringType>(parts[0]);
            crimeId = int.Parse(parts[1]);
        }
        else
        {
            textType = Enum.Parse<TextModel.StringType>(_mainEditorState.SelectedItem.Value.id);
        }
        
        var screenSize = ImGui.GetMainViewport().Size;
        var initialPos = new Vector2(300.0f, 20.0f);
        var initialSize = new Vector2(screenSize.X - 300.0f, screenSize.Y - 200.0f);
        ImGui.SetNextWindowSize(initialSize);
        ImGui.SetNextWindowPos(initialPos);
        ImGui.Begin($"Text {textType}", //TODO: change label but not ID to prevent unfocusing
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);
        
        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            
            DrawTextWindow(model, textType, crimeId);
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawTextWindow(PackageModel model, TextModel.StringType textType, int? crimeId)
    {
        //TODO: keep a pending model and have a save button?

        var texts = model.Texts.Values
            .Where(x => x.Type == textType && x.CrimeId == crimeId)
            .OrderBy(x => x.CrimeId)
            .ThenBy(x => x.Id)
            .ToList();
        foreach (var text in texts)
        {
            var origId = text.Id;
            var id = origId;
            ImGui.SetNextItemWidth(100.0f);
            ImGui.InputInt("ID", ref id);
            if (id != origId)
            {
                if (model.Texts.Any(x => x.Value.Id == id && x.Value.Type == textType))
                {
                    ImGui.SameLine();
                    ImGui.Text("Key already taken");
                }
                else
                {
                    //TODO: change ID?
                }
            }

            ImGui.SameLine();
            ImGui.Text("");
            ImGui.SameLine();

            if (textType == TextModel.StringType.CrimeMessage)
            {
                ImGui.SetNextItemWidth(100.0f);
                var crime = text.CrimeId ?? -1;
                var origCrime = crime;
                ImGui.InputInt("Crime ID", ref crime);
                if (crime != origCrime)
                {
                    //TODO: change crime ID
                }
            }
            else
            {
                ImGui.SetNextItemWidth(100.0f);
                var crime = 0;
                ImGui.InputInt("Crime ID", ref crime, 1, 1, ImGuiInputTextFlags.ReadOnly);
            }
            
            ImGui.SameLine();
            ImGui.Text("");
            ImGui.SameLine();
            
            var windowSize = ImGui.GetContentRegionAvail();
            var message = text.Message.Replace("\r", ""); //strip out \r and re-add after, for consistency across OS
            var origMessage = message;
            ImGui.InputTextMultiline($"Message {text.GetMessagePrefix()}", ref message, 1024, new Vector2(windowSize.X, 50.0f),
                ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.CtrlEnterForNewLine);
            if (message != origMessage)
            {
                var fixedMessage = message.Replace("\n", "\r\n"); //re-add \r, for consistency across OS
                //TODO: change message
            }
            
            ImGui.Text("");
            ImGui.Separator();
            ImGui.Text("");
        }
    }
}