// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RoslynPad.Roslyn.LanguageServices.PickMembers;

[Export(typeof(IPickMembersDialog))]
internal sealed partial class PickMembersDialog : Window, IPickMembersDialog
{
    private PickMembersDialogViewModel? _viewModel;

    /// <summary>
    /// For test purposes only. The integration tests need to know when the dialog is up and
    /// ready for automation.
    /// </summary>
    internal static event Action? TEST_DialogLoaded;

    // Expose localized strings for binding
    public static string PickMembersDialogTitle => "Pick members";

    public static string SelectAll => "Select All";
    public static string DeselectAll => "Deselect All";
    public static string OK => "OK";
    public static string Cancel => "Cancel";

    [ImportingConstructor]
    public PickMembersDialog()
    {
        SetCommandBindings();

        InitializeComponent();

        IsVisibleChanged += PickMembers_IsVisibleChanged;
    }

    private void PickMembers_IsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            IsVisibleChanged -= PickMembers_IsVisibleChanged;
            TEST_DialogLoaded?.Invoke();
        }
    }

    private void SetCommandBindings()
    {
        CommandBindings.Add(new CommandBinding(
            new RoutedCommand(
                "SelectAllClickCommand",
                typeof(PickMembersDialog),
                new InputGestureCollection(new List<InputGesture> { new KeyGesture(Key.S, ModifierKeys.Alt) })),
            Select_All_Click));

        CommandBindings.Add(new CommandBinding(
            new RoutedCommand(
                "DeselectAllClickCommand",
                typeof(PickMembersDialog),
                new InputGestureCollection(new List<InputGesture> { new KeyGesture(Key.D, ModifierKeys.Alt) })),
            Deselect_All_Click));
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
        => DialogResult = true;

    private void Cancel_Click(object? sender, RoutedEventArgs e)
        => DialogResult = false;

    private void Select_All_Click(object? sender, RoutedEventArgs e)
        => _viewModel?.SelectAll();

    private void Deselect_All_Click(object? sender, RoutedEventArgs e)
        => _viewModel?.DeselectAll();

    private void MoveUp_Click(object? sender, EventArgs e)
    {
        int oldSelectedIndex = Members.SelectedIndex;
        if (_viewModel?.CanMoveUp == true && oldSelectedIndex >= 0)
        {
            _viewModel.MoveUp();
            Members.Items.Refresh();
            Members.SelectedIndex = oldSelectedIndex - 1;
        }

        SetFocusToSelectedRow();
    }

    private void MoveDown_Click(object? sender, EventArgs e)
    {
        int oldSelectedIndex = Members.SelectedIndex;
        if (_viewModel?.CanMoveDown == true && oldSelectedIndex >= 0)
        {
            _viewModel.MoveDown();
            Members.Items.Refresh();
            Members.SelectedIndex = oldSelectedIndex + 1;
        }

        SetFocusToSelectedRow();
    }

    private void SetFocusToSelectedRow()
    {
        if (Members.SelectedIndex >= 0)
        {
            ListViewItem? row = Members.ItemContainerGenerator.ContainerFromIndex(Members.SelectedIndex) as ListViewItem;
            if (row == null)
            {
                Members.ScrollIntoView(Members.SelectedItem);
                row = Members.ItemContainerGenerator.ContainerFromIndex(Members.SelectedIndex) as ListViewItem;
            }

            row?.Focus();
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
        var selectedItems = Members.SelectedItems.OfType<PickMembersDialogViewModel.MemberSymbolViewModel>().ToArray();
        var allChecked = Array.TrueForAll(selectedItems, m => m.IsChecked);
        foreach (var item in selectedItems)
        {
            item.IsChecked = !allChecked;
        }
    }

    public object ViewModel
    {
        get => DataContext ?? throw new InvalidOperationException("DataContext is null");
        set
        {
            _viewModel = (PickMembersDialogViewModel)value;
            DataContext = value;
        }
    }

    bool? IRoslynDialog.Show()
    {
        this.SetOwnerToActive();
        return ShowDialog();
    }
}
