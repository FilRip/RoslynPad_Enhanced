namespace RoslynPad.UI;

public interface IOpenFileDialog
{
    bool AllowMultiple { get; set; }

    void SetFilter(FileDialogFilter filter);

    string InitialDirectory { get; set; }

    string FileName { get; set; }

    Task<string[]?> ShowAsync();
}
