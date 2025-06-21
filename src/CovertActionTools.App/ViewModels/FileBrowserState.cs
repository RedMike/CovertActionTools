namespace CovertActionTools.App.ViewModels;

public class FileBrowserState : IViewModel
{
    public bool Shown { get; set; }
    public string CurrentPath { get; set; } = string.Empty;
    public string CurrentDir { get; set; } = string.Empty;
    public bool FoldersOnly { get; set; }
    public bool NewFolderButton { get; set; }
    public string NewFolderString { get; set; } = string.Empty;
    public Action<string> Callback { get; set; } = (_) => { };
}