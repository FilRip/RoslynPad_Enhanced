﻿using System.Windows.Media;

using AvalonDock.Themes.VS2013.Themes;

using RoslynPad.Themes;

namespace RoslynPad;

/// <summary>
/// Interaction logic for DockStyles.xaml
/// </summary>
public partial class DockThemeDictionary : ThemeDictionaryBase
{
    public DockThemeDictionary(Theme theme) : base(theme)
    {
        InitializeComponent();
        this[ResourceKeys.Background] = CreateBrush(theme, "background");
        this[ResourceKeys.DocumentWellTabSelectedActiveBackground] = CreateBrush(theme, "tab.border");
        this[ResourceKeys.DocumentWellTabSelectedInactiveBackground] = CreateBrush(theme, "tab.inactiveBackground");
        SolidColorBrush? focusBorder = CreateBrush(theme, "focusBorder");
        this[ResourceKeys.ToolWindowCaptionActiveBackground] = focusBorder;
        this[ResourceKeys.AutoHideTabHoveredBorder] = focusBorder;
        this[ResourceKeys.AutoHideTabHoveredText] = focusBorder;
        this[ResourceKeys.ToolWindowTabSelectedActiveText] = focusBorder;
        this[ResourceKeys.ToolWindowTabSelectedInactiveText] = focusBorder;
        this[ResourceKeys.ToolWindowTabUnselectedHoveredText] = focusBorder;
    }
}
