namespace Pulse.UI;

public static class RelativeTime
{
    /// <summary>Feed-style timestamps: "now", "2m ago", "3h ago", "Yesterday", "4d ago", then a date.</summary>
    public static string Format(DateTimeOffset moment)
    {
        var elapsed = DateTimeOffset.UtcNow - moment;

        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return "now";
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return $"{(int)elapsed.TotalMinutes}m ago";
        }

        if (elapsed < TimeSpan.FromHours(24))
        {
            return $"{(int)elapsed.TotalHours}h ago";
        }

        if (elapsed < TimeSpan.FromHours(48))
        {
            return "Yesterday";
        }

        if (elapsed < TimeSpan.FromDays(7))
        {
            return $"{(int)elapsed.TotalDays}d ago";
        }

        return moment.ToLocalTime().ToString("d MMM");
    }
}
