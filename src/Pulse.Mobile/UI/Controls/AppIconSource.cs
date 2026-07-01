using System.ComponentModel;
using MauiIcons.Core.Helpers;
using MauiIcons.Material.Outlined;
using Microsoft.Maui.Platform;

namespace Pulse.UI.Controls;

/// <summary>
/// A cross-platform, unsealed ImageSource that works anywhere.
/// Converts directly to native vector SF Symbols on iOS and Material Fonts on Android via Streams.
/// </summary>
public sealed class AppIconSource : ImageSource, IStreamImageSource
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(
            nameof(Icon),
            typeof(PulseIcon),
            typeof(AppIconSource),
            default(PulseIcon),
            propertyChanged: (b, o, n) => ((AppIconSource)b).OnSourceChanged());

    public static readonly BindableProperty ColorProperty =
        BindableProperty.Create(
            nameof(Color),
            typeof(Color),
            typeof(AppIconSource),
            null,
            propertyChanged: (b, o, n) => ((AppIconSource)b).OnSourceChanged());

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(
            nameof(Size),
            typeof(double),
            typeof(AppIconSource),
            24d,
            propertyChanged: (b, o, n) => ((AppIconSource)b).OnSourceChanged());

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

    [TypeConverter(typeof(FontSizeConverter))]
    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public override bool IsEmpty => false;

    /// <summary>
    /// This is the core method called natively by MAUI when loading the image source anywhere in the UI layout.
    /// </summary>
    public Task<Stream> GetStreamAsync(CancellationToken cancellationToken = default)
    {
#if IOS
        // 1. Resolve the string identifier for the SF Symbol (e.g. "heart")
        string sfSymbolName = PulseIconResolver.ResolveCupertino(Icon);

        // 2. Fetch the pristine system vector image from Apple's native core cache
        var uiImage = UIKit.UIImage.GetSystemImage(sfSymbolName);
        if (uiImage is null)
        {
            return Task.FromResult(Stream.Null);
        }

        // 3. Apply the tint color natively to the vector if one is declared
        if (Color is not null)
        {
            uiImage = uiImage.ApplyTintColor(Color.ToPlatform(), UIKit.UIImageRenderingMode.AlwaysOriginal);
        }

        // 4. Extract its uncompressed byte data and stream it cleanly directly to the UI rendering tree
        Stream vectorStream = uiImage.AsPNG()?.AsStream() ?? Stream.Null;
        return Task.FromResult(vectorStream);

#elif ANDROID
        // 1. Resolve the Material Font Glyph character block data
        var materialIcon = PulseIconResolver.ResolveMaterial(Icon);
        string glyphText = materialIcon.GetDescription();
        
        // For Android text font streams, we convert the character directly into a layout text stream block
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(glyphText));
        return Task.FromResult<Stream>(stream);
#else
        return Task.FromResult(Stream.Null);
#endif
    }
}