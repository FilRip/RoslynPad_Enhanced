﻿using System.ComponentModel;
using System.Composition.Hosting;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

using Avalon.Windows.Controls;

using AvalonDock;
using AvalonDock.Controls;
using AvalonDock.Layout.Serialization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RoslynPad.Themes;
using RoslynPad.UI;

namespace RoslynPad;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly MainViewModel _viewModel;
    private bool _isClosing;
    private bool _isClosed;
    private ThemeDictionary? _themeDictionary;

    public MainWindow()
    {
        Loaded += OnLoaded;

        ServiceCollection services = new();
        services.AddLogging(l =>
        {
#if DEBUG
            l.AddDebug();
#endif
        });

        ContainerConfiguration container = new ContainerConfiguration()
            .WithProvider(new ServiceCollectionExportDescriptorProvider(services))
            .WithAssembly(typeof(MainViewModel).Assembly)   // RoslynPad.Common.UI
            .WithAssembly(typeof(MainWindow).Assembly);     // RoslynPad
        IServiceProvider? serviceProvider = container.CreateContainer().GetExport<IServiceProvider>();

        _viewModel = serviceProvider.GetRequiredService<MainViewModel>();
        _viewModel.ThemeChanged += OnViewModelThemeChanged;
        _ = Task.Run(_viewModel.InitializeThemeAsync);

        DataContext = _viewModel;
        InitializeComponent();
        DocumentsPane.ToggleAutoHide();
        SetDockTheme(_viewModel.Theme);
        LoadWindowLayout();
        LoadDockLayout();
    }

    private bool IsDark => _viewModel.ThemeType == ThemeType.Dark;

    private void OnViewModelThemeChanged(object? sender, EventArgs e)
    {
        Application app = Application.Current;
        if (_themeDictionary is not null)
        {
            app.Resources.MergedDictionaries.Remove(_themeDictionary);
        }

        _themeDictionary = new ThemeDictionary(_viewModel.Theme);
        app.Resources.MergedDictionaries.Add(_themeDictionary);

        SetDockTheme(_viewModel.Theme);
        this.UseImmersiveDarkMode(IsDark);
    }

    private void SetDockTheme(Theme theme)
    {
        if (DockingManager is null)
        {
            return;
        }

        DockingManager.Theme = IsDark ? new AvalonDock.Themes.Vs2013DarkTheme() : new AvalonDock.Themes.Vs2013LightTheme();
        DockingManager.Resources.MergedDictionaries.Add(new DockThemeDictionary(theme));
        DockingManager.DocumentPaneControlStyle = new Style(typeof(LayoutDocumentPaneControl), DockingManager.DocumentPaneControlStyle)
        {
            Setters =
            {
                new Setter(ItemsControl.ItemContainerStyleProperty, DockingManager.TryFindResource("DocumentPaneControlTabStyle"))
            }
        };
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        _ = _viewModel.InitializeAsync().ConfigureAwait(false);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (!_isClosing)
        {
            SaveDockLayout();
            SaveWindowLayout();

            _isClosing = true;
            IsEnabled = false;
            e.Cancel = true;

            try
            {
                _ = _viewModel.OnExitAsync();
            }
            catch
            {
                // ignored
            }

            _isClosed = true;
            _ = Dispatcher.InvokeAsync(Close);
        }
        else
        {
            e.Cancel = !_isClosed;
        }
    }

    private void LoadWindowLayout()
    {
        string? boundsString = _viewModel.Settings.WindowBounds;

        if (!string.IsNullOrEmpty(boundsString))
        {
            try
            {
                Rect bounds = Rect.Parse(boundsString);
                if (bounds != default)
                {
                    Left = bounds.Left;
                    Top = bounds.Top;
                    Width = bounds.Width;
                    Height = bounds.Height;
                }
            }
            catch (FormatException) { /* Ignore errors */ }
        }

        if (Enum.TryParse(_viewModel.Settings.WindowState, out WindowState state) &&
            state != WindowState.Minimized)
        {
            WindowState = state;
        }

        if (_viewModel.Settings.WindowFontSize.HasValue)
        {
            FontSize = _viewModel.Settings.WindowFontSize.Value;
        }

        Width = Math.Clamp(Width, 0, SystemParameters.VirtualScreenWidth);
        Height = Math.Clamp(Height, 0, SystemParameters.VirtualScreenHeight);
        Left = Math.Clamp(Left, SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenWidth - Width);
        Top = Math.Clamp(Top, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenHeight - Height);
    }

    private void SaveWindowLayout()
    {
        _viewModel.Settings.WindowBounds = RestoreBounds.ToString(CultureInfo.InvariantCulture);
        _viewModel.Settings.WindowState = WindowState.ToString();
    }

    private void LoadDockLayout()
    {
        string? layout = _viewModel.Settings.DockLayout;
        if (string.IsNullOrEmpty(layout))
            return;

        XmlLayoutSerializer serializer = new(DockingManager);
        StringReader reader = new(layout);
        try
        {
            serializer.Deserialize(reader);
        }
        catch
        {
            // ignored
        }
    }

    private void SaveDockLayout()
    {
        XmlLayoutSerializer serializer = new(DockingManager);
        XDocument document = new();
        using (System.Xml.XmlWriter writer = document.CreateWriter())
        {
            serializer.Serialize(writer);
        }
        document.Root?.Element("FloatingWindows")?.Remove();
        _viewModel.Settings.DockLayout = document.ToString();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        Application.Current.Shutdown();
    }

    private void DockingManager_OnDocumentClosing(object? sender, DocumentClosingEventArgs e)
    {
        e.Cancel = true;
        OpenDocumentViewModel document = (OpenDocumentViewModel)e.Document.Content;
        _ = _viewModel.CloseDocumentAsync(document).ConfigureAwait(false);
    }

    private void ViewErrorDetails_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel.LastError == null) return;

        TaskDialog taskDialog = new()
        {
            Header = "Unhandled Exception",
            Content = _viewModel.LastError.ToString(),
            Buttons =
            {
                TaskDialogButtonData.FromStandardButtons(TaskDialogButtons.Close).First(),
            },
        };

        taskDialog.SetResourceReference(BackgroundProperty, SystemColors.WindowBrushKey);
        taskDialog.ShowInline(this);
    }

    private void ViewUpdateClick(object? sender, RoutedEventArgs e)
    {
        _ = Task.Run(() => Process.Start(new ProcessStartInfo($"https://roslynpad.net/") { UseShellExecute = true }));
    }

#pragma warning disable IDE0051, IDE0060 // Supprimer les membres privés non utilisés
    private void ILViewer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        SetShowIL();
    }
#pragma warning restore IDE0051, IDE0060 // Supprimer les membres privés non utilisés

    private void DockingManager_ActiveContentChanged(object sender, EventArgs e)
    {
        SetShowIL();
    }

    private void SetShowIL()
    {
        if (_viewModel.CurrentOpenDocument is not { } currentDocument)
        {
            return;
        }

        currentDocument.ShowIL = ILViewer.IsVisible;
    }
}
