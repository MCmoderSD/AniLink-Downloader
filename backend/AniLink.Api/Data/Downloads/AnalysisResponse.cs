using JetBrains.Annotations;

namespace AniLink.Api.Data.Downloads;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record AnalyzedPart(string FileName, string Url, int PartNumber);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record AnalyzedEpisode(int Number, IReadOnlyList<AnalyzedPart> Parts);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record AnalysisResponse(
    string? Format,
    bool FormatDetected,
    int? Season,
    int LinkCount,
    IReadOnlyList<AnalyzedEpisode> Episodes,
    IReadOnlyList<string> Unmatched
) {
    public static AnalysisResponse From(LinkAnalysis analysis)
    {
        return new AnalysisResponse(
            analysis.Format?.Prefix,
            analysis.FormatDetected,
            analysis.Season,
            analysis.Links.Count,
            [.. analysis.Episodes.Select(episode => new AnalyzedEpisode(
                episode.Number,
                [.. episode.Parts.Select(part => new AnalyzedPart(part.FileName, part.Url, part.PartNumber))]))],
            analysis.Unmatched);
    }
}