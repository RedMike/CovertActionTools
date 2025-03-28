using CovertActionTools.Core.Importing;

namespace CovertActionTools.App.ViewModels;

public class ParsePublishedState : IViewModel
{
    public bool Show { get; set; }
    public bool Run { get; set; }
    public string? SourcePath { get; set; }
    public string? DestinationPath { get; set; }
    public IImporter? Importer { get; set; }
}