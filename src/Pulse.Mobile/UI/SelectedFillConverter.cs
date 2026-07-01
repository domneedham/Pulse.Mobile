using System.Globalization;

namespace Pulse.UI;

/// <summary>
/// Maps a bool (is-selected) to a fill colour for selectable chips: the soft brand wash when selected,
/// transparent otherwise. Used by the filter sheet's type chips.
/// </summary>
public class SelectedFillConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var selected = value is true;
        var key = selected ? "BrandSoft" : "SurfaceAlt";
        return Application.Current?.Resources.TryGetValue(key, out var color) == true && color is Color c
            ? c
            : Colors.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
