using System.Diagnostics;
using AniLink.Api.Exceptions.Archives;
using AniLink.Api.Helpers;

namespace AniLink.Api.Archives;

public sealed record ArchiveEntry(string Path, long Size, bool Encrypted, bool IsFolder);

public sealed record ArchiveListing(bool Locked, IReadOnlyList<ArchiveEntry> Entries)
{
    public bool IsEncrypted => Locked || Entries.Any(entry => entry is { IsFolder: false, Encrypted: true });
}

public sealed class ArchiveExtractor(SevenZipProcess sevenZip, ILogger<ArchiveExtractor> logger)
{
    private const int SniffLength = 16;

    private static readonly TimeSpan FirstLook = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Heartbeat = TimeSpan.FromSeconds(20);

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
        IReadOnlyList<string> volumes, string outputDirectory, string? password, string label, Action<double> onProgress, CancellationToken cancellationToken)
    {
        var first = RequireVolumes(volumes);

        Directory.CreateDirectory(outputDirectory);

        string[] arguments = ["x", "-y", "-aoa", "-bsp1", "-bb0", $"-o{outputDirectory}", .. PasswordArguments(password), first];

        var activity = new ToolActivity();
        var clock = Stopwatch.StartNew();

        logger.LogInformation("Starting 7-Zip for {Label}: {Command}", label, Describe(arguments));

        using var stopBeat = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var beat = BeatAsync(label, outputDirectory, activity, clock, stopBeat.Token);

        ToolResult result;

        try
        {
            result = await sevenZip.RunAsync(arguments, onProgress, cancellationToken, activity);
        }
        finally
        {
            await stopBeat.CancelAsync();
            await beat;
        }

        logger.LogInformation(
            "7-Zip ended for {Label} with exit code {Code} after {Elapsed}: {Written} written, last progress {Percent:0}%, {Chunks} progress messages",
            label, result.ExitCode, DurationText.Format(clock.Elapsed), ByteSize.Format(DirectorySize(outputDirectory)), activity.Percent, activity.Chunks);

        if (result.ExitCode is 0 or 1)
        {
            if (result.ExitCode is 1) logger.LogWarning("7-Zip reported warnings for {Label}: {Output}", label, LogSafe.Excerpt(result.Combined, 800, true));

            onProgress(100);
            return;
        }

        logger.LogWarning("7-Zip failed for {Label}: {Output}", label, LogSafe.Excerpt(result.Combined, 800, true));

        throw new ExtractionException($"Unpacking failed: {Summarize(result, first)}") { PasswordProblem = IsPasswordProblem(result.Combined) };
    }

    // Says what 7-Zip has done while it unpacks, because its own percentage can stand still for a long time on a big
    // archive. What it printed, how much has reached the disk and for how long: those three tell apart a tool that is
    // busy, one that says nothing, and one whose output is not understood.
    private async Task BeatAsync(string label, string outputDirectory, ToolActivity activity, Stopwatch clock, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(FirstLook, cancellationToken);

            logger.LogInformation("7-Zip output for {Label} begins with: {Output}", label, activity.Chunks == 0 ? "nothing yet" : LogSafe.Excerpt(activity.First, 600));

            var seen = DirectorySize(outputDirectory);
            var since = clock.Elapsed;
            var cpu = activity.CpuTime;

            using var timer = new PeriodicTimer(Heartbeat);

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                var written = DirectorySize(outputDirectory);
                var seconds = (clock.Elapsed - since).TotalSeconds;
                var used = activity.CpuTime;

                // Close to 100% means 7-Zip computes as fast as one thread can; far below, it waits for the disk
                var load = cpu is { } before && used is { } after && seconds > 0 ? $"{(after - before).TotalSeconds / seconds * 100:0}%" : "unknown";

                logger.LogInformation(
                    "7-Zip is unpacking {Label}: {Percent:0}% reported, {Written} written ({Rate}/s), CPU {Load} of one thread, running for {Elapsed}, {Chunks} progress messages, last: {Last}",
                    label,
                    activity.Percent,
                    ByteSize.Format(written),
                    ByteSize.Format(seconds > 0 ? (long)((written - seen) / seconds) : 0),
                    load,
                    DurationText.Format(clock.Elapsed),
                    activity.Chunks,
                    LogSafe.Excerpt(activity.Tail, 80, true));

                seen = written;
                since = clock.Elapsed;
                cpu = used;
            }
        }
        catch (OperationCanceledException)
        {
            // The unpacking ended or was stopped.
        }
    }

    private string Describe(IEnumerable<string> arguments)
    {
        return sevenZip.Executable + " " + string.Join(' ', arguments.Select(argument => argument.StartsWith("-p", StringComparison.Ordinal) ? "-p***" : LogSafe.OneLine(argument)));
    }

    private static long DirectorySize(string directory)
    {
        try
        {
            return Directory.Exists(directory) ? new DirectoryInfo(directory).EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length) : 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return 0;
        }
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