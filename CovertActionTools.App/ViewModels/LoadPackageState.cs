namespace CovertActionTools.App.ViewModels;

public class LoadPackageState : IViewModel
{
    public bool Show { get; private set; }
    public bool AutoRun { get; private set; }
    public bool Run { get; private set; }
    public string? SourcePath { get; private set; }

    public void ShowDialog(string path, bool autorun)
    {
        if (Show)
        {
            throw new Exception("Dialog already being shown");
        }
        SourcePath = path;
        Show = true;
        AutoRun = autorun;
        Run = false;
    }

    public void UpdatePath(string path)
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

    public void StartRunning()
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
        AutoRun = false;
    }

    public void CloseDialog()
    {
        SourcePath = null;
        Show = false;
        AutoRun = false;
        Run = false;
    }
}