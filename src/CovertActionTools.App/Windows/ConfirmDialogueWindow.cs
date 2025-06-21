using System.Numerics;
using CovertActionTools.App.ViewModels;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class ConfirmDialogueWindow : BaseWindow
{
    private readonly ILogger<ConfirmDialogueWindow> _logger;
    private readonly ConfirmDialogueState _state;

    public ConfirmDialogueWindow(ILogger<ConfirmDialogueWindow> logger, ConfirmDialogueState state)
    {
        _logger = logger;
        _state = state;
    }

    public override void Draw()
    {
        if (!_state.Show)
        {
            return;
        }
        var initialPos = new Vector2(50.0f, 50.0f);
        var initialSize = new Vector2(800.0f, 400.0f);
        ImGui.SetNextWindowSize(initialSize, ImGuiCond.Appearing);
        ImGui.SetNextWindowPos(initialPos, ImGuiCond.Appearing);
        ImGui.Begin("Confirm", ImGuiWindowFlags.Popup);
        DrawWindow();
        ImGui.End();
    }

    private void DrawWindow()
    {
        foreach (var text in _state.Texts)
        {
            ImGui.Text(text);
            ImGui.NewLine();
        }

        if (ImGui.BeginTable("buttons", 2, ImGuiTableFlags.None))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (ImGui.Button("Cancel"))
            {
                _state.Callback(false);
                _state.CloseDialog();
            }

            ImGui.TableNextColumn();
            if (ImGui.Button("Confirm"))
            {
                _state.Callback(true);
                _state.CloseDialog();
            }
            
            ImGui.EndTable();
        }
    }
}