using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Exporting;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SavePackageWindow : BaseWindow
{
    private readonly ILogger<SavePackageWindow> _logger;
    private readonly AppLoggingState _appLogging;
    private readonly SavePackageState _savePackageState;
    private readonly MainEditorState _mainEditorState;
    private readonly IPackageExporter _exporter;

    public SavePackageWindow(ILogger<SavePackageWindow> logger, AppLoggingState appLogging, SavePackageState savePackageState, MainEditorState mainEditorState, IPackageExporter exporter)
    {
        _logger = logger;
        _appLogging = appLogging;
        _savePackageState = savePackageState;
        _mainEditorState = mainEditorState;
        _exporter = exporter;
    }

    public override void Draw()
    {
        if (!_savePackageState.Show)
        {
            return;
        }

        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);
        ImGui.Begin("Save Package", 
            ImGuiWindowFlags.NoResize | 
            ImGuiWindowFlags.NoMove | 
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.Modal);
        
        if (_savePackageState.Run)
        {
            DrawRunning();
        }
        else
        {
            DrawNotRunning();
        }

        ImGui.End();
    }

    private void DrawRunning()
    {
        if (_savePackageState.Exporter == null)
        {
            throw new Exception("Missing exporter");
        }
        var destPath = _savePackageState.DestinationPath;
        var publishPath = _savePackageState.PublishPath;
        var exportStatus = _savePackageState.Exporter.CheckStatus() ?? new ExportStatus();
        
        ImGui.Text($"Saving package to folder: {destPath}");
        if (!string.IsNullOrEmpty(publishPath))
        {
            ImGui.Text($"Publishing to folder: {publishPath}");
        }

        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        var windowSize = ImGui.GetContentRegionMax();

        var progress = 0.1f;
        switch (exportStatus.Stage)
        {
            case ExportStatus.ExportStage.Preparing:
                progress = 0.1f;
                break;
            case ExportStatus.ExportStage.ProcessingSimpleImages:
                if (exportStatus.StageItems <= 0)
                {
                    progress = 0.1f;
                }
                else
                {
                    progress = 0.1f + ((float)exportStatus.StageItemsDone / exportStatus.StageItems) * 0.1f;
                }

                break;
            case ExportStatus.ExportStage.ProcessingCrimes:
                if (exportStatus.StageItems <= 0)
                {
                    progress = 0.2f;
                }
                else
                {
                    progress = 0.2f + ((float)exportStatus.StageItemsDone / exportStatus.StageItems) * 0.1f;
                }

                break;
            case ExportStatus.ExportStage.ProcessingTexts:
                if (exportStatus.StageItems <= 0)
                {
                    progress = 0.3f;
                }
                else
                {
                    progress = 0.3f + ((float)exportStatus.StageItemsDone / exportStatus.StageItems) * 0.1f;
                }

                break;
            case ExportStatus.ExportStage.ProcessingClues:
                if (exportStatus.StageItems <= 0)
                {
                    progress = 0.4f;
                }
                else
                {
                    progress = 0.4f + ((float)exportStatus.StageItemsDone / exportStatus.StageItems) * 0.1f;
                }
                break;
            case ExportStatus.ExportStage.ProcessingPlots:
                if (exportStatus.StageItems <= 0)
                {
                    progress = 0.5f;
                }
                else
                {
                    progress = 0.5f + ((float)exportStatus.StageItemsDone / exportStatus.StageItems) * 0.1f;
                }
                break;
            case ExportStatus.ExportStage.ProcessingWorlds:
                if (exportStatus.StageItems <= 0)
                {
                    progress = 0.6f;
                }
                else
                {
                    progress = 0.6f + ((float)exportStatus.StageItemsDone / exportStatus.StageItems) * 0.1f;
                }
                break;
            case ExportStatus.ExportStage.ProcessingCatalogs:
                if (exportStatus.StageItems <= 0)
                {
                    progress = 0.7f;
                }
                else
                {
                    progress = 0.7f + ((float)exportStatus.StageItemsDone / exportStatus.StageItems) * 0.1f;
                }
                break;
        }

        var progressBarSize = new Vector2(windowSize.X - 20.0f, 15.0f);
        ImGui.ProgressBar(progress, progressBarSize);
        
        var text = $"{exportStatus.StageMessage} {exportStatus.StageItemsDone}/{exportStatus.StageItems}";
        var textSize = ImGui.CalcTextSize(text);
        var oldCursorPos = ImGui.GetCursorPos();
        try
        {   
            ImGui.SetCursorPos(new Vector2(windowSize.X/2.0f - textSize.X/2.0f, oldCursorPos.Y));
            ImGui.Text(text);
        }
        finally
        {
            ImGui.SetCursorPos(oldCursorPos);
            ImGui.Text("");
        }

        ImGui.Text("");
        if (exportStatus.Stage == ExportStatus.ExportStage.Unknown || 
            exportStatus.Stage == ExportStatus.ExportStage.FatalError ||
            exportStatus.Stage == ExportStatus.ExportStage.ExportDone)
        {
            var now = DateTime.Now;
            if (exportStatus.Errors.Count == 0)
            {
                _savePackageState.Show = false;
                _savePackageState.Run = false;
                _savePackageState.Exporter = null;
            }
            else
            {
                if (ImGui.Button("Close"))
                {
                    _savePackageState.Show = false;
                    _savePackageState.Run = false;
                    _savePackageState.Exporter = null;
                }
            }
        }
        
        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        var cursorPos = ImGui.GetCursorPos();
        var logSize = new Vector2(windowSize.X - cursorPos.X, windowSize.Y - cursorPos.Y);
        ImGui.BeginChild("SaveLogs", logSize, true, ImGuiWindowFlags.ChildWindow);
        var logs = _appLogging.Logs.ToList();
        foreach (var log in logs)
        {
            ImGui.TextUnformatted(log);
        }
        ImGui.EndChild();
    }
    
    private void DrawNotRunning()
    {
        //TODO: better file explorer?
        var origDestPath = _savePackageState.DestinationPath ?? "";
        var destPath = origDestPath;
        ImGui.InputText("Package Path", ref destPath, 256);
        if (destPath != origDestPath)
        {
            _savePackageState.DestinationPath = destPath;
        }
        
        var origPublishPath = _savePackageState.PublishPath ?? "";
        var publishPath = origPublishPath;
        ImGui.InputText("Publish Path", ref publishPath, 256);
        if (publishPath != origPublishPath)
        {
            _savePackageState.PublishPath = publishPath;
        }
        
        ImGui.Separator();

        if (ImGui.Button("Cancel"))
        {
            _savePackageState.Show = false;
        }

        ImGui.SameLine();
        if (ImGui.Button("Save"))
        {
            var now = DateTime.Now;
            _savePackageState.Exporter = _exporter;
            _logger.LogInformation($"Starting exporting at: {now:s}");
            _savePackageState.Exporter.StartExport(_mainEditorState.LoadedPackage!, destPath, publishPath);
            _savePackageState.Run = true;
        }
    }
}