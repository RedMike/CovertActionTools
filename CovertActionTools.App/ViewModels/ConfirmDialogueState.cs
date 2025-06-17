namespace CovertActionTools.App.ViewModels;

public class ConfirmDialogueState : IViewModel
{
    public bool Show { get; private set; }
    public List<string> Texts { get; private set; } = new();
    public Action<bool> Callback { get; private set; } = (_) => { };

    public void ShowDialog(List<string> texts, Action<bool> cb)
    {
        if (Show)
        {
            throw new Exception("Dialog already being shown");
        }

        Texts = texts.ToList();
        Callback = cb;
    }

    public void CloseDialog()
    {
        Show = false;
        Texts = new();
        Callback = (_) => { };
    }
}