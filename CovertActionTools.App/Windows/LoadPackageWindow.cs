using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Services;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class LoadPackageWindow : BaseWindow
{
    private readonly ILogger<LoadPackageWindow> _logger;
    private readonly AppLoggingState _appLogging;
    private readonly LoadPackageState _loadPackageState;
    private readonly IImporterFactory _importerFactory;
    private readonly MainEditorState _mainEditorState;

    public LoadPackageWindow(ILogger<LoadPackageWindow> logger, AppLoggingState appLogging, LoadPackageState loadPackageState, IImporterFactory importerFactory, MainEditorState mainEditorState)
    {
        _logger = logger;
        _appLogging = appLogging;
        _loadPackageState = loadPackageState;
        _importerFactory = importerFactory;
        _mainEditorState = mainEditorState;
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
                progress = 0.15f;
                break;
            case ImportStatus.ImportStage.ProcessingSimpleImages:
                if (importStatus.StageItems <= 0)
                {
                    progress = 0.2f;
                }
                else
                {
                    progress = 0.15f + ((float)importStatus.StageItemsDone / importStatus.StageItems) * 0.2f;
                }
                break;
            //0.35f
            //TODO: other stages
            case ImportStatus.ImportStage.ImportDone:
                progress = 1.0f;
                break;
        }

        var progressBarSize = new Vector2(windowSize.X - 20.0f, 15.0f);
        ImGui.ProgressBar(progress, progressBarSize);
        
        var text = $"{importStatus.StageMessage}";
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
            _loadPackageState.Importer = _importerFactory.Create(false);
            _logger.LogInformation($"Starting importing at: {now:s}");
            _loadPackageState.Importer.StartImport(sourcePath);
            _loadPackageState.Run = true;
        }
    }
}