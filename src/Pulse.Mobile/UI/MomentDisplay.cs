using Pulse.Models;
using Pulse.UI.Controls;

namespace Pulse.UI;

/// <summary>
/// Display helpers for Moments: maps a category to its native icon + label for the Trail card and detail,
/// mirroring <see cref="PulseDisplay"/>. The category emoji itself comes from the API (Moment.Emoji).
/// </summary>
public static class MomentDisplay
{
    /// <summary>The native (platform) icon for a moment category.</summary>
    public static PulseIcon CategoryIcon(MomentCategory category) => category switch
    {
        MomentCategory.Capture => PulseIcon.Capture,
        MomentCategory.Draw => PulseIcon.Draw,
        MomentCategory.LoveLetter => PulseIcon.LoveLetter,
        MomentCategory.Voice => PulseIcon.Voice,
        MomentCategory.Fun => PulseIcon.Fun,
        MomentCategory.Adventure => PulseIcon.Adventure,
        MomentCategory.Reflection => PulseIcon.Reflection,
        MomentCategory.Puzzle => PulseIcon.Puzzle,
        MomentCategory.Micro => PulseIcon.Micro,
        _ => PulseIcon.Micro
    };

    public static string CategoryLabel(MomentCategory category) => category switch
    {
        MomentCategory.Capture => "Capture",
        MomentCategory.Draw => "Draw",
        MomentCategory.LoveLetter => "Love Letter",
        MomentCategory.Voice => "Voice",
        MomentCategory.Fun => "Fun",
        MomentCategory.Adventure => "Adventure",
        MomentCategory.Reflection => "Reflection",
        MomentCategory.Puzzle => "Puzzle",
        MomentCategory.Micro => "Micro Moment",
        _ => "Moment"
    };

    /// <summary>The verb shown on the action button, e.g. "Take a photo" / "Start drawing" / "Write".</summary>
    public static string ActionLabel(MomentResponseKind kind) => kind switch
    {
        MomentResponseKind.Photo => "Take a photo",
        MomentResponseKind.Drawing => "Start drawing",
        MomentResponseKind.Text => "Write your answer",
        MomentResponseKind.Voice => "Record",
        _ => "Start Moment"
    };
}
