using Microsoft.Maui.Graphics;

namespace Pulse.UI;

/// <summary>
/// A pickable "fun avatar": an emoji on a two-tone gradient. The same spec both renders the
/// preview tile and produces the PNG bytes uploaded to storage, so picking one exercises the
/// real upload flow (a placeholder until on-device photo capture lands with a paid dev account).
/// </summary>
public record PresetAvatar(string Id, string Emoji, Color Top, Color Bottom);

public static class PresetAvatars
{
    public static readonly IReadOnlyList<PresetAvatar> All =
    [
        new("flame", "🔥", Color.FromArgb("#FF8A3D"), Color.FromArgb("#E2480F")),
        new("rocket", "🚀", Color.FromArgb("#6FB1FC"), Color.FromArgb("#3A5795")),
        new("trophy", "🏆", Color.FromArgb("#FFD66B"), Color.FromArgb("#D9A006")),
        new("muscle", "💪", Color.FromArgb("#7CD992"), Color.FromArgb("#2E7468")),
        new("bolt", "⚡", Color.FromArgb("#FBE07A"), Color.FromArgb("#C29A12")),
        new("star", "⭐", Color.FromArgb("#C7A8F5"), Color.FromArgb("#6B4E9E")),
        new("run", "🏃", Color.FromArgb("#FF9FB2"), Color.FromArgb("#C2456A")),
        new("book", "📚", Color.FromArgb("#9AD0C2"), Color.FromArgb("#2E7468")),
        new("mountain", "⛰️", Color.FromArgb("#A7C7E7"), Color.FromArgb("#3A5795")),
        new("dog", "🐶", Color.FromArgb("#E8C9A0"), Color.FromArgb("#94621B")),
        new("cat", "🐱", Color.FromArgb("#F4B8C2"), Color.FromArgb("#94457A")),
        new("alien", "👾", Color.FromArgb("#A8E6CF"), Color.FromArgb("#2E7468")),
    ];
}
