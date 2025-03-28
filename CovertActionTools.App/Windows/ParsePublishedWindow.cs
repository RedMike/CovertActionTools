using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Services;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class ParsePublishedWindow : BaseWindow
{
    private readonly ILogger<ParsePublishedWindow> _logger;
    private readonly AppLoggingState _appLogging;
    private readonly ParsePublishedState _parsePublishedState;
    private readonly IImporterFactory _importerFactory;

    public ParsePublishedWindow(ILogger<ParsePublishedWindow> logger, AppLoggingState appLogging, ParsePublishedState parsePublishedState, IImporterFactory importerFactory)
    {
        _logger = logger;
        _appLogging = appLogging;
        _parsePublishedState = parsePublishedState;
        _importerFactory = importerFactory;
    }

    public override void Draw()
    {
        if (!_parsePublishedState.Show)
        {
            return;
        }

        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);
        ImGui.Begin("Parse Published Folder", 
            ImGuiWindowFlags.NoResize | 
            ImGuiWindowFlags.NoMove | 
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.Modal);
        
        if (_parsePublishedState.Run)
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
        if (_parsePublishedState.Importer == null)
        {
            throw new Exception("Missing importer");
        }
        var sourcePath = _parsePublishedState.SourcePath;
        var destinationPath = _parsePublishedState.DestinationPath;
        var status = _parsePublishedState.Importer.CheckStatus() ?? new ImportStatus();
        
        ImGui.Text($"Parsing published folder: {sourcePath}");
        ImGui.Text($"Saving into package: {destinationPath}");
        
        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        var windowSize = ImGui.GetContentRegionMax();

        var progress = 0.1f;
        switch (status.Stage)
        {
            case ImportStatus.ImportStage.ReadingIndex:
                progress = 0.15f;
                break;
            case ImportStatus.ImportStage.ProcessingSimpleImages:
                if (status.StageItems <= 0)
                {
                    progress = 0.2f;
                }
                else
                {
                    progress = 0.15f + ((float)status.StageItemsDone / status.StageItems) * 0.2f;
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
        
        var text = $"{status.StageMessage}";
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
        if (status.Stage == ImportStatus.ImportStage.Unknown || status.Stage == ImportStatus.ImportStage.FatalError ||
            status.Stage == ImportStatus.ImportStage.ImportDone)
        {
            if (ImGui.Button("Save & Close"))
            {
                //TODO: trigger save
                _parsePublishedState.Run = false;
                _parsePublishedState.Show = false;
            }
        }
        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        var cursorPos = ImGui.GetCursorPos();
        var logSize = new Vector2(windowSize.X - cursorPos.X, windowSize.Y - cursorPos.Y);
        ImGui.BeginChild("PublishLogs", logSize, true, ImGuiWindowFlags.ChildWindow);
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
        var origSourcePath = _parsePublishedState.SourcePath ?? "";
        var sourcePath = origSourcePath;
        ImGui.InputText("Source Path", ref sourcePath, 256);
        if (sourcePath != origSourcePath)
        {
            _parsePublishedState.SourcePath = sourcePath;
        }
        
        //TODO: better file explorer?
        var origDestinationPath = _parsePublishedState.DestinationPath ?? "";
        var destinationPath = origDestinationPath;
        ImGui.InputText("Destination Path", ref destinationPath, 256);
        if (destinationPath != origDestinationPath)
        {
            _parsePublishedState.DestinationPath = destinationPath;
        }
        
        ImGui.Separator();

        if (ImGui.Button("Cancel"))
        {
            _parsePublishedState.Show = false;
        }

        ImGui.SameLine();
        if (ImGui.Button("Load"))
        {
            var now = DateTime.Now;
            _parsePublishedState.Importer = _importerFactory.Create();
            _appLogging.Clear(); //TODO: filter to publishing things
            _logger.LogInformation($"Starting publishing at: {now:s}");
            _parsePublishedState.Importer.StartImport(destinationPath);
            _parsePublishedState.Run = true;
        }
    }
}