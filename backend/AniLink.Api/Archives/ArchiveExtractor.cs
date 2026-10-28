using AniLink.Api.Exceptions.Archives;

namespace AniLink.Api.Archives;

public sealed record ArchiveEntry(string Path, long Size, bool Encrypted, bool IsFolder);

public sealed record ArchiveListing(bool Locked, IReadOnlyList<ArchiveEntry> Entries)
{
    public bool IsEncrypted => Locked || Entries.Any(entry => entry is { IsFolder: false, Encrypted: true });
}

public sealed class ArchiveExtractor(SevenZipProcess sevenZip)
{
    private const int SniffLength = 16;

    public async Task<ArchiveListing> ListAsync(IReadOnlyList<string> volumes, string? password, CancellationToken cancellationToken)
    {
        var first = RequireVolumes(volumes);

        var result = await sevenZip.RunAsync(["l", "-slt", "-y", "-bd", .. PasswordArguments(password), first], null, cancellationToken);

        if (result.ExitCode is not (0 or 1))
        {
            if (IsPasswordProblem(result.Combined)) return new ArchiveListing(true, []);

            throw new ExtractionException($"The archive could not be read: {Summarize(result, first)}");
        }

        return new ArchiveListing(false, SevenZipListing.Parse(result.Output));
    }

    public async Task<bool> AcceptsPasswordAsync(IReadOnlyList<string> volumes, string password, CancellationToken cancellationToken)
    {
        var first = RequireVolumes(volumes);

        ArchiveListing listing;

        try
        {
            listing = await ListAsync(volumes, password, cancellationToken);
        }
        catch (ExtractionException)
        {
            return false;
        }

        if (listing.Locked) return false;

        var entry = listing.Entries
            .Where(candidate => !candidate.IsFolder)
            .OrderByDescending(candidate => MediaSignature.IsKnownVideo(candidate.Path))
            .ThenByDescending(candidate => candidate.Size)
            .FirstOrDefault();

        if (entry is null) return false;
        if (!entry.Encrypted) return true;

        var head = await sevenZip.ReadHeadAsync(["e", "-so", "-y", "-bd", .. PasswordArguments(password), first, entry.Path], SniffLength, cancellationToken);

        return head is not null && MediaSignature.Matches(entry.Path, head);
    }

    public async Task ExtractAsync(
        IReadOnlyList<string> volumes, string outputDirectory, string? password, Action<double> onProgress, CancellationToken cancellationToken)
    {
        var first = RequireVolumes(volumes);

        Directory.CreateDirectory(outputDirectory);

        var result = await sevenZip.RunAsync(
            ["x", "-y", "-aoa", "-bsp1", "-bb0", $"-o{outputDirectory}", .. PasswordArguments(password), first],
            onProgress,
            cancellationToken);

        if (result.ExitCode is 0 or 1)
        {
            onProgress(100);
            return;
        }

        throw new ExtractionException($"Unpacking failed: {Summarize(result, first)}") { PasswordProblem = IsPasswordProblem(result.Combined) };
    }

    private static string RequireVolumes(IReadOnlyList<string> volumes)
    {
        if (volumes.Count == 0) throw new ExtractionException("There is no archive file to unpack.");

        var missing = volumes.FirstOrDefault(volume => !File.Exists(volume));
        if (missing is not null) throw new ExtractionException($"The archive part {Path.GetFileName(missing)} is missing.");

        return volumes[0];
    }

    private static string[] PasswordArguments(string? password)
    {
        return string.IsNullOrEmpty(password) ? [] : ["-p" + password];
    }

    private static bool IsPasswordProblem(string text)
    {
        return text.Contains("wrong password", StringComparison.OrdinalIgnoreCase)
               || text.Contains("enter password", StringComparison.OrdinalIgnoreCase)
               || text.Contains("encrypted archive", StringComparison.OrdinalIgnoreCase)
               || text.Contains("CRC Failed in encrypted", StringComparison.OrdinalIgnoreCase);
    }

    private static string Summarize(ToolResult result, string archive)
    {
        var folder = (Path.GetDirectoryName(archive) ?? "") + Path.DirectorySeparatorChar;

        var lines = result.Combined
            .Replace(folder, "", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.Contains("ERROR", StringComparison.OrdinalIgnoreCase)
                           || line.Contains("Can not", StringComparison.OrdinalIgnoreCase)
                           || line.Contains("Unexpected", StringComparison.OrdinalIgnoreCase)
                           || line.Contains("password", StringComparison.OrdinalIgnoreCase)
                           || line.Contains("Missing", StringComparison.OrdinalIgnoreCase))
            .Take(3);

        var summary = string.Join(" ", lines);

        if (summary.Length == 0) summary = $"7-Zip exited with code {result.ExitCode}";

        return summary.Length <= 300 ? summary : summary[..300] + "…";
    }
}