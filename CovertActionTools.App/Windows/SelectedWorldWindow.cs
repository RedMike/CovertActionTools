using CovertActionTools.App.ViewModels;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.App.Windows;

public class SelectedWorldWindow : BaseWindow
{
    private readonly ILogger<SelectedWorldWindow> _logger;
    private readonly MainEditorState _mainEditorState;
    private readonly RenderWindow _renderWindow;

    public SelectedWorldWindow(ILogger<SelectedWorldWindow> logger, MainEditorState mainEditorState, RenderWindow renderWindow)
    {
        _logger = logger;
        _mainEditorState = mainEditorState;
        _renderWindow = renderWindow;
    }

    public override void Draw()
    {
        if (!_mainEditorState.IsPackageLoaded)
        {
            return;
        }

        if (_mainEditorState.SelectedItem == null ||
            _mainEditorState.SelectedItem.Value.type != MainEditorState.ItemType.Crime)
        {
            return;
        }

        var key = int.Parse(_mainEditorState.SelectedItem.Value.id);
        
        
    }
}