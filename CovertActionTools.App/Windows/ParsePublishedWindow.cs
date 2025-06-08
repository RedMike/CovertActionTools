using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Exporting;
using CovertActionTools.Core.Importing;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class ParsePublishedWindow : BaseWindow
{
    private readonly ILogger<ParsePublishedWindow> _logger;
    private readonly AppLoggingState _appLogging;
    private readonly ParsePublishedState _parsePublishedState;
    private readonly IPackageImporter<ILegacyParser> _importer;
    private readonly IPackageExporter _exporter;

    public ParsePublishedWindow(ILogger<ParsePublishedWindow> logger, AppLoggingState appLogging, ParsePublishedState parsePublishedState, IPackageImporter<ILegacyParser> importer, IPackageExporter<IExporter> exporter)
    {
        _logger = logger;
        _appLogging = appLogging;
        _parsePublishedState = parsePublishedState;
        _importer = importer;
        _exporter = exporter;
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
        if (_parsePublishedState.Exporter == null)
        {
            throw new Exception("Missing exporter");
        }
        var sourcePath = _parsePublishedState.SourcePath;
        var destinationPath = _parsePublishedState.DestinationPath;
        var importStatus = _parsePublishedState.Importer.CheckStatus() ?? new ImportStatus();
        var exportStatus = _parsePublishedState.Exporter.CheckStatus() ?? new ExportStatus();
        
        ImGui.Text($"Parsing published folder: {sourcePath}");
        ImGui.Text($"Saving into package: {destinationPath}");
        
        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        var windowSize = ImGui.GetContentRegionMax();

        var progress = importStatus.GetProgress();

        var text = $"{importStatus.StageMessage} {importStatus.StageItemsDone}/{importStatus.StageItems}";
        if (importStatus.Done && _parsePublishedState.Export)
        {
            text = $"{exportStatus.StageMessage} {exportStatus.StageItemsDone}/{exportStatus.StageItems}";
            progress = exportStatus.GetProgress();
        }
        var progressBarSize = new Vector2(windowSize.X - 20.0f, 15.0f);
        ImGui.ProgressBar(progress, progressBarSize);
        
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
        if (!_parsePublishedState.Export && importStatus.Done)
        {
            if (ImGui.Button("Save"))
            {
                var now = DateTime.Now;
                _logger.LogInformation($"Starting exporting at: {now:s}");
                _parsePublishedState.Export = true;
                _parsePublishedState.Exporter.StartExport(_parsePublishedState.Importer.GetImportedModel(), destinationPath ?? string.Empty);
            }
        }

        if (_parsePublishedState.Export && exportStatus.Done)
        {
            if (ImGui.Button("Close"))
            {
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
            _parsePublishedState.Importer = _importer;
            _parsePublishedState.Exporter = _exporter;
            _logger.LogInformation($"Starting importing at: {now:s}");
            _parsePublishedState.Importer.StartImport(sourcePath);
            _parsePublishedState.Run = true;
            _parsePublishedState.Export = false;
        }
    }
}