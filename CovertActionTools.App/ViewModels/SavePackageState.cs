using CovertActionTools.Core.Exporting;

namespace CovertActionTools.App.ViewModels;

public class SavePackageState : IViewModel
{
    public bool Show { get; set; }
    public bool Run { get; set; }
    public string? DestinationPath { get; set; }
    public IPackageExporter? Exporter { get; set; }
}