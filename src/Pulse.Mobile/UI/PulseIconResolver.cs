using MauiIcons.Material.Outlined;

namespace Pulse.UI;

internal static class PulseIconResolver
{
#if IOS
    public static string ResolveCupertino(PulseIcon icon)
    {
        return icon switch
        {
            PulseIcon.Home => "house",
            PulseIcon.Trail => "map",
            PulseIcon.Profile => "person",
            PulseIcon.Back => "chevron.left",
            PulseIcon.Close => "xmark",
            PulseIcon.ChevronRight => "chevron.right",
            PulseIcon.Check => "checkmark",
            PulseIcon.Star => "star",
            PulseIcon.StarFilled => "star.fill",
            PulseIcon.Heart => "heart.fill",
            PulseIcon.HeartOutline => "heart",
            PulseIcon.Lock => "lock",
            PulseIcon.Calendar => "calendar",
            PulseIcon.Clock => "clock",
            PulseIcon.Quote => "quote.bubble",
            PulseIcon.MinusCircle => "minus.circle",
            PulseIcon.Flag => "flag",
            PulseIcon.Lightbulb => "lightbulb",
            PulseIcon.Bug => "ant.circle",
            PulseIcon.Image => "photo",
            PulseIcon.Camera => "camera",
            PulseIcon.Play => "play",
            PulseIcon.Stop => "stop.fill",
            PulseIcon.Sparkles => "sparkles",
            PulseIcon.Plus => "plus",
            PulseIcon.Mood => "face.smiling",
            PulseIcon.Need => "hand.raised",
            PulseIcon.Thought => "heart",
            PulseIcon.Touch => "pencil",
            PulseIcon.Capture => "camera",
            PulseIcon.Draw => "paintbrush",
            PulseIcon.LoveLetter => "envelope",
            PulseIcon.Voice => "mic",
            PulseIcon.Fun => "face.smiling",
            PulseIcon.Adventure => "map",
            PulseIcon.Reflection => "lightbulb",
            PulseIcon.Puzzle => "sparkles",
            PulseIcon.Micro => "sparkles",
            _ => "heart"
        };
    }
#else
    public static MaterialOutlinedIcons ResolveMaterial(PulseIcon icon)
    {
        return icon switch
        {
            PulseIcon.Home => MaterialOutlinedIcons.House,
            PulseIcon.Trail => MaterialOutlinedIcons.Map,
            PulseIcon.Profile => MaterialOutlinedIcons.PersonOutline,
            PulseIcon.Back => MaterialOutlinedIcons.ChevronLeft,
            PulseIcon.Close => MaterialOutlinedIcons.Close,
            PulseIcon.ChevronRight => MaterialOutlinedIcons.ChevronRight,
            PulseIcon.Check => MaterialOutlinedIcons.Check,
            PulseIcon.Star => MaterialOutlinedIcons.StarBorder,
            PulseIcon.StarFilled => MaterialOutlinedIcons.Star,
            PulseIcon.Heart => MaterialOutlinedIcons.Favorite,
            PulseIcon.HeartOutline => MaterialOutlinedIcons.FavoriteBorder,
            PulseIcon.Lock => MaterialOutlinedIcons.Lock,
            PulseIcon.Calendar => MaterialOutlinedIcons.CalendarMonth,
            PulseIcon.Clock => MaterialOutlinedIcons.Schedule,
            PulseIcon.Quote => MaterialOutlinedIcons.FormatQuote,
            PulseIcon.MinusCircle => MaterialOutlinedIcons.RemoveCircleOutline,
            PulseIcon.Flag => MaterialOutlinedIcons.Flag,
            PulseIcon.Lightbulb => MaterialOutlinedIcons.Lightbulb,
            PulseIcon.Bug => MaterialOutlinedIcons.BugReport,
            PulseIcon.Image => MaterialOutlinedIcons.Image,
            PulseIcon.Camera => MaterialOutlinedIcons.PhotoCamera,
            PulseIcon.Play => MaterialOutlinedIcons.PlayArrow,
            PulseIcon.Stop => MaterialOutlinedIcons.Stop,
            PulseIcon.Sparkles => MaterialOutlinedIcons.AutoAwesome,
            PulseIcon.Plus => MaterialOutlinedIcons.Add,
            PulseIcon.Mood => MaterialOutlinedIcons.SentimentSatisfied,
            PulseIcon.Need => MaterialOutlinedIcons.VolunteerActivism,
            PulseIcon.Thought => MaterialOutlinedIcons.FavoriteBorder,
            PulseIcon.Touch => MaterialOutlinedIcons.Edit,
            PulseIcon.Capture => MaterialOutlinedIcons.PhotoCamera,
            PulseIcon.Draw => MaterialOutlinedIcons.Palette,
            PulseIcon.LoveLetter => MaterialOutlinedIcons.MailOutline,
            PulseIcon.Voice => MaterialOutlinedIcons.Mic,
            PulseIcon.Fun => MaterialOutlinedIcons.SentimentSatisfied,
            PulseIcon.Adventure => MaterialOutlinedIcons.DirectionsWalk,
            PulseIcon.Reflection => MaterialOutlinedIcons.Psychology,
            PulseIcon.Puzzle => MaterialOutlinedIcons.Extension,
            PulseIcon.Micro => MaterialOutlinedIcons.AutoAwesome,
            _ => MaterialOutlinedIcons.Favorite
        };
    }
#endif
}