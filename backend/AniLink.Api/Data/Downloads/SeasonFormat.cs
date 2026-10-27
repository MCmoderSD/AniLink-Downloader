using System.Text.RegularExpressions;

namespace AniLink.Api.Data.Downloads;

// The text in front of the episode number, e.g. "Frieren.S01E". An episode is the run of digits directly
// after it, so a link tells which episode it belongs to without any knowledge of the show.
public sealed partial record SeasonFormat(string Prefix)
{
    [GeneratedRegex(@"S\d+E(?=\d)", RegexOptions.IgnoreCase)]
    private static partial Regex EpisodeMarker();

    [GeneratedRegex(@"S(\d+)E$", RegexOptions.IgnoreCase)]
    private static partial Regex SeasonSuffix();

    public int? Season
    {
        get
        {
            var match = SeasonSuffix().Match(Prefix);
            return match.Success && int.TryParse(match.Groups[1].ValueSpan, out var season) ? season : null;
        }
    }

    public bool TryGetEpisode(ArchiveLink link, out int episode)
    {
        // The file name is tried first; the whole URL only matters for a format the user typed that
        // reaches into the path.
        foreach (var haystack in new[] { link.FileName, link.Url })
        {
            var index = haystack.IndexOf(Prefix, StringComparison.OrdinalIgnoreCase);
            if (index < 0) continue;

            var start = index + Prefix.Length;
            var end = start;
            while (end < haystack.Length && char.IsAsciiDigit(haystack[end])) end++;

            if (end > start && int.TryParse(haystack.AsSpan(start, end - start), out episode)) return true;
        }

        episode = 0;
        return false;
    }

    public static SeasonFormat? TryDetect(IReadOnlyList<ArchiveLink> links)
    {
        if (links.Count == 0) return null;

        foreach (var candidate in Candidates(links))
        {
            var format = new SeasonFormat(candidate);
            if (links.All(link => format.TryGetEpisode(link, out _))) return format;
        }

        return null;
    }

    private static IEnumerable<string> Candidates(IReadOnlyList<ArchiveLink> links)
    {
        // Most specific first: the whole name up to the season marker, then the marker alone.
        var marker = EpisodeMarker().Match(links[0].FileName);

        if (marker.Success)
        {
            yield return links[0].FileName[..(marker.Index + marker.Length)];
            yield return marker.Value;
        }

        // No marker: the longest stretch every file name starts with, minus the digits it ends on.
        var common = links[0].FileName;
        foreach (var link in links.Skip(1))
        {
            var length = 0;
            while (length < common.Length && length < link.FileName.Length && common[length] == link.FileName[length]) length++;
            common = common[..length];
        }

        var trimmed = common.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
        if (trimmed.Length > 0) yield return trimmed;
    }
}
