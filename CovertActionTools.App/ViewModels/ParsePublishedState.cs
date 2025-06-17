namespace CovertActionTools.App.ViewModels;

public class ParsePublishedState : IViewModel
{
    public bool Show { get; private set; }
    public bool Run { get; private set; }
    public bool Export { get; private set; }
    public string? SourcePath { get; private set; }
    public string? DestinationPath { get; private set; }
    
    public void ShowDialog(string sourcePath, string destinationPath)
    {
        if (Show)
        {
            throw new Exception("Dialog already being shown");
        }
        SourcePath = sourcePath;
        DestinationPath = destinationPath;
        Show = true;
        Run = false;
        Export = false;
    }
    
    public void UpdateSourcePath(string path)
    {
        if (!Show)
        {
            throw new Exception("Dialog not being shown");
        }

        if (Run)
        {
            throw new Exception("Process already running");
        }

        SourcePath = path;
    }
    
    public void UpdateDestinationPath(string path)
    {
        if (!Show)
        {
            throw new Exception("Dialog not being shown");
        }

        if (Run)
        {
            throw new Exception("Process already running");
        }

        DestinationPath = path;
    }
    
    public void StartLoad()
    {
        if (!Show)
        {
            throw new Exception("Dialog not being shown");
        }

        if (Run)
        {
            throw new Exception("Process already running");
        }

        Run = true;
        Export = false;
    }

    public void StartExport()
    {
        if (!Show)
        {
            throw new Exception("Dialog not being shown");
        }

        if (!Run)
        {
            throw new Exception("Process not running");
        }

        if (Export)
        {
            throw new Exception("Export already running");
        }

        Export = true;
    }

    public void CloseDialog()
    {
        SourcePath = string.Empty;
        DestinationPath = string.Empty;
        Show = false;
        Run = false;
        Export = false;
    }
}