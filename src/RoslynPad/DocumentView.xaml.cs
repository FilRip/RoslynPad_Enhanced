using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using Avalon.Windows.Controls;

using ICSharpCode.AvalonEdit.Document;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using RoslynPad.Build;
using RoslynPad.Editor;
using RoslynPad.Themes;
using RoslynPad.UI;

namespace RoslynPad;

public partial class DocumentView : IDisposable
{
    private readonly MarkerMargin _errorMargin;
    private OpenDocumentViewModel? _viewModel;

    public DocumentView()
    {
        InitializeComponent();

        _errorMargin = new MarkerMargin { Visibility = Visibility.Collapsed, MarkerImage = TryFindResource("ExceptionMarker") as ImageSource, Width = 10 };
        Editor.TextArea.LeftMargins.Insert(0, _errorMargin);
        Editor.PreviewMouseWheel += EditorPreviewMouseWheel;
        Editor.TextArea.Caret.PositionChanged += CaretOnPositionChanged;
        Editor.TextArea.SelectionChanged += EditorSelectionChanged;

        DataContextChanged += OnDataContextChanged;
    }

    public OpenDocumentViewModel ViewModel => _viewModel.NotNull();

    private void EditorSelectionChanged(object? sender, EventArgs e)
        => ViewModel.SelectedText = Editor.SelectedText;

    private void CaretOnPositionChanged(object? sender, EventArgs eventArgs)
    {
        Ln.Text = Editor.TextArea.Caret.Line.ToString(CultureInfo.InvariantCulture);
        Col.Text = Editor.TextArea.Caret.Column.ToString(CultureInfo.InvariantCulture);
    }

    private void EditorPreviewMouseWheel(object? sender, MouseWheelEventArgs args)
    {
        if (_viewModel == null)
        {
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            _viewModel.MainViewModel.EditorFontSize += args.Delta > 0 ? 1 : -1;
            args.Handled = true;
        }
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs args)
    {
        Func<object, DependencyPropertyChangedEventArgs, Task> task = OnDataContextChangedAsync;
        _ = task.Invoke(this, args);
    }

    private async Task OnDataContextChangedAsync(object? sender, DependencyPropertyChangedEventArgs args)
    {
        _viewModel = (OpenDocumentViewModel)args.NewValue;
        BindingOperations.EnableCollectionSynchronization(_viewModel.Results, _viewModel.Results);

        _viewModel.ReadInput += OnReadInput;
        _viewModel.NuGet.PackageInstalled += NuGetOnPackageInstalled;

        _viewModel.EditorFocus += (o, e) => Editor.Focus();
        _viewModel.EditorChangeLocation += ((int line, int column) value) => ChangePosition(value.line, value.column);
        _viewModel.DocumentUpdated += Editor.RefreshHighlighting;

        _viewModel.MainViewModel.EditorFontSizeChanged += EditorFontSizeChanged;
        Editor.FontSize = _viewModel.MainViewModel.EditorFontSize;

        string documentText = await _viewModel.LoadTextAsync().ConfigureAwait(true);

        ViewModel.MainViewModel.ThemeChanged += OnThemeChanged;
        DocumentId documentId = await Editor.InitializeAsync(_viewModel.MainViewModel.RoslynHost, new ThemeClassificationColors(_viewModel.MainViewModel.Theme),
            _viewModel.WorkingDirectory, documentText, _viewModel.SourceCodeKind).ConfigureAwait(true);

        _viewModel.Initialize(documentId, OnError,
            () => new TextSpan(Editor.SelectionStart, Editor.SelectionLength),
            this);

        Editor.Document.TextChanged += (o, e) => _viewModel.OnTextChanged();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        Editor.ClassificationHighlightColors = new ThemeClassificationColors(ViewModel.MainViewModel.Theme);
    }

    private void OnReadInput()
    {
        TextBox textBox = new();

        TaskDialog dialog = new()
        {
            Header = "Console Input",
            Content = textBox,
            Background = Brushes.White,
        };

        textBox.Loaded += (o, e) => textBox.Focus();

        textBox.KeyDown += (o, e) =>
        {
            if (e.Key == Key.Enter)
            {
                TaskDialog.CancelCommand.Execute(null, dialog);
            }
        };

        dialog.ShowInline(this);

        ViewModel.SendInputAsync(textBox.Text).RunSynchronously();
    }

    private void OnError(ExceptionResultObject? e)
    {
        if (e != null)
        {
            _errorMargin.Visibility = Visibility.Visible;
            _errorMargin.LineNumber = e.LineNumber;
            _errorMargin.Message = "Exception: " + e.Message;
        }
        else
        {
            _errorMargin.Visibility = Visibility.Collapsed;
        }
    }

    private void EditorFontSizeChanged(double fontSize)
    {
        Editor.FontSize = fontSize;
    }

    private void NuGetOnPackageInstalled(PackageData package)
    {
        _ = Dispatcher.InvokeAsync(() =>
        {
            string text = $"#r \"nuget: {package.Id}, {package.Version}\"{Environment.NewLine}";
            Editor.Document.Insert(0, text, AnchorMovementType.Default);
        });
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
        {
            switch (e.Key)
            {
                case Key.T:
                    e.Handled = true;
                    NuGetSearch.Focus();
                    break;
            }
        }
    }

    private void Editor_OnLoaded(object? sender, RoutedEventArgs e)
    {
        _ = Dispatcher.InvokeAsync(Editor.Focus, System.Windows.Threading.DispatcherPriority.Background);
    }

    public bool IsDisposed { get; private set; }
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_viewModel?.MainViewModel is not { } mainViewModel)
            {
                return;
            }

            mainViewModel.EditorFontSizeChanged -= EditorFontSizeChanged;
            mainViewModel.ThemeChanged -= OnThemeChanged;
            IsDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void ChangePosition(int lineNumber, int column)
    {
        Editor.TextArea.Caret.Line = lineNumber;
        Editor.TextArea.Caret.Column = column;
        Editor.ScrollToLine(lineNumber);

        _ = Dispatcher.InvokeAsync(Editor.Focus);
    }

    private void SearchTerm_OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && ViewModel.NuGet.Packages?.Any() == true)
        {
            if (!ViewModel.NuGet.IsPackagesMenuOpen)
            {
                ViewModel.NuGet.IsPackagesMenuOpen = true;
            }
            RootNuGetMenu.Focus();
        }
        else if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Editor.Focus();
        }
    }
}
