using AniLink.Api.Data.Downloads;

namespace AniLink.Api.Archives;

public enum PasswordOutcome
{
    NotRequired,
    Found,
    Missing
}

public sealed record PasswordResult(PasswordOutcome Outcome, string? Password);

public sealed class PasswordResolver(ArchiveExtractor extractor, ILogger<PasswordResolver> logger)
{
    public async Task<PasswordResult> ResolveAsync(IReadOnlyList<string> volumes, DownloadJob job, CancellationToken cancellationToken)
    {
        var listing = await extractor.ListAsync(volumes, null, cancellationToken);
        if (!listing.IsEncrypted) return new PasswordResult(PasswordOutcome.NotRequired, null);

        var candidates = job.Passwords.Candidates();

        foreach (var candidate in candidates)
        {
            if (!await extractor.AcceptsPasswordAsync(volumes, candidate, cancellationToken)) continue;

            job.Passwords.Remember(candidate);
            return new PasswordResult(PasswordOutcome.Found, candidate);
        }

        logger.LogInformation("None of {Count} passwords opened the archive of job {Job}", candidates.Count, job.Id);

        return new PasswordResult(PasswordOutcome.Missing, null);
    }
}