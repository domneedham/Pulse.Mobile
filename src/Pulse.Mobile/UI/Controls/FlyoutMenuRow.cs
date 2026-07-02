using System.Windows.Input;

namespace Pulse.UI.Controls;

/// <summary>
/// One tappable row in the Shell flyout (icon + label + chevron). Kept intentionally plain — the
/// flyout only lists screens that actually exist (see AppShell.xaml); rows for Calendar/Wishlist/etc.
/// get added here once those screens are built.
/// </summary>
public sealed class FlyoutMenuRow : Grid
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(PulseIcon), typeof(FlyoutMenuRow), default(PulseIcon),
            propertyChanged: (b, _, n) => ((FlyoutMenuRow)b)._icon.Icon = (PulseIcon)n);

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(FlyoutMenuRow), null,
            propertyChanged: (b, _, n) => ((FlyoutMenuRow)b)._label.Text = (string?)n);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(FlyoutMenuRow), null,
            propertyChanged: (b, _, n) => ((FlyoutMenuRow)b)._tap.Command = (ICommand?)n);

    private readonly AppIcon _icon;
    private readonly Label _label;
    private readonly TapGestureRecognizer _tap;

    public PulseIcon Icon
    {
        get => (PulseIcon)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public FlyoutMenuRow()
    {
        ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)];
        ColumnSpacing = 14;
        Padding = new Thickness(12, 12);
        HeightRequest = 48;

        _icon = new AppIcon { Size = 20, VerticalOptions = LayoutOptions.Center };
        _icon.SetDynamicResource(AppIcon.ColorProperty, "Ink");
        SetColumn((BindableObject)_icon, 0);

        _label = new Label { FontFamily = "SansSemiBold", FontSize = 15, VerticalOptions = LayoutOptions.Center };
        _label.SetDynamicResource(Label.TextColorProperty, "Ink");
        SetColumn((BindableObject)_label, 1);

        Children.Add(_icon);
        Children.Add(_label);

        _tap = new TapGestureRecognizer();
        GestureRecognizers.Add(_tap);
    }
}
