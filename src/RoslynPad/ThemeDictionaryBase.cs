using System.Windows;
using System.Windows.Media;

using RoslynPad.Themes;

#pragma warning disable CA1010 // Generic interface should also be implemented

namespace RoslynPad;

#pragma warning disable CS9113 // Paramètre non lu.
public abstract class ThemeDictionaryBase(Theme theme) : ResourceDictionary
#pragma warning restore CS9113 // Paramètre non lu.
{
    protected void SetThemeColor(string name, string colorString)
    {
        SolidColorBrush brush = CreateBrush(ParseColor(colorString));
        this[name] = brush;
        this[GetColorKey(name)] = brush.Color;
    }

    protected void SetThemeColorForSystemKeys(string name, ResourceKey brushKey, ResourceKey colorKey)
    {
        this[brushKey] = this[name];
        this[colorKey] = this[GetColorKey(name)];
    }

    private static string GetColorKey(string key) => key + "Color";

    protected static SolidColorBrush? CreateBrush(Theme theme, string id)
    {
        return theme.TryGetColor(id) is { } color ? CreateBrush(ParseColor(color)) : null;
    }

    private static SolidColorBrush CreateBrush(Color color)
    {
        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }

    private static Color ParseColor(string color) => (Color)ColorConverter.ConvertFromString(color);
}
