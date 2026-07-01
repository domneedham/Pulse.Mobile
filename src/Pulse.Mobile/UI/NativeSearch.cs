using System.Windows.Input;

namespace Pulse.UI;

/// <summary>
/// Attaches a native iOS <c>UISearchController</c> to a page's navigation bar — the proper Apple search
/// experience (integrated under the large title, gets Liquid Glass on iOS 26). Two-way binds the
/// search text and raises a command as the user types. No-op on other platforms (those keep an inline
/// search box). Usage on a ContentPage:
/// <code>ui:NativeSearch.Placeholder="Search pulses"
///       ui:NativeSearch.Text="{Binding SearchText}"
///       ui:NativeSearch.Command="{Binding SearchCommand}"</code>
/// </summary>
public static class NativeSearch
{
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.CreateAttached(
        "Placeholder", typeof(string), typeof(NativeSearch), null, propertyChanged: OnChanged);

    public static readonly BindableProperty TextProperty = BindableProperty.CreateAttached(
        "Text", typeof(string), typeof(NativeSearch), null,
        defaultBindingMode: BindingMode.TwoWay, propertyChanged: OnChanged);

    public static readonly BindableProperty CommandProperty = BindableProperty.CreateAttached(
        "Command", typeof(ICommand), typeof(NativeSearch), null, propertyChanged: OnChanged);

    public static string? GetPlaceholder(BindableObject o) => (string?)o.GetValue(PlaceholderProperty);
    public static void SetPlaceholder(BindableObject o, string? v) => o.SetValue(PlaceholderProperty, v);

    public static string? GetText(BindableObject o) => (string?)o.GetValue(TextProperty);
    public static void SetText(BindableObject o, string? v) => o.SetValue(TextProperty, v);

    public static ICommand? GetCommand(BindableObject o) => (ICommand?)o.GetValue(CommandProperty);
    public static void SetCommand(BindableObject o, ICommand? v) => o.SetValue(CommandProperty, v);

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue)
    {
#if IOS
        if (bindable is Page page)
        {
            IosNativeSearch.Attach(page);
        }
#endif
    }
}
