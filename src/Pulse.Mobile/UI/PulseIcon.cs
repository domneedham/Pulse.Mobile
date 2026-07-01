namespace Pulse.UI;

/// <summary>
/// The finite set of icons Pulse uses. Each maps to a native glyph per platform (Cupertino on iOS,
/// Material Outlined on Android) inside <see cref="AppIcon"/>. Referencing icons by this enum keeps every
/// call site platform-agnostic and gives one place to change a mapping. Brand/logo glyphs (Apple/Google
/// auth, the Pulse mark) intentionally stay on MDI and are NOT here.
/// </summary>
public enum PulseIcon
{
    // Navigation / chrome
    Home,
    Trail,
    Profile,
    Back,
    Close,
    ChevronRight,
    Check,
    Star,
    StarFilled,
    Heart,
    HeartOutline,
    Lock,
    Calendar,
    Clock,
    Quote,
    MinusCircle,
    Flag,
    Lightbulb,
    Bug,
    Image,
    Camera,
    Play,
    Stop,
    Sparkles,

    // Signal categories
    Mood,
    Need,
    Thought,
    Touch,

    // Moment categories
    Capture,
    Draw,
    LoveLetter,
    Voice,
    Fun,
    Adventure,
    Reflection,
    Puzzle,
    Micro,
}