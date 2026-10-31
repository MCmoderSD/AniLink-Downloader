using System.Diagnostics;
using AniLink.Api.Data.Downloads;
using AniLink.Api.Helpers;

namespace AniLink.Api.Archives;

public enum PasswordOutcome
{
    NotRequired,
    Found,
    Missing
}

public sealed record PasswordResult(PasswordOutcome Outcome, string? Password, long UnpackedBytes = 0);

public sealed class PasswordResolver(ArchiveExtractor extractor, ILogger<PasswordResolver> logger)
{
    public async Task<PasswordResult> ResolveAsync(IReadOnlyList<string> volumes, DownloadJob job, string label, CancellationToken cancellationToken)
    {
        var clock = Stopwatch.StartNew();

        var listing = await extractor.ListAsync(volumes, null, cancellationToken);
        var files = listing.Entries.Where(entry => !entry.IsFolder).ToList();

        logger.LogInformation(
            "Listing the archive of {Label} took {Elapsed}: {Files} files, {Size} unpacked, encrypted: {Encrypted}",
            label, DurationText.Format(clock.Elapsed), files.Count, ByteSize.Format(files.Sum(entry => entry.Size)), listing.IsEncrypted ? "yes" : "no");

        var unpacked = files.Sum(entry => entry.Size);

        if (!listing.IsEncrypted) return new PasswordResult(PasswordOutcome.NotRequired, null, unpacked);

        var candidates = job.Passwords.Candidates();
        var tried = 0;

        foreach (var candidate in candidates)
        {
            tried++;

            if (!await extractor.AcceptsPasswordAsync(volumes, candidate, cancellationToken)) continue;

            job.Passwords.Remember(candidate);
            logger.LogInformation("Password {Number} of {Count} opened the archive of {Label} after {Elapsed}", tried, candidates.Count, label, DurationText.Format(clock.Elapsed));

            return new PasswordResult(PasswordOutcome.Found, candidate, unpacked);
        }

        logger.LogInformation("None of {Count} passwords opened the archive of {Label} ({Elapsed})", candidates.Count, label, DurationText.Format(clock.Elapsed));

        return new PasswordResult(PasswordOutcome.Missing, null);
    }
}