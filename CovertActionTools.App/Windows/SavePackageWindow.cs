using System.Numerics;
using CovertActionTools.App.ViewModels;
using CovertActionTools.Core.Exporting;
using CovertActionTools.Core.Importing;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SavePackageWindow : BaseWindow
{
    private readonly ILogger<SavePackageWindow> _logger;
    private readonly AppLoggingState _appLogging;
    private readonly SavePackageState _savePackageState;
    private readonly MainEditorState _mainEditorState;
    private readonly IPackageExporter<IExporter> _exporter;
    private readonly FileBrowserState _fileBrowserState;

    public SavePackageWindow(ILogger<SavePackageWindow> logger, AppLoggingState appLogging, SavePackageState savePackageState, MainEditorState mainEditorState, IPackageExporter<IExporter> exporter, FileBrowserState fileBrowserState)
    {
        _logger = logger;
        _appLogging = appLogging;
        _savePackageState = savePackageState;
        _mainEditorState = mainEditorState;
        _exporter = exporter;
        _fileBrowserState = fileBrowserState;
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
        var destPath = _savePackageState.DestinationPath;
        var exportStatus = _exporter.CheckStatus() ?? new ExportStatus();
        
        ImGui.Text($"Saving package to folder: {destPath}");

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
                _savePackageState.CloseDialog();
            }
            else
            {
                if (ImGui.Button("Close"))
                {
                    _savePackageState.CloseDialog();
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
        var origDestPath = _savePackageState.DestinationPath ?? "";
        var destinationPath = origDestPath;
        ImGui.InputText("Package Path", ref destinationPath, 256);
        if (destinationPath != origDestPath)
        {
            _savePackageState.UpdatePath(destinationPath);
        }
        
        ImGui.SameLine();

        if (ImGui.Button("Browse"))
        {
            _fileBrowserState.CurrentPath = destinationPath + Path.DirectorySeparatorChar;
            _fileBrowserState.CurrentDir = Directory.GetParent(destinationPath)!.FullName;
            _fileBrowserState.FoldersOnly = true;
            _fileBrowserState.NewFolderButton = true;
            _fileBrowserState.Shown = true;
            _fileBrowserState.Callback = (newPath) => _savePackageState.UpdatePath(newPath);
        }
        
        ImGui.Separator();

        if (ImGui.Button("Cancel"))
        {
            _savePackageState.CloseDialog();
        }

        ImGui.SameLine();
        if (ImGui.Button("Save") || _savePackageState.AutoRun)
        {
            var now = DateTime.Now;
            _logger.LogInformation($"Starting exporting at: {now:s}");
            _exporter.StartExport(_mainEditorState.LoadedPackage!, destinationPath);
            _savePackageState.StartRunning();
        }
    }
}