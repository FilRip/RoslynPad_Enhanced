using System.Windows.Controls.Primitives;

namespace RoslynPad.Editor;

internal class ContextActionsBulbContextMenu : ContextMenu
{
    private readonly ActionCommandConverter _converter;

    public ContextActionsBulbContextMenu(ActionCommandConverter converter)
    {
        _converter = converter;
        ItemContainerStyle = CreateItemContainerStyle();
        HasDropShadow = false;
        Placement = SystemParameters.MenuDropAlignment ? PlacementMode.Left : PlacementMode.Right;
    }

    private Style CreateItemContainerStyle()
    {
        Style style = new(typeof(MenuItem), TryFindResource(typeof(MenuItem)) as Style);
        style.Setters.Add(new Setter(MenuItem.CommandProperty,
            new Binding { Converter = _converter }));
        style.Seal();
        return style;
    }
}
