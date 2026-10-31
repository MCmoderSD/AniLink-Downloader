using AniLink.Api.Data.Downloads;

namespace AniLink.Api.Services;

public static class LinkAnalyzer
{
    public static LinkAnalysis Analyze(string? text, ReleaseKind kind, string? requestedFormat)
    {
        var links = Clean(text);
        var unmatched = links.Where(link => !ArchiveLink.IsWebUrl(link)).ToList();
        var parsed = links.Where(ArchiveLink.IsWebUrl).Select(ArchiveLink.Parse).ToList();

        if (kind is ReleaseKind.Movie)
        {
            return new LinkAnalysis(links, null, false, null, parsed.Count == 0 ? [] : [new EpisodeGroup(0, Sort(parsed))], unmatched);
        }

        var typed = string.IsNullOrWhiteSpace(requestedFormat) ? null : new SeasonFormat(requestedFormat.Trim());
        var format = typed ?? SeasonFormat.TryDetect(parsed);

        if (format is null) return new LinkAnalysis(links, null, false, null, [], [.. unmatched, .. parsed.Select(link => link.Url)]);

        var episodes = new SortedDictionary<int, List<ArchiveLink>>();

        foreach (var link in parsed)
        {
            if (!format.TryGetEpisode(link, out var episode))
            {
                unmatched.Add(link.Url);
                continue;
            }

            if (!episodes.TryGetValue(episode, out var parts)) episodes[episode] = parts = [];
            parts.Add(link);
        }

        return new LinkAnalysis(
            links,
            format,
            typed is null,
            format.Season,
            [.. episodes.Select(episode => new EpisodeGroup(episode.Key, Sort(episode.Value)))],
            unmatched);
    }

    private static IReadOnlyList<ArchiveLink> Sort(IEnumerable<ArchiveLink> parts)
    {
        var archiveLinks = parts as ArchiveLink[] ?? [.. parts];
        var distinct = archiveLinks.Where(part => !part.LooksLikeArchive).Concat(archiveLinks.Where(part => part.LooksLikeArchive).DistinctBy(part => part.FileName, StringComparer.OrdinalIgnoreCase));

        return [.. distinct.OrderBy(part => part.PartNumber).ThenBy(part => part.FileName, StringComparer.OrdinalIgnoreCase)];
    }

    private static List<string> Clean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        return
        [
            .. text
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)
        ];
    }
}