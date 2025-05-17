using CovertActionTools.Core.Models;

namespace CovertActionTools.App.ViewModels;

/// <summary>
/// High level things like 'is a package loaded' and 'the folder that is open'
/// </summary>
public class MainEditorState : IViewModel
{
    public enum ItemType
    {
        Unknown = -1,
        SimpleImage = 0,
        Crime = 1,
        Text = 2,
        Clue = 3,
        Plot = 4,
        World = 5,
        CatalogImage = 6,
        Animation = 7,
        Font = 8,
    }
    public string? DefaultPublishPath { get; set; }
    public string? DefaultRunPath { get; set; }
    public bool Running { get; set; }
    public string? LoadedPackagePath { get; private set; }
    public PackageModel? OriginalLoadedPackage { get; private set; }
    public PackageModel? LoadedPackage { get; private set; }
    public bool IsPackageLoaded => !string.IsNullOrEmpty(LoadedPackagePath);
    public (ItemType type, string id)? SelectedItem { get; set; }

    public void PackageWasLoaded(string path, PackageModel model)
    {
        LoadedPackagePath = path;
        LoadedPackage = model;
        OriginalLoadedPackage = model.Clone();
    }
    
    private DateTime _lastPackageModificationCheck = DateTime.MinValue;
    private bool _packageModificationResult = false;

    public bool IsPackageModified()
    {
        if (LoadedPackage == null || OriginalLoadedPackage == null)
        {
            return false;
        }
        var now = DateTime.UtcNow;
        if ((now - _lastPackageModificationCheck).TotalSeconds <= 1.0f)
        {
            return _packageModificationResult;
        }
        _packageModificationResult = LoadedPackage!.IsModified(OriginalLoadedPackage!);
        _lastPackageModificationCheck = now;
        return _packageModificationResult;
    }
}