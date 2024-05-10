using System.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RoslynPad.Roslyn.LanguageServices.ExtractInterface;

[Export(typeof(IExtractInterfaceDialog))]
internal sealed partial class ExtractInterfaceDialog : IExtractInterfaceDialog
{
    private ExtractInterfaceDialogViewModel? _viewModel;

    public static string ExtractInterfaceDialogTitle => "Extract Interface";
    public static string NewInterfaceName => "New Interface Name";
    public static string GeneratedName => "Generated Name";
    public static string NewFileName => "New File Name";
    public static string SelectPublicMembersToFormInterface => "Select Public Members To Form Interface";
    public static string SelectAll => "Select All";
    public static string DeselectAll => "Deselect All";
    public static string OK => "OK";
    public static string Cancel => "Cancel";

    public ExtractInterfaceDialog()
    {
        SetCommandBindings();

        InitializeComponent();

        Loaded += ExtractInterfaceDialog_Loaded;
        IsVisibleChanged += ExtractInterfaceDialog_IsVisibleChanged;
    }

    private void ExtractInterfaceDialog_Loaded(object? sender, RoutedEventArgs e)
    {
        interfaceNameTextBox.Focus();
        interfaceNameTextBox.SelectAll();
    }

    private void ExtractInterfaceDialog_IsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            IsVisibleChanged -= ExtractInterfaceDialog_IsVisibleChanged;
        }
    }

    private void SetCommandBindings()
    {
        CommandBindings.Add(new CommandBinding(
            new RoutedCommand(
                "SelectAllClickCommand",
                typeof(ExtractInterfaceDialog),
                new InputGestureCollection(new List<InputGesture> { new KeyGesture(Key.S, ModifierKeys.Alt) })),
            Select_All_Click));

        CommandBindings.Add(new CommandBinding(
            new RoutedCommand(
                "DeselectAllClickCommand",
                typeof(ExtractInterfaceDialog),
                new InputGestureCollection(new List<InputGesture> { new KeyGesture(Key.D, ModifierKeys.Alt) })),
            Deselect_All_Click));
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        if (_viewModel?.TrySubmit() == true)
        {
            DialogResult = true;
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Select_All_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel?.SelectAll();
    }

    private void Deselect_All_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel?.DeselectAll();
    }

    private void SelectAllInTextBox(object? sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TextBox textbox && Mouse.LeftButton == MouseButtonState.Released)
        {
            textbox.SelectAll();
        }
    }

    private void OnListViewPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && e.KeyboardDevice.Modifiers == ModifierKeys.None)
        {
            ToggleCheckSelection();
            e.Handled = true;
        }
    }

    private void OnListViewDoubleClick(object? sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            ToggleCheckSelection();
            e.Handled = true;
        }
    }

    private void ToggleCheckSelection()
    {
        var selectedItems = Members.SelectedItems.OfType<ExtractInterfaceDialogViewModel.MemberSymbolViewModel>().ToArray();
        var allChecked = Array.TrueForAll(selectedItems, m => m.IsChecked);
        foreach (var item in selectedItems)
        {
            item.IsChecked = !allChecked;
        }
    }


    public object ViewModel
    {
        get => DataContext;
        set
        {
            DataContext = value;
            _viewModel = (ExtractInterfaceDialogViewModel)value;
        }
    }

    bool? IRoslynDialog.Show()
    {
        this.SetOwnerToActive();
        return ShowDialog();
    }
}
