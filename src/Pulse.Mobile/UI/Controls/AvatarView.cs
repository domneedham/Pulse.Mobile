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

    private static readonly (Color Bg, Color Fg)[] Palette =
    [
        (Color.FromArgb("#FFE0D1"), Color.FromArgb("#C2470F")),
        (Color.FromArgb("#D9EAC8"), Color.FromArgb("#48702A")),
        (Color.FromArgb("#D4E0F5"), Color.FromArgb("#3A5795")),
        (Color.FromArgb("#F4E3C2"), Color.FromArgb("#94621B")),
        (Color.FromArgb("#E6DCF2"), Color.FromArgb("#6B4E9E")),
        (Color.FromArgb("#CCE9E4"), Color.FromArgb("#2E7468")),
    ];

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
        var (bg, fg) = Palette[Math.Abs(StableHash(name)) % Palette.Length];

        Background = new SolidColorBrush(bg);
        _initials.TextColor = fg;
        _initials.FontSize = Size * 0.38;
        _initials.Text = Initials(name);

        bool hasUrl = !string.IsNullOrWhiteSpace(AvatarUrl);
        _image.IsVisible = hasUrl;
        _image.Source = hasUrl ? ImageSource.FromUri(new Uri(AvatarUrl!)) : null;
    }

    private static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length >= 2
            ? $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}"
            : char.ToUpperInvariant(name[0]).ToString();
    }

    /// <summary>string.GetHashCode is randomised per process; colours must survive restarts.</summary>
    private static int StableHash(string value)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in value)
            {
                hash = hash * 31 + c;
            }

            return hash;
        }
    }
}
