﻿using CovertActionTools.Core.Models;

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
        Prose = 9,
        
        Package = 100,
    }
    public string? LoadedPackagePath { get; private set; }
    public PackageModel? OriginalLoadedPackage { get; private set; }
    public PackageModel? LoadedPackage { get; private set; }
    public bool HasChanges { get; private set; }
    public bool IsPackageLoaded => !string.IsNullOrEmpty(LoadedPackagePath);
    public (ItemType type, string id)? SelectedItem { get; set; }

    public void PackageWasLoaded(string path, PackageModel model)
    {
        LoadedPackagePath = path;
        LoadedPackage = model;
        OriginalLoadedPackage = model.Clone();
        HasChanges = false;
    }

    public void PackageWasSaved()
    {
        HasChanges = false;
        OriginalLoadedPackage = LoadedPackage!.Clone();
    }

    public void RecordChange()
    {
        HasChanges = true;
    }

    public void UnloadPackage()
    {
        LoadedPackagePath = null;
        LoadedPackage = null;
        OriginalLoadedPackage = null;
        HasChanges = false;
        SelectedItem = null;
    }
}