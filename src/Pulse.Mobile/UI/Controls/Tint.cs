namespace Pulse.UI.Controls;

/// <summary>
/// Attached properties that paint an element's background or foreground from a
/// theme colour-key prefix (e.g. "CatEnergise", "FocusStrength"). The prefix is
/// data-bindable; the handler appends "Bg"/"Fg" and applies the resulting theme
/// colour via <see cref="BindableObject.SetDynamicResource"/>, so the element
/// repaints automatically when the active theme dictionary is swapped — no
/// view-model plumbing, subscriptions, or refresh calls required.
///
/// Usage:
///   <Border ui:Tint.Background="{Binding ColorKey}" />
///   <Label  ui:Tint.Foreground="{Binding ColorKey}" />
/// </summary>
public static class Tint
{
    public static readonly BindableProperty BackgroundProperty =
        BindableProperty.CreateAttached(
            "Background", typeof(string), typeof(Tint), null,
            propertyChanged: (b, _, v) => Apply(b, v, "Bg", VisualElement.BackgroundProperty));

    public static readonly BindableProperty ForegroundProperty =
        BindableProperty.CreateAttached(
            "Foreground", typeof(string), typeof(Tint), null,
            propertyChanged: (b, _, v) => Apply(b, v, "Fg", Label.TextColorProperty));

    /// <summary>Paints the background with the *foreground* tint colour — for solid icon badges.</summary>
    public static readonly BindableProperty AccentBackgroundProperty =
        BindableProperty.CreateAttached(
            "AccentBackground", typeof(string), typeof(Tint), null,
            propertyChanged: (b, _, v) => Apply(b, v, "Fg", VisualElement.BackgroundProperty));

    public static string? GetAccentBackground(BindableObject view) =>
        (string?)view.GetValue(AccentBackgroundProperty);

    public static void SetAccentBackground(BindableObject view, string? value) =>
        view.SetValue(AccentBackgroundProperty, value);

    public static string? GetBackground(BindableObject view) =>
        (string?)view.GetValue(BackgroundProperty);

    public static void SetBackground(BindableObject view, string? value) =>
        view.SetValue(BackgroundProperty, value);

    public static string? GetForeground(BindableObject view) =>
        (string?)view.GetValue(ForegroundProperty);

    public static void SetForeground(BindableObject view, string? value) =>
        view.SetValue(ForegroundProperty, value);

    private static void Apply(BindableObject bindable, object? value, string part, BindableProperty target)
    {
        if (bindable is not Element element)
        {
            return;
        }

        if (value is string prefix && prefix.Length > 0)
        {
            // DynamicResource tracks theme swaps; a Color resource coerces to a
            // SolidColorBrush for Brush-typed targets (see IconButton precedent).
            element.SetDynamicResource(target, $"{prefix}{part}");
        }
        else
        {
            element.RemoveDynamicResource(target);
        }
    }
}
