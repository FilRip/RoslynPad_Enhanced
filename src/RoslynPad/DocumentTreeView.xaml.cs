﻿using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

using RoslynPad.UI;

namespace RoslynPad;

public partial class DocumentTreeView
{
    private MainViewModel? _viewModel;

    public DocumentTreeView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        _viewModel = (MainViewModel)e.NewValue;
    }

    private void OnDocumentClick(object? sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            OpenDocument(e.Source);
        }
    }

    private void OnDocumentKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OpenDocument(e.Source);
        }
    }

    private void OpenDocument(object source)
    {
        DocumentViewModel documentViewModel = (DocumentViewModel)((FrameworkElement)source).DataContext;
        _viewModel?.OpenDocument(documentViewModel);
    }

    private void DocumentsContextMenu_OpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)e.Source).DataContext is DocumentViewModel documentViewModel)
        {
            if (documentViewModel.IsFolder)
            {
                _ = Task.Run(() => Process.Start(new ProcessStartInfo { FileName = documentViewModel.Path, UseShellExecute = true }));
            }
            else
            {
                _ = Task.Run(() => Process.Start("explorer.exe", "/select," + documentViewModel.Path));
            }
        }
    }

    private void Search_OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                _viewModel?.ClearSearchCommand.Execute();
                break;
            case Key.Enter:
                _viewModel?.SearchCommand.Execute();
                break;
        }
    }

#pragma warning disable S1144, CA1822, IDE0051 // Supprimer les membres privés non utilisés
    private bool FilterCollectionViewSourceConverter_OnFilter(object arg) => ((DocumentViewModel)arg).IsSearchMatch;
#pragma warning restore S1144, CA1822, IDE0051 // Supprimer les membres privés non utilisés
}

internal sealed class FilterCollectionViewConverter : IValueConverter
{
    public string? FilterProperty { get; set; }

    public event Predicate<object>? Filter;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IList list)
        {
            ListCollectionView collectionView = new(list)
            {
                IsLiveFiltering = true,
                LiveFilteringProperties = { FilterProperty },
                Filter = Filter,
            };

            return collectionView;
        }

        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
