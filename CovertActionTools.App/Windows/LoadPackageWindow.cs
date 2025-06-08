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
    private readonly IPackageImporter<IImporter> _importer;
    private readonly FileBrowserState _fileBrowserState;

    public LoadPackageWindow(ILogger<LoadPackageWindow> logger, AppLoggingState appLogging, LoadPackageState loadPackageState, MainEditorState mainEditorState, IPackageImporter<IImporter> importer, FileBrowserState fileBrowserState)
    {
        _logger = logger;
        _appLogging = appLogging;
        _loadPackageState = loadPackageState;
        _mainEditorState = mainEditorState;
        _importer = importer;
        _fileBrowserState = fileBrowserState;
    }

    public override void Draw()
    {
        if (!_loadPackageState.Show || _fileBrowserState.Shown)
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

        var progress = importStatus.GetProgress();

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
        if (importStatus.Done)
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
        var origSourcePath = _loadPackageState.SourcePath ?? "";
        var sourcePath = origSourcePath;
        ImGui.InputText("Source Path", ref sourcePath, 256);
        if (sourcePath != origSourcePath)
        {
            _loadPackageState.SourcePath = sourcePath;
        }
        
        ImGui.SameLine();

        if (ImGui.Button("Browse"))
        {
            _fileBrowserState.CurrentPath = sourcePath + Path.DirectorySeparatorChar;
            _fileBrowserState.CurrentDir = Directory.GetParent(sourcePath)!.FullName;
            _fileBrowserState.FoldersOnly = true;
            _fileBrowserState.NewFolderButton = false;
            _fileBrowserState.Shown = true;
            _fileBrowserState.Callback = (newPath) => _loadPackageState.SourcePath = newPath;
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