using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Models;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedPlotWindow : BaseWindow
{
    private readonly ILogger<SelectedPlotWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly RenderWindow _renderWindow;
    private readonly PendingEditorPlotState _pendingState;

    public SelectedPlotWindow(ILogger<SelectedPlotWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow, PendingEditorPlotState pendingState)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _renderWindow = renderWindow;
        _pendingState = pendingState;
    }
    
    public override void Draw()
    {
        if (!_mainEditorState.IsPackageLoaded)
        {
            return;
        }

        if (_mainEditorState.SelectedItem == null ||
            _mainEditorState.SelectedItem.Value.type != MainEditorState.ItemType.Plot)
        {
            return;
        }

        var missionSetId = int.Parse(_mainEditorState.SelectedItem.Value.id);
        
        var screenSize = ImGui.GetMainViewport().Size;
        var initialPos = new Vector2(300.0f, 20.0f);
        var initialSize = new Vector2(screenSize.X - 300.0f, screenSize.Y - 200.0f);
        ImGui.SetNextWindowSize(initialSize);
        ImGui.SetNextWindowPos(initialPos);
        ImGui.Begin("Plots",
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoNav | 
            ImGuiWindowFlags.NoCollapse);
        
        if (_mainEditorState.LoadedPackage != null)
        {
            var model = _mainEditorState.LoadedPackage;
            
            DrawPlotWindow(model, missionSetId);
        }
        else
        {
            ImGui.Text("Something went wrong, no package loaded..");
        }

        ImGui.End();
    }

    private void DrawPlotWindow(PackageModel model, int missionSetId)
    {
        Dictionary<string, PlotModel> allPlots;
        if (string.IsNullOrEmpty(_pendingState.Id))
        {
            allPlots = model.Plots.ToDictionary(x => x.Key, x => x.Value.Clone());
            _pendingState.Reset("id", allPlots);
        }
        else
        {
            if (_pendingState.PendingData == null)
            {
                return;
            }
            allPlots = _pendingState.PendingData;
        }
        var windowSize = ImGui.GetContentRegionAvail();
        if (_pendingState.HasChanges && _pendingState.PendingData != null)
        {
            if (ImGui.Button("Save Changes", new Vector2(windowSize.X, 30.0f)))
            {
                model.Plots = _pendingState.PendingData;
                _pendingState.Reset("id", model.Plots.ToDictionary(x => x.Key, x => x.Value.Clone()));
                _mainEditorState.RecordChange();
                if (!model.Index.PlotChanges)
                {
                    model.Index.PlotChanges = true;
                    model.Index.PlotIncluded = true;
                }
            }
            ImGui.NewLine();
        }
        var plots = allPlots.Values
            .Where(x => x.MissionSetId == missionSetId)
            .OrderBy(x => x.CrimeIndex ?? -1)
            .ThenBy(x => x.StringType == PlotModel.PlotStringType.Briefing ? int.MinValue : (int)x.StringType)
            .ThenBy(x => x.MessageNumber)
            .ToList();
        var i = 0;
        foreach (var plot in plots)
        {
            ImGui.PushID($"Plot_{i++}");
            ImGui.SetNextItemWidth(100.0f);
            var plotMissionSetId = missionSetId;
            ImGui.InputInt("Mission Set ID", ref plotMissionSetId, 1, 1, ImGuiInputTextFlags.ReadOnly);
            
            ImGui.SameLine();
            ImGui.Text("");
            ImGui.SameLine();
            
            if (plot.StringType == PlotModel.PlotStringType.Briefing)
            {
                ImGui.SetNextItemWidth(100.0f);
                var crime = 0;
                ImGui.InputInt("Crime Index", ref crime, 1, 1, ImGuiInputTextFlags.ReadOnly);
            }
            else
            {
                ImGui.SetNextItemWidth(100.0f);
                var crime = plot.CrimeIndex ?? -1;
                var origCrime = crime;
                ImGui.InputInt("Crime Index", ref crime, 1, 1, ImGuiInputTextFlags.ReadOnly);
                if (crime != origCrime)
                {
                    //TODO: change crime index
                }
            }
            
            ImGui.SameLine();
            ImGui.Text("");
            ImGui.SameLine();
            
            ImGui.SetNextItemWidth(150.0f);
            var types = Enum.GetValues<PlotModel.PlotStringType>()
                .Where(x => x != PlotModel.PlotStringType.Unknown)
                .Select(x => $"{x}")
                .ToArray();
            var typeIndex = types.ToList().FindIndex(x => x == plot.StringType.ToString());
            var origTypeIndex = typeIndex;
            ImGui.Combo("Type", ref typeIndex, types, types.Length);
            if (typeIndex != origTypeIndex)
            {
                //TODO: change
            }
            
            ImGui.SameLine();
            ImGui.Text("");
            ImGui.SameLine();
            
            ImGui.SetNextItemWidth(100.0f);
            var messageNumber = plot.MessageNumber;
            var origMessageNumber = messageNumber;
            ImGui.InputInt("Msg Number", ref messageNumber, 1, 1, ImGuiInputTextFlags.ReadOnly);
            if (messageNumber != origMessageNumber)
            {
                //TODO: change message number
            }
            
            var message = plot.Message.Replace("\r", ""); //strip out \r and re-add after, for consistency across OS
            var origMessage = message;
            ImGui.InputTextMultiline($"Message {plot.GetMessagePrefix()}", ref message, 1024, new Vector2(windowSize.X, 60.0f),
                ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.CtrlEnterForNewLine);
            if (message != origMessage)
            {
                var fixedMessage = message.Replace("\n", "\r\n"); //re-add \r, for consistency across OS
                plot.Message = fixedMessage;
                _pendingState.RecordChange();
            }
            
            ImGui.Text("");
            ImGui.Separator();
            ImGui.Text("");
            
            ImGui.PopID();
        }
    }
}