using CovertActionTools.Core.Importing;

namespace CovertActionTools.App.ViewModels;

public class LoadPackageState : IViewModel
{
    public bool Show { get; set; }
    public bool Run { get; set; }
    public string? SourcePath { get; set; }
    public IPackageImporter? Importer { get; set; }
}