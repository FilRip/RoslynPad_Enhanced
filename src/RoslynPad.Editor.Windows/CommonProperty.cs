namespace RoslynPad.Editor;

public static class CommonProperty
{
    public static StyledProperty<TValue> Register<TOwner, TValue>(string name,
        TValue defaultValue = default!, PropertyOptions options = PropertyOptions.None,
        Action<TOwner, CommonPropertyChangedArgs<TValue>>? onChanged = null)
        where TOwner : DependencyObject
    {
        FrameworkPropertyMetadataOptions metadataOptions = FrameworkPropertyMetadataOptions.None;

        if (options.Has(PropertyOptions.AffectsRender))
        {
            metadataOptions |= FrameworkPropertyMetadataOptions.AffectsRender;
        }

        if (options.Has(PropertyOptions.AffectsArrange))
        {
            metadataOptions |= FrameworkPropertyMetadataOptions.AffectsArrange;
        }

        if (options.Has(PropertyOptions.AffectsMeasure))
        {
            metadataOptions |= FrameworkPropertyMetadataOptions.AffectsMeasure;
        }

        if (options.Has(PropertyOptions.Inherits))
        {
            metadataOptions |= FrameworkPropertyMetadataOptions.Inherits;
        }

        if (options.Has(PropertyOptions.BindsTwoWay))
        {
            metadataOptions |= FrameworkPropertyMetadataOptions.BindsTwoWayByDefault;
        }

        PropertyChangedCallback? changedCallback = onChanged != null
            ? new PropertyChangedCallback((o, e) => onChanged((TOwner)o, new CommonPropertyChangedArgs<TValue>((TValue)e.OldValue, (TValue)e.NewValue)))
            : null;
        FrameworkPropertyMetadata metadata = new(defaultValue, metadataOptions, changedCallback);
        DependencyProperty property = DependencyProperty.Register(name, typeof(TValue), typeof(TOwner), metadata);

        return new StyledProperty<TValue>(property);
    }

    public static TValue GetValue<TValue>(this DependencyObject o, StyledProperty<TValue> property)
    {
        return (TValue)o.GetValue(property.Property);
    }

    public static void SetValue<TValue>(this DependencyObject o, StyledProperty<TValue> property, TValue value)
    {
        o.SetValue(property.Property, value);
    }
}

public sealed class StyledProperty<TValue>(DependencyProperty property)
{
    public DependencyProperty Property { get; } = property;

    public static implicit operator DependencyProperty(StyledProperty<TValue> property) => property.Property;

    public StyledProperty<TValue> AddOwner<TOwner>() =>
        new(Property.AddOwner(typeof(TOwner)));

    public Type PropertyType => Property.PropertyType;
}
