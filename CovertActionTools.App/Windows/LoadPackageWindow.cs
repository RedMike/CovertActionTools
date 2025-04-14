using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Importing;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class LoadPackageWindow : BaseWindow
{
    private readonly ILogger<LoadPackageWindow> _logger;
    private readonly AppLoggingState _appLogging;
    private readonly LoadPackageState _loadPackageState;
    private readonly MainEditorState _mainEditorState;
    private readonly IPackageImporter _importer;

    public LoadPackageWindow(ILogger<LoadPackageWindow> logger, AppLoggingState appLogging, LoadPackageState loadPackageState, MainEditorState mainEditorState, IPackageImporter importer)
    {
        _logger = logger;
        _appLogging = appLogging;
        _loadPackageState = loadPackageState;
        _mainEditorState = mainEditorState;
        _importer = importer;
    }

    public override void Draw()
    {
        if (!_loadPackageState.Show)
        {
            return;
        }

        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);
        ImGui.Begin("Load Package", 
            ImGuiWindowFlags.NoResize | 
            ImGuiWindowFlags.NoMove | 
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.Modal);
        
        if (_loadPackageState.Run)
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
        if (_loadPackageState.Importer == null)
        {
            throw new Exception("Missing importer");
        }
        var sourcePath = _loadPackageState.SourcePath;
        var importStatus = _loadPackageState.Importer.CheckStatus() ?? new ImportStatus();
        
        ImGui.Text($"Loading package folder: {sourcePath}");
        
        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        var windowSize = ImGui.GetContentRegionMax();

        var progress = 0.1f;
        switch (importStatus.Stage)
        {
            case ImportStatus.ImportStage.ReadingIndex:
                progress = 0.1f;
                break;
            case ImportStatus.ImportStage.ProcessingSimpleImages:
                if (importStatus.StageItems <= 0)
                {
                    progress = 0.1f;
                }
                else
                {
                    progress = 0.1f + ((float)importStatus.StageItemsDone / importStatus.StageItems) * 0.1f;
                }
                break;
            case ImportStatus.ImportStage.ProcessingCrimes:
                if (importStatus.StageItems <= 0)
                {
                    progress = 0.2f;
                }
                else
                {
                    progress = 0.2f + ((float)importStatus.StageItemsDone / importStatus.StageItems) * 0.1f;
                }
                break;
            case ImportStatus.ImportStage.ProcessingTexts:
                if (importStatus.StageItems <= 0)
                {
                    progress = 0.3f;
                }
                else
                {
                    progress = 0.3f + ((float)importStatus.StageItemsDone / importStatus.StageItems) * 0.1f;
                }
                break;
            case ImportStatus.ImportStage.ProcessingClues:
                if (importStatus.StageItems <= 0)
                {
                    progress = 0.4f;
                }
                else
                {
                    progress = 0.4f + ((float)importStatus.StageItemsDone / importStatus.StageItems) * 0.1f;
                }
                break;
            case ImportStatus.ImportStage.ProcessingPlots:
                if (importStatus.StageItems <= 0)
                {
                    progress = 0.5f;
                }
                else
                {
                    progress = 0.5f + ((float)importStatus.StageItemsDone / importStatus.StageItems) * 0.1f;
                }
                break;
            case ImportStatus.ImportStage.ProcessingWorlds:
                if (importStatus.StageItems <= 0)
                {
                    progress = 0.6f;
                }
                else
                {
                    progress = 0.6f + ((float)importStatus.StageItemsDone / importStatus.StageItems) * 0.1f;
                }
                break;
            case ImportStatus.ImportStage.ProcessingCatalogs:
                if (importStatus.StageItems <= 0)
                {
                    progress = 0.7f;
                }
                else
                {
                    progress = 0.7f + ((float)importStatus.StageItemsDone / importStatus.StageItems) * 0.1f;
                }
                break;
            case ImportStatus.ImportStage.ImportDone:
                progress = 1.0f;
                break;
        }

        var progressBarSize = new Vector2(windowSize.X - 20.0f, 15.0f);
        ImGui.ProgressBar(progress, progressBarSize);
        
        var text = $"{importStatus.StageMessage} {importStatus.StageItemsDone}/{importStatus.StageItems}";
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
        if (importStatus.Stage == ImportStatus.ImportStage.Unknown || 
            importStatus.Stage == ImportStatus.ImportStage.FatalError ||
            importStatus.Stage == ImportStatus.ImportStage.ImportDone)
        {
            if (importStatus.Errors.Count == 0)
            {
                _mainEditorState.PackageWasLoaded(sourcePath!, _loadPackageState.Importer.GetImportedModel());
                _loadPackageState.Show = false;
                _loadPackageState.Run = false;
                _loadPackageState.Importer = null;
            }
            else
            {
                if (ImGui.Button("Close"))
                {
                    _loadPackageState.Show = false;
                    _loadPackageState.Run = false;
                    _loadPackageState.Importer = null;
                }
            }
        }
        
        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        var cursorPos = ImGui.GetCursorPos();
        var logSize = new Vector2(windowSize.X - cursorPos.X, windowSize.Y - cursorPos.Y);
        ImGui.BeginChild("LoadLogs", logSize, true, ImGuiWindowFlags.ChildWindow);
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
        var origSourcePath = _loadPackageState.SourcePath ?? "";
        var sourcePath = origSourcePath;
        ImGui.InputText("Source Path", ref sourcePath, 256);
        if (sourcePath != origSourcePath)
        {
            _loadPackageState.SourcePath = sourcePath;
        }
        
        ImGui.Separator();

        if (ImGui.Button("Cancel"))
        {
            _loadPackageState.Show = false;
        }

        ImGui.SameLine();
        if (ImGui.Button("Load"))
        {
            var now = DateTime.Now;
            _loadPackageState.Importer = _importer;
            _logger.LogInformation($"Starting importing at: {now:s}");
            _loadPackageState.Importer.StartImport(sourcePath);
            _loadPackageState.Run = true;
        }
    }
}