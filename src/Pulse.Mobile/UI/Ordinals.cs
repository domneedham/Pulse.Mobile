namespace Pulse.UI;

public static class Ordinals
{
    /// <summary>1 → "1st", 2 → "2nd", 11 → "11th" …</summary>
    public static string Format(int value)
    {
        var suffix = (value % 100) switch
        {
            11 or 12 or 13 => "th",
            _ => (value % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th",
            },
        };

        return $"{value}{suffix}";
    }
}
