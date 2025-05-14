using CovertActionTools.Core.Models;
using CovertActionTools.Core.Processors;

namespace CovertActionTools.App.ViewModels;

public class AnimationPreviewState : IViewModel
{
    public string SelectedId { get; private set; } = string.Empty;
    
    public int SelectedFrameId { get; set; } = 0;
    public Dictionary<int, (int value, int frameIndex)> InputRegisters { get; set; } = new();
    
    private int _cachedFrameId = -1;
    private Dictionary<int, (int value, int frameIndex)> _cachedInputRegisters = new();
    private AnimationState? _cachedState = null;
    
    public void Reset(string id)
    {
        SelectedId = id;
        SelectedFrameId = 0;
        InputRegisters.Clear();

        _cachedFrameId = -1;
        _cachedInputRegisters.Clear();
        _cachedState = null;
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

    public AnimationState GetState(AnimationModel animation, IAnimationProcessor animationProcessor)
    {
        if (CacheIsValid())
        {
            return _cachedState!;
        }

        _cachedFrameId = SelectedFrameId;
        _cachedInputRegisters = InputRegisters.ToDictionary(x => x.Key, x => x.Value);
        _cachedState = animationProcessor.Process(animation, _cachedFrameId, _cachedInputRegisters);
        return _cachedState;
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