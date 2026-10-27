using AniLink.Api.Data.Downloads;

namespace AniLink.Api.Services;

// Pure logic, no state: pasted lines in, episodes with their ordered parts out. The create endpoint runs the
// same analysis again on the server, so the preview a user confirmed can never differ from what is queued.
public static class LinkAnalyzer
{
    public static LinkAnalysis Analyze(string? text, ReleaseKind kind, string? requestedFormat)
    {
        var links = Clean(text);
        var unmatched = links.Where(link => !ArchiveLink.IsWebUrl(link)).ToList();
        var parsed = links.Where(ArchiveLink.IsWebUrl).Select(ArchiveLink.Parse).ToList();

        // A film or a single episode is one release whatever the file names look like.
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

    // Order is what makes a multipart archive extractable: SharpCompress is handed the volumes in this order.
    public static IReadOnlyList<ArchiveLink> Sort(IEnumerable<ArchiveLink> parts)
    {
        // The same part pasted from two hosters would land in one folder under one name; the first one wins.
        // Links without an archive name (hosters that only put an id in the URL) cannot be told apart this way.
        var distinct = parts.Where(part => !part.LooksLikeArchive).Concat(parts.Where(part => part.LooksLikeArchive).DistinctBy(part => part.FileName, StringComparer.OrdinalIgnoreCase));

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
