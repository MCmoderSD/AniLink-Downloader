using System.Text.RegularExpressions;

namespace AniLink.Api.Data.Downloads;

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
        return links.Count == 0 ? null : Candidates(links).Select(candidate => new SeasonFormat(candidate)).FirstOrDefault(format => links.All(link => format.TryGetEpisode(link, out _)));
    }

    private static IEnumerable<string> Candidates(IReadOnlyList<ArchiveLink> links)
    {
        var marker = EpisodeMarker().Match(links[0].FileName);

        if (marker.Success)
        {
            yield return links[0].FileName[..(marker.Index + marker.Length)];
            yield return marker.Value;
        }

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