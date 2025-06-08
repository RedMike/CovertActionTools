using System.Numerics;
using CovertActionTools.App.ViewModels;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class FileBrowserWindow : BaseWindow
{
    private readonly ILogger<FileBrowserWindow> _logger;
    private readonly FileBrowserState _state;

    public FileBrowserWindow(ILogger<FileBrowserWindow> logger, FileBrowserState state)
    {
        _logger = logger;
        _state = state;
    }

    public override void Draw()
    {
        if (!_state.Shown)
        {
            return;
        }

        var initialPos = new Vector2(50.0f, 50.0f);
        var initialSize = new Vector2(800.0f, 400.0f);
        ImGui.SetNextWindowSize(initialSize, ImGuiCond.Appearing);
        ImGui.SetNextWindowPos(initialPos, ImGuiCond.Appearing);
        ImGui.Begin("File Browser", ImGuiWindowFlags.Popup);
        DrawWindow();
        ImGui.End();
    }

    private void DrawWindow()
    {
        var windowSize = ImGui.GetContentRegionAvail();
        var cursorPos = ImGui.GetCursorPos();
        ImGui.BeginChild("Tree", new Vector2(2.0f * windowSize.X / 5.0f, windowSize.Y - 40.0f), true);

        var currentPath = _state.CurrentPath;
        try
        {
            var rootDrives = Directory.GetLogicalDrives();
            foreach (var rootDrive in rootDrives)
            {
                var flags = ImGuiTreeNodeFlags.SpanAvailWidth |
                            ImGuiTreeNodeFlags.OpenOnDoubleClick |
                            ImGuiTreeNodeFlags.OpenOnArrow;
                if (currentPath.StartsWith(rootDrive))
                {
                    flags |= ImGuiTreeNodeFlags.DefaultOpen;
                }
                if (ImGui.TreeNodeEx(rootDrive, flags))
                {
                    DrawTreeChildren(currentPath, rootDrive);
                    
                    ImGui.TreePop();
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to handle path: {_state.CurrentPath}");
        }
        
        ImGui.EndChild();

        ImGui.SetCursorPos(cursorPos + new Vector2(2.0f * windowSize.X / 5.0f, 0.0f));
        ImGui.BeginChild("Files", new Vector2(3.0f * windowSize.X / 5.0f, windowSize.Y - 40.0f), true);

        var currentDir = _state.CurrentDir;
        try
        {
            List<string> items;
            if (_state.FoldersOnly)
            {
                items = Directory.EnumerateDirectories(currentDir, "*", SearchOption.TopDirectoryOnly)
                    .ToList();
            }
            else
            {
                items = Directory.EnumerateFileSystemEntries(currentDir, "*", SearchOption.TopDirectoryOnly)
                    .ToList();
            }

            foreach (var item in items
                         .OrderBy(x => Directory.Exists(x) ? 0 : 1)
                         .ThenBy(x => x))
            {
                var flags = ImGuiTreeNodeFlags.Leaf |
                           ImGuiTreeNodeFlags.SpanAvailWidth |
                           ImGuiTreeNodeFlags.None;
                if (currentPath == (_state.FoldersOnly ? (item + Path.DirectorySeparatorChar) : item))
                {
                    flags |= ImGuiTreeNodeFlags.Selected;
                }

                var itemName = Path.GetFileName(item);
                if (ImGui.TreeNodeEx(itemName, flags))
                {
                    if (ImGui.IsItemClicked())
                    {
                        if (_state.FoldersOnly)
                        {
                            _state.CurrentDir = Directory.GetParent(item)!.FullName;
                            _state.CurrentPath = item + Path.DirectorySeparatorChar;
                        }
                        else
                        {
                            if (Directory.Exists(item))
                            {
                                _state.CurrentDir = Directory.GetParent(item)!.FullName;
                                _state.CurrentPath = item + Path.DirectorySeparatorChar;
                            }
                            else
                            {
                                
                                _state.CurrentDir = Directory.GetParent(item)!.FullName;
                                _state.CurrentPath = item;
                            }
                        }
                    }
                    ImGui.TreePop();
                }
            }

            if (_state.NewFolderButton)
            {
                var areaSize = ImGui.GetContentRegionAvail();
                ImGui.SetNextItemWidth(areaSize.X - 100.0f);
                var str = _state.NewFolderString;
                ImGui.InputText("New Folder", ref str, 128);
                if (str != _state.NewFolderString)
                {
                    _state.NewFolderString = str;
                }

                ImGui.SameLine();

                ImGui.SetNextItemWidth(100.0f);
                if (ImGui.Button("Create"))
                {
                    try
                    {
                        var newFolderPath = Path.Combine(_state.CurrentDir, _state.NewFolderString);
                        Directory.CreateDirectory(newFolderPath);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to create folder");
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to handle path: {_state.CurrentPath}");
        }
        
        ImGui.EndChild();
        
        
        ImGui.SetCursorPos(cursorPos + new Vector2(0.0f, windowSize.Y - 40.0f));
        ImGui.BeginChild("PathContainer", new Vector2(windowSize.X, 40.0f), true);
        var path = _state.CurrentPath;
        ImGui.SetNextItemWidth(windowSize.X - 100.0f);
        ImGui.InputText("Path", ref path, 1024);
        if (path != _state.CurrentPath)
        {
            if (_state.FoldersOnly)
            {
                _state.CurrentDir = Directory.GetParent(path)!.FullName;
                _state.CurrentPath = path + Path.DirectorySeparatorChar;
            }
            else
            {
                _state.CurrentDir = Directory.GetParent(path)!.FullName;
                _state.CurrentPath = path;
            }
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100.0f);
        if (ImGui.Button("Select"))
        {
            var finalPath = _state.CurrentPath;
            if (_state.FoldersOnly)
            {
                finalPath = finalPath.TrimEnd(Path.DirectorySeparatorChar);
            }

            _state.Shown = false;
            _state.Callback(finalPath);
        }
        ImGui.EndChild();
        
    }

    private void DrawTreeChildren(string currentPath, string path)
    {
        try
        {
            foreach (var dirPath in Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly))
            {
                var flags = ImGuiTreeNodeFlags.SpanAvailWidth |
                            ImGuiTreeNodeFlags.OpenOnDoubleClick |
                            ImGuiTreeNodeFlags.OpenOnArrow;
                if (currentPath.StartsWith(dirPath + Path.DirectorySeparatorChar))
                {
                    flags |= ImGuiTreeNodeFlags.DefaultOpen;
                }

                var leaf = false;
                try
                {
                    var subDirs = Directory.EnumerateDirectories(dirPath, "*", SearchOption.TopDirectoryOnly)
                        .ToList();
                    if (subDirs.Count == 0)
                    {
                        if (currentPath.StartsWith(dirPath + Path.DirectorySeparatorChar))
                        {
                            flags |= ImGuiTreeNodeFlags.Selected;
                        }
                        flags |= ImGuiTreeNodeFlags.Leaf;
                        leaf = true;
                    }
                }
                catch (Exception)
                {
                    //exception means permission issues/etc
                }

                var dirName = Path.GetFileName(dirPath);

                if (ImGui.TreeNodeEx(dirName, flags))
                {
                    if (ImGui.IsItemClicked())
                    {
                        if (_state.FoldersOnly)
                        {
                            _state.CurrentDir = Directory.GetParent(dirPath)!.FullName;
                            _state.CurrentPath = dirPath + Path.DirectorySeparatorChar;
                        }
                        else
                        {
                            _state.CurrentDir = dirPath;
                        }
                    }
                    if (!leaf)
                    {
                        DrawTreeChildren(currentPath, dirPath);
                    }
                    
                    ImGui.TreePop();
                }
            }
        }
        catch (Exception)
        {
            //exception means permission issues/etc
        }
    }
}