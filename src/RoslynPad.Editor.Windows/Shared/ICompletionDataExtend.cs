namespace RoslynPad.Editor;

public interface ICompletionDataExtend : ICompletionData
{
    bool IsSelected { get; }

    string SortText { get; }
}
