namespace RoslynPad.Editor;

public sealed class CompletionResult(IList<ICompletionDataExtend>? completionData, IOverloadProviderExtend? overloadProvider, bool useHardSelection)
{
    public bool UseHardSelection { get; } = useHardSelection;

    public IList<ICompletionDataExtend>? CompletionData { get; } = completionData;

    public IOverloadProviderExtend? OverloadProvider { get; } = overloadProvider;
}