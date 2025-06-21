using CovertActionTools.Core.Models;

namespace CovertActionTools.App.ViewModels;

public abstract class PendingEditorState<TData> : IViewModel
{
    public string Id { get; private set; } = string.Empty;
    public TData? OriginalData { get; private set; }
    public TData? PendingData { get; private set; }
    
    public bool HasChanges { get; private set; }

    public void RecordChange()
    {
        HasChanges = true;
    }
    
    public void Reset(string id, TData data)
    {
        Id = id;
        OriginalData = data;
        PendingData = data;
        HasChanges = false;
    }
}

public class PendingEditorSimpleImageState : PendingEditorState<SimpleImageModel> { }
public class PendingEditorCatalogState : PendingEditorState<CatalogModel> { }
public class PendingEditorClueState : PendingEditorState<Dictionary<string, ClueModel>> { }
public class PendingEditorPlotState : PendingEditorState<Dictionary<string, PlotModel>> { }
public class PendingEditorAnimationState : PendingEditorState<AnimationModel> { }
public class PendingEditorFontState : PendingEditorState<FontsModel> { }
public class PendingEditorProseState : PendingEditorState<Dictionary<string, ProseModel>> { }
public class PendingEditorTextState : PendingEditorState<Dictionary<string, TextModel>> { }
public class PendingEditorWorldState : PendingEditorState<WorldModel> { }
public class PendingEditorCrimeState : PendingEditorState<CrimeModel> { }