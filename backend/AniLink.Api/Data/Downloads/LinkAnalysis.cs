namespace AniLink.Api.Data.Downloads;

public sealed record EpisodeGroup(int Number, IReadOnlyList<ArchiveLink> Parts);

public sealed record LinkAnalysis(
    IReadOnlyList<string> Links,
    SeasonFormat? Format,
    bool FormatDetected,
    int? Season,
    IReadOnlyList<EpisodeGroup> Episodes,
    IReadOnlyList<string> Unmatched
);
