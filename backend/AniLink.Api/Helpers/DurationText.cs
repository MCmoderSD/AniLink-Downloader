using System.Globalization;

namespace AniLink.Api.Helpers;

public static class DurationText
{
    public static string Format(TimeSpan span)
    {
        if (span.TotalHours >= 1) return $"{(int)span.TotalHours}h {span.Minutes}m {span.Seconds}s";
        if (span.TotalMinutes >= 1) return $"{span.Minutes}m {span.Seconds}s";

        return span.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + "s";
    }
}