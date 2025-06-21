using CovertActionTools.Core.Models;
using CovertActionTools.Core.Processors;

namespace CovertActionTools.App.ViewModels;

public class AnimationPreviewState : IViewModel
{
    public string SelectedId { get; private set; } = string.Empty;
    
    public int SelectedFrameId { get; set; } = 0;
    public Dictionary<int, (int value, int frameIndex)> InputRegisters { get; set; } = new();
    public string PreviousAnimationId { get; set; } = string.Empty;
    
    public bool LimitToGameWindow { get; set; }
    
    private int _cachedFrameId = -1;
    private Dictionary<int, (int value, int frameIndex)> _cachedInputRegisters = new();
    private AnimationState? _cachedState = null;
    private AnimationState? _cachedPreviousAnimationState = null;
    private string _cachedPreviousAnimationId = string.Empty;
    
    public void Reset(string id)
    {
        SelectedId = id;
        SelectedFrameId = 0;
        InputRegisters.Clear();

        _cachedFrameId = -1;
        _cachedInputRegisters.Clear();
        _cachedState = null;
        _cachedPreviousAnimationState = null;

        PreviousAnimationId = string.Empty;
        LimitToGameWindow = false;
    }

    public void SetInputRegister(int registerId, int value, int frameIndex)
    {
        if (value == 0 && frameIndex == 0)
        {
            InputRegisters.Remove(registerId);
            return;
        }

        InputRegisters[registerId] = (value, frameIndex);
    }

    public (AnimationState current, AnimationState? previous) GetState(AnimationModel animation, AnimationModel? previousAnimation, IAnimationProcessor animationProcessor)
    {
        if (CacheIsValid())
        {
            return (_cachedState!, _cachedPreviousAnimationState);
        }

        _cachedFrameId = SelectedFrameId;
        _cachedInputRegisters = InputRegisters.ToDictionary(x => x.Key, x => x.Value);
        _cachedState = animationProcessor.Process(animation, _cachedFrameId, _cachedInputRegisters);

        if (!string.IsNullOrEmpty(PreviousAnimationId) && previousAnimation != null)
        {
            _cachedPreviousAnimationState = animationProcessor.Process(previousAnimation, 1000, new());
        }
        
        return (_cachedState, _cachedPreviousAnimationState);
    }

    private bool CacheIsValid()
    {
        if (_cachedState == null)
        {
            return false;
        }
        
        if (SelectedFrameId != _cachedFrameId)
        {
            return false;
        }

        if (InputRegisters.Count != _cachedInputRegisters.Count)
        {
            return false;
        }

        if (PreviousAnimationId != _cachedPreviousAnimationId)
        {
            return false;
        }

        foreach (var pair in InputRegisters)
        {
            if (!_cachedInputRegisters.TryGetValue(pair.Key, out var value))
            {
                return false;
            }
            
            if (pair.Value.value != value.value || pair.Value.frameIndex != value.frameIndex)
            {
                return false;
            }
        }

        return true;
    }
}