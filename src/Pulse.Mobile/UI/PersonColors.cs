namespace Pulse.UI;

/// <summary>
/// Assigns each person in the connection a stable colour derived from their display name, so the same
/// person reads as the same colour everywhere — avatars, their Moment response cards, their Trail nodes,
/// the Home progress avatars. Shares the exact palette + hashing used by <see cref="Controls.AvatarView"/>
/// so an avatar and that person's card always match.
/// </summary>
public static class PersonColors
{
    // Mirror of AvatarView.Palette (background, foreground) pairs.
    private static readonly (Color Bg, Color Fg)[] Palette =
    [
        (Color.FromArgb("#FFE0D1"), Color.FromArgb("#C2470F")),
        (Color.FromArgb("#D9EAC8"), Color.FromArgb("#48702A")),
        (Color.FromArgb("#D4E0F5"), Color.FromArgb("#3A5795")),
        (Color.FromArgb("#F4E3C2"), Color.FromArgb("#94621B")),
        (Color.FromArgb("#E6DCF2"), Color.FromArgb("#6B4E9E")),
        (Color.FromArgb("#CCE9E4"), Color.FromArgb("#2E7468")),
    ];

    /// <summary>The soft background colour for this person (the avatar fill).</summary>
    public static Color Background(string? name) => Pick(name).Bg;

    /// <summary>The stronger foreground colour for this person (initials / accents / node dot).</summary>
    public static Color Foreground(string? name) => Pick(name).Fg;

    /// <summary>
    /// A much paler version of the person colour for card backgrounds, so the full-strength avatar
    /// circle stays distinguishable against it (the mockup's look). ~35% of the colour blended over white.
    /// </summary>
    public static Color CardTint(string? name) => Blend(Pick(name).Bg, Colors.White, 0.35);

    private static Color Blend(Color color, Color over, double amount) => new(
        (float)(color.Red * amount + over.Red * (1 - amount)),
        (float)(color.Green * amount + over.Green * (1 - amount)),
        (float)(color.Blue * amount + over.Blue * (1 - amount)));

    private static (Color Bg, Color Fg) Pick(string? name)
    {
        var key = string.IsNullOrWhiteSpace(name) ? "?" : name.Trim();
        return Palette[Math.Abs(StableHash(key)) % Palette.Length];
    }

    /// <summary>string.GetHashCode is randomised per process; colours must survive restarts. (Matches AvatarView.)</summary>
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
