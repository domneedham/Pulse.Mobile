using Pulse.Models;
using Pulse.Resources;
using Pulse.UI.Controls;

namespace Pulse.UI;

/// <summary>
/// Display helpers for pulses. Each pulse carries its own phrase + emoji, but a displayed signal is
/// denoted by its CATEGORY (Mood/Need/Thought/Touch) MDI glyph — not the per-phrase emoji — to keep the
/// UI clean and low on emoji. This provides that glyph, the per-category fallback emoji (used only when
/// sending a custom phrase without one), and the category label.
/// </summary>
public static class PulseDisplay
{
    /// <summary>The native (platform) icon denoting a signal's category — used wherever a signal is shown.</summary>
    public static PulseIcon CategoryIcon(PulseType category) => category switch
    {
        PulseType.Mood => PulseIcon.Mood,
        PulseType.Need => PulseIcon.Need,
        PulseType.Thought => PulseIcon.Thought,
        PulseType.Touch => PulseIcon.Touch,
        _ => PulseIcon.Thought
    };

    public static string DefaultEmoji(PulseType category) => category switch
    {
        PulseType.Mood => "🙂",
        PulseType.Need => "💗",
        PulseType.Thought => "💬",
        PulseType.Touch => "✏️",
        _ => "💗"
    };

    public static string CategoryLabel(PulseType category) => category switch
    {
        PulseType.Mood => "Mood",
        PulseType.Need => "Need",
        PulseType.Thought => "Thought",
        PulseType.Touch => "Touch",
        _ => "Pulse"
    };
}
