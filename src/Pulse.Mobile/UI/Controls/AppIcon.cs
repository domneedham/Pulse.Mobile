using Microsoft.Maui.Controls;

namespace Pulse.UI.Controls;

/// <summary>
/// A single-file, C#-only standalone icon view control.
/// Wraps AppIconSource and exposes clean properties for layout design.
/// </summary>
public sealed class AppIcon : ContentView
{
    private readonly Image _innerImage;

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(PulseIcon), typeof(AppIcon), default(PulseIcon),
            propertyChanged: (b, o, n) => ((AppIcon)b).UpdateSource());

    public static readonly BindableProperty ColorProperty =
        BindableProperty.Create(nameof(Color), typeof(Color), typeof(AppIcon), null,
            propertyChanged: (b, o, n) => ((AppIcon)b).UpdateSource());

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(AppIcon), 24d,
            propertyChanged: (b, o, n) => ((AppIcon)b).UpdateSource());

    public PulseIcon Icon
    {
        get => (PulseIcon)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Color? Color
    {
        get => (Color?)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public AppIcon()
    {
        // 1. Initialize the native image layout container
        _innerImage = new Image
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        // 2. Set the ContentView's Content property to our inner image layout container
        Content = _innerImage;

        // 3. Run initial configuration pass
        UpdateSource();
    }

    private void UpdateSource()
    {
        // Bind dimensional footprint demands directly down to our inner element
        _innerImage.HeightRequest = Size;
        _innerImage.WidthRequest = Size;

        // Re-instantiate our unified stream provider with the active parameters
        _innerImage.Source = new AppIconSource
        {
            Icon = this.Icon,
            Color = this.Color,
            Size = this.Size
        };
    }
}