namespace Pulse.UI.Controls;

/// <summary>
/// Circular avatar: shows the user's photo when an AvatarUrl is available,
/// otherwise their initials on a colour deterministically picked from the name
/// (so the same person is always the same colour).
/// </summary>
public class AvatarView : Border
{
    public static readonly BindableProperty DisplayNameProperty = BindableProperty.Create(
        nameof(DisplayName), typeof(string), typeof(AvatarView), null,
        propertyChanged: (b, _, _) => ((AvatarView)b).Render());

    public static readonly BindableProperty AvatarUrlProperty = BindableProperty.Create(
        nameof(AvatarUrl), typeof(string), typeof(AvatarView), null,
        propertyChanged: (b, _, _) => ((AvatarView)b).Render());

    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size), typeof(double), typeof(AvatarView), 44d,
        propertyChanged: (b, _, _) => ((AvatarView)b).Render());

    public string? DisplayName
    {
        get => (string?)GetValue(DisplayNameProperty);
        set => SetValue(DisplayNameProperty, value);
    }

    public string? AvatarUrl
    {
        get => (string?)GetValue(AvatarUrlProperty);
        set => SetValue(AvatarUrlProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    private readonly Label _initials;
    private readonly Image _image;

    public AvatarView()
    {
        StrokeThickness = 0;
        Stroke = Colors.Transparent;
        Padding = 0;

        _initials = new Label
        {
            FontFamily = "SansBold",
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };

        _image = new Image { Aspect = Aspect.AspectFill, IsVisible = false };

        Content = new Grid { Children = { _initials, _image } };
        Render();
    }

    private void Render()
    {
        WidthRequest = Size;
        HeightRequest = Size;
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = (float)(Size / 2) };

        var name = string.IsNullOrWhiteSpace(DisplayName) ? "?" : DisplayName.Trim();
        Background = new SolidColorBrush(PersonColors.Background(name));
        _initials.TextColor = PersonColors.Foreground(name);
        _initials.FontSize = Size * 0.38;
        _initials.Text = PersonColors.Initials(name);

        bool hasUrl = !string.IsNullOrWhiteSpace(AvatarUrl);
        _image.IsVisible = hasUrl;
        _image.Source = hasUrl ? ImageSource.FromUri(new Uri(AvatarUrl!)) : null;
    }
}
