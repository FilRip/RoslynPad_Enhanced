using System.Composition;
using System.Windows;

using Microsoft.Win32;

using RoslynPad.UI;

namespace RoslynPad;

[Export(typeof(IOpenFileDialog))]
internal sealed class OpenFileDialogAdapter : IOpenFileDialog
{
    private readonly OpenFileDialog _dialog;

    public OpenFileDialogAdapter()
    {
        _dialog = new OpenFileDialog();
    }

    public bool AllowMultiple
    {
        get => _dialog.Multiselect;
        set => _dialog.Multiselect = value;
    }

    public void SetFilter(FileDialogFilter filter)
    {
        _dialog.Filter = filter + string.Empty;
    }

    public string InitialDirectory
    {
        get => _dialog.InitialDirectory;
        set => _dialog.InitialDirectory = value;
    }

    public string FileName
    {
        get => _dialog.FileName;
        set => _dialog.FileName = value;
    }

    public Task<string[]?> ShowAsync()
    {
        if (_dialog.ShowDialog(Application.Current.MainWindow) == true)
        {
            return Task.FromResult<string[]?>(_dialog.FileNames);
        }

        return Task.FromResult<string[]?>(null);
    }
}