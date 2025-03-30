using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Exporting;
using CovertActionTools.Core.Services;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SavePackageWindow : BaseWindow
{
    private readonly ILogger<SavePackageWindow> _logger;
    private readonly AppLoggingState _appLogging;
    private readonly SavePackageState _savePackageState;
    private readonly IExporterFactory _exporterFactory;
    private readonly MainEditorState _mainEditorState;

    public SavePackageWindow(ILogger<SavePackageWindow> logger, AppLoggingState appLogging, SavePackageState savePackageState, IExporterFactory exporterFactory, MainEditorState mainEditorState)
    {
        _logger = logger;
        _appLogging = appLogging;
        _savePackageState = savePackageState;
        _exporterFactory = exporterFactory;
        _mainEditorState = mainEditorState;
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
        var exportStatus = _savePackageState.Exporter.CheckStatus() ?? new ExportStatus();
        
        ImGui.Text($"Saving package to folder: {destPath}");
        
        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        var windowSize = ImGui.GetContentRegionMax();

        var progress = 0.1f;
        switch (exportStatus.Stage)
        {
            case ExportStatus.ExportStage.Preparing:
                progress = 0.15f;
                break;
            case ExportStatus.ExportStage.ProcessingSimpleImages:
                if (exportStatus.StageItems <= 0)
                {
                    progress = 0.2f;
                }
                else
                {
                    progress = 0.15f + ((float)exportStatus.StageItemsDone / exportStatus.StageItems) * 0.2f;
                }
                break;
            //0.35f
            //TODO: other stages
            case ExportStatus.ExportStage.ExportDone:
                progress = 1.0f;
                break;
        }

        var progressBarSize = new Vector2(windowSize.X - 20.0f, 15.0f);
        ImGui.ProgressBar(progress, progressBarSize);
        
        var text = $"{exportStatus.StageMessage}";
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
            _logger.LogInformation($"Done exporting at: {now:s}");
            if (exportStatus.Errors.Count == 0)
            {
                _savePackageState.Show = false;
                _savePackageState.Run = false;
                _savePackageState.Exporter = null;
            }
            else
            {
                _logger.LogError($"Review errors above.");
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
        ImGui.InputText("Source Path", ref destPath, 256);
        if (destPath != origDestPath)
        {
            _savePackageState.DestinationPath = destPath;
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
            _savePackageState.Exporter = _exporterFactory.Create();
            _logger.LogInformation($"Starting exporting at: {now:s}");
            _savePackageState.Exporter.StartExport(_mainEditorState.LoadedPackage!, destPath);
            _savePackageState.Run = true;
        }
    }
}