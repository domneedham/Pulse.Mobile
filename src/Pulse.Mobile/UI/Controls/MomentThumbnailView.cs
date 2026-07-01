namespace Pulse.UI.Controls;

/// <summary>
/// A rounded-square Moment thumbnail tile for one person: their photo, or their stroke drawing, or —
/// if they haven't answered with either yet — their initials on their <see cref="PersonColors"/> colour
/// (the same placeholder <see cref="AvatarView"/> uses), so an empty tile still denotes whose slot it
/// is rather than just disappearing. Used in pairs on Home to stack both partners' thumbnails.
/// </summary>
public class MomentThumbnailView : Border
{
    public static readonly BindableProperty PersonNameProperty = BindableProperty.Create(
        nameof(PersonName), typeof(string), typeof(MomentThumbnailView), null,
        propertyChanged: (b, _, _) => ((MomentThumbnailView)b).Render());

    public static readonly BindableProperty PhotoUrlProperty = BindableProperty.Create(
        nameof(PhotoUrl), typeof(string), typeof(MomentThumbnailView), null,
        propertyChanged: (b, _, _) => ((MomentThumbnailView)b).Render());

    public static readonly BindableProperty StrokeDataProperty = BindableProperty.Create(
        nameof(StrokeData), typeof(string), typeof(MomentThumbnailView), null,
        propertyChanged: (b, _, _) => ((MomentThumbnailView)b).Render());

    public string? PersonName
    {
        get => (string?)GetValue(PersonNameProperty);
        set => SetValue(PersonNameProperty, value);
    }

    public string? PhotoUrl
    {
        get => (string?)GetValue(PhotoUrlProperty);
        set => SetValue(PhotoUrlProperty, value);
    }

    public string? StrokeData
    {
        get => (string?)GetValue(StrokeDataProperty);
        set => SetValue(StrokeDataProperty, value);
    }

    private readonly Label _initials;
    private readonly Image _image;
    private readonly StrokeView _strokeView;

    public MomentThumbnailView()
    {
        StrokeThickness = 3;
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 };
        Padding = 0;
        this.SetDynamicResource(Border.StrokeProperty, "Surface");

        // A visible drop shadow so the back tile in a stacked pair reads as sitting under/behind the
        // front one, rather than blending into the card's own background.
        var shadow = new Shadow { Opacity = 0.22f, Radius = 8, Offset = new Point(0, 3) };
        shadow.SetDynamicResource(Shadow.BrushProperty, "Ink");
        Shadow = shadow;

        _initials = new Label
        {
            FontFamily = "SansBold",
            FontSize = 20,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };

        _image = new Image { Aspect = Aspect.AspectFill, IsVisible = false };
        _strokeView = new StrokeView { IsVisible = false };

        Content = new Grid { Children = { _initials, _image, _strokeView } };
        Render();
    }

    private void Render()
    {
        var name = string.IsNullOrWhiteSpace(PersonName) ? "?" : PersonName.Trim();
        var hasPhoto = !string.IsNullOrWhiteSpace(PhotoUrl);
        var hasDrawing = !hasPhoto && !string.IsNullOrWhiteSpace(StrokeData);

        Background = hasPhoto || hasDrawing
            ? new SolidColorBrush(Colors.Transparent)
            : new SolidColorBrush(PersonColors.Background(name));

        _initials.IsVisible = !hasPhoto && !hasDrawing;
        _initials.TextColor = PersonColors.Foreground(name);
        _initials.Text = PersonColors.Initials(name);

        _image.IsVisible = hasPhoto;
        _image.Source = hasPhoto ? ImageSource.FromUri(new Uri(PhotoUrl!)) : null;

        _strokeView.IsVisible = hasDrawing;
        _strokeView.StrokeData = hasDrawing ? StrokeData : null;
    }
}
