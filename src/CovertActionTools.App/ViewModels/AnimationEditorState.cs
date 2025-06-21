using CovertActionTools.Core.Models;

namespace CovertActionTools.App.ViewModels;

public class AnimationEditorState : IViewModel
{
    public string SelectedId { get; private set; } = string.Empty;

    public string SerialisedInstructions = string.Empty; //can't be a property or else ImGui won't work well
    public string SerialisedSteps = String.Empty; //can't be a property or else ImGui won't work well
    
    private string _originalInstructions = string.Empty;
    private string _originalSteps = string.Empty;
    private bool _loaded = false;

    public bool HasChanges()
    {
        if (SerialisedInstructions != _originalInstructions)
        {
            return true;
        }

        return false;
    }
    
    public void Reset(string id)
    {
        SelectedId = id;

        SerialisedInstructions = string.Empty;
        SerialisedSteps = string.Empty;
        
        _originalInstructions = string.Empty;
        _originalSteps = string.Empty;
        _loaded = false;
    }

    public void Update(AnimationModel animation)
    {
        if (_loaded)
        {
            return;
        }
        
        _originalInstructions = animation.Control.GetSerialisedInstructions();
        SerialisedInstructions = _originalInstructions;

        _originalSteps = animation.Control.GetSerialisedSteps();
        SerialisedSteps = _originalSteps;

        _loaded = true;
    }
}