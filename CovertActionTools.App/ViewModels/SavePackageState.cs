using CovertActionTools.Core.Exporting;

namespace CovertActionTools.App.ViewModels;

public class SavePackageState : IViewModel
{
    public bool Show { get; set; }
    public bool Run { get; set; }
    /// <summary>
    /// Where to save source files for the package
    /// </summary>
    public string? DestinationPath { get; set; }
    /// <summary>
    /// Where to export compiled files for the package
    /// </summary>
    public string? PublishPath { get; set; }
    public IPackageExporter? Exporter { get; set; }
}