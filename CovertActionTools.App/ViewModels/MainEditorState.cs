using CovertActionTools.Core.Models;

namespace CovertActionTools.App.ViewModels;

/// <summary>
/// High level things like 'is a package loaded' and 'the folder that is open'
/// </summary>
public class MainEditorState : IViewModel
{
    public string? LoadedPackagePath { get; set; }
    public PackageModel? LoadedPackage { get; set; }
    public bool IsPackageLoaded => !string.IsNullOrEmpty(LoadedPackagePath);
}