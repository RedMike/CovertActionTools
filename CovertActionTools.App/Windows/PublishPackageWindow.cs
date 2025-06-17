using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Exporting;
using CovertActionTools.Core.Importing;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class PublishPackageWindow : BaseWindow
{
    private readonly ILogger<PublishPackageWindow> _logger;
    private readonly AppLoggingState _appLogging;
    private readonly PublishPackageState _publishPackageState;
    private readonly MainEditorState _mainEditorState;
    private readonly IPackageExporter<ILegacyPublisher> _exporter;
    private readonly FileBrowserState _fileBrowserState;

    public PublishPackageWindow(ILogger<PublishPackageWindow> logger, AppLoggingState appLogging, PublishPackageState publishPackageState, MainEditorState mainEditorState, IPackageExporter<ILegacyPublisher> exporter, FileBrowserState fileBrowserState)
    {
        _logger = logger;
        _appLogging = appLogging;
        _publishPackageState = publishPackageState;
        _mainEditorState = mainEditorState;
        _exporter = exporter;
        _fileBrowserState = fileBrowserState;
    }

    public override void Draw()
    {
        if (!_publishPackageState.Show)
        {
            return;
        }

        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);
        ImGui.Begin("Publish Package", 
            ImGuiWindowFlags.NoResize | 
            ImGuiWindowFlags.NoMove | 
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.Modal);
        
        if (_publishPackageState.Run)
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
        var destPath = _publishPackageState.DestinationPath;
        var exportStatus = _exporter.CheckStatus() ?? new ExportStatus();
        
        ImGui.Text($"Publishing package to folder: {destPath}");

        ImGui.Text("");
        ImGui.Separator();
        ImGui.Text("");

        var windowSize = ImGui.GetContentRegionMax();

        var progress = exportStatus.GetProgress();

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
        if (exportStatus.Done)
        {
            if (exportStatus.Errors.Count == 0)
            {
                _mainEditorState.PackageWasSaved();
                _publishPackageState.CloseDialog();
            }
            else
            {
                if (ImGui.Button("Close"))
                {
                    _publishPackageState.CloseDialog();
                }
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
        var origDestPath = _publishPackageState.DestinationPath ?? "";
        var destinationPath = origDestPath;
        ImGui.InputText("Publish Path", ref destinationPath, 256);
        if (destinationPath != origDestPath)
        {
            _publishPackageState.UpdatePath(destinationPath);
        }
        
        ImGui.SameLine();

        if (ImGui.Button("Browse"))
        {
            _fileBrowserState.CurrentPath = destinationPath + Path.DirectorySeparatorChar;
            _fileBrowserState.CurrentDir = Directory.GetParent(destinationPath)!.FullName;
            _fileBrowserState.FoldersOnly = true;
            _fileBrowserState.NewFolderButton = true;
            _fileBrowserState.Shown = true;
            _fileBrowserState.Callback = (newPath) => _publishPackageState.UpdatePath(newPath);
        }
        
        ImGui.Separator();

        if (ImGui.Button("Cancel"))
        {
            _publishPackageState.CloseDialog();
        }

        ImGui.SameLine();
        if (ImGui.Button("Publish") || _publishPackageState.AutoRun)
        {
            var now = DateTime.Now;
            _logger.LogInformation($"Starting publishing at: {now:s}");
            _exporter.StartExport(_mainEditorState.OriginalLoadedPackage!, destinationPath);
            _publishPackageState.StartRunning();
        }
    }
}