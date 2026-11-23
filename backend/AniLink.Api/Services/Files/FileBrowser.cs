using AniLink.Api.Data.Downloads;
using AniLink.Api.Data.Files;
using AniLink.Api.Helpers;
using AniLink.Api.Repositories;

namespace AniLink.Api.Services.Files;

public sealed class FileBrowser(
    FilePaths paths,
    FolderTotals totals,
    FileChanges changes,
    JobRepository jobs,
    WorkspaceGate gate,
    ILogger<FileBrowser> logger
) {
    private static readonly EnumerationOptions Everything = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = 0
    };

    public ListingOutcome List(string? relative)
    {
        return paths.Resolve(relative, false) switch
        {
            PathInvalid invalid => new ListingInvalid(invalid.Reason),
            PathMissing => new ListingMissing(),
            PathResolved resolved => Read(resolved)
        };
    }

    public TreeOutcome Tree(string? relative)
    {
        return paths.Resolve(relative, false) switch
        {
            PathInvalid invalid => new ListingInvalid(invalid.Reason),
            PathMissing => new ListingMissing(),
            PathResolved resolved => ReadTree(resolved)
        };
    }

    public FileBatchResponse Delete(IReadOnlyList<string> requested)
    {
        var results = requested.Distinct(StringComparer.Ordinal).Select(Delete).ToList();

        logger.LogInformation(
            "Deleted {Done} of {Total} items of the downloads folder, {Locked} in use, {Failed} failed",
            results.Count(result => result.Outcome is FileOutcome.Done),
            results.Count,
            results.Count(result => result.Outcome is FileOutcome.Locked),
            results.Count(result => result.Outcome is FileOutcome.Failed));

        return new FileBatchResponse(results);
    }

    private FileResultResponse Delete(string relative)
    {
        return paths.Resolve(relative, true) switch
        {
            PathInvalid invalid => Result(relative, FileOutcome.Invalid, invalid.Reason),
            PathMissing => Result(relative, FileOutcome.Missing, "It is no longer there."),
            PathResolved resolved => Remove(resolved)
        };
    }

    private FileResultResponse Remove(PathResolved resolved)
    {
        using var claim = gate.TryClaim(resolved.Full, out var blockers);

        if (claim is null)
        {
            if (blockers.Count == 0) return Result(resolved.Relative, FileOutcome.Failed, "It is being deleted already.");

            var locks = blockers.Select(job => new FileLockResponse(job.Id, job.Name, job.Release.Season, job.Status)).ToList();
            var names = string.Join(", ", blockers.Select(job => job.Release.Season is { } season ? $"{job.Name} (season {season})" : job.Name));

            return new FileResultResponse(resolved.Relative, FileOutcome.Locked, $"It is in use by {names}.", locks);
        }

        // The checks of the path ran before the claim. Running them again narrows the time in which a folder could be
        // replaced by a link, but cannot close it: only the app is supposed to write into the downloads folder.
        if (paths.Resolve(resolved.Relative, true) is not PathResolved again || !string.Equals(again.Full, resolved.Full, StringComparison.Ordinal))
        {
            return Result(resolved.Relative, FileOutcome.Invalid, "The path changed while it was being checked.");
        }

        try
        {
            Erase(resolved.Full);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return Result(resolved.Relative, FileOutcome.Missing, "It is no longer there.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "{Path} could not be deleted", LogSafe.OneLine(resolved.Relative));

            return Result(resolved.Relative, FileOutcome.Failed, exception is UnauthorizedAccessException ? "It cannot be deleted: access is denied." : "It cannot be deleted: another program is using it.");
        }

        totals.Invalidate(resolved.Full);
        changes.Report(resolved.Full);

        logger.LogInformation("Deleted {Path}", LogSafe.OneLine(resolved.Relative));

        return Result(resolved.Relative, FileOutcome.Done);
    }

    private static void Erase(string full)
    {
        var attributes = File.GetAttributes(full);

        if (attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            if (OperatingSystem.IsWindows() && attributes.HasFlag(FileAttributes.Directory)) Directory.Delete(full, false);
            else File.Delete(full);

            return;
        }

        try
        {
            EraseTree(full, attributes.HasFlag(FileAttributes.Directory));
        }
        catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows())
        {
            ClearReadOnly(full);
            EraseTree(full, attributes.HasFlag(FileAttributes.Directory));
        }
    }

    private static void EraseTree(string full, bool directory)
    {
        if (directory) Directory.Delete(full, true);
        else File.Delete(full);
    }

    private static void ClearReadOnly(string full)
    {
        if (File.Exists(full))
        {
            File.SetAttributes(full, File.GetAttributes(full) & ~FileAttributes.ReadOnly);
            return;
        }

        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint };

        foreach (var path in Directory.EnumerateFileSystemEntries(full, "*", options))
        {
            var attributes = File.GetAttributes(path);
            if (attributes.HasFlag(FileAttributes.ReadOnly)) File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
        }
    }

    private static FileResultResponse Result(string path, FileOutcome outcome, string? reason = null)
    {
        return new FileResultResponse(path, outcome, reason, []);
    }

    private ListingOutcome Read(PathResolved resolved)
    {
        if (!Directory.Exists(resolved.Full))
        {
            return File.Exists(resolved.Full) ? new ListingInvalid("The path is a file, not a folder.") : new ListingMissing();
        }

        var active = jobs.ListActive().Select(job => (Job: job, Root: Path.GetFullPath(job.Workspace.Root))).ToList();

        try
        {
            var entries = new DirectoryInfo(resolved.Full)
                .EnumerateFileSystemInfos("*", Everything)
                .Where(entry => !FilePaths.IsInternal(entry.Name))
                .Select(entry => Describe(entry, resolved.Relative, active))
                .ToList();

            return new ListingFound(new FolderListingResponse(resolved.Relative, entries));
        }
        catch (DirectoryNotFoundException)
        {
            return new ListingMissing();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "The folder {Path} could not be read", LogSafe.OneLine(resolved.Relative));

            return new ListingUnavailable("The folder cannot be read.");
        }
    }

    private TreeOutcome ReadTree(PathResolved resolved)
    {
        if (!Directory.Exists(resolved.Full))
        {
            return File.Exists(resolved.Full) ? new ListingInvalid("The path is a file, not a folder.") : new ListingMissing();
        }

        try
        {
            var nodes = new DirectoryInfo(resolved.Full)
                .EnumerateFileSystemInfos("*", Everything)
                .Where(entry => !FilePaths.IsInternal(entry.Name))
                .Select(entry => Node(entry, resolved.Relative))
                .ToList();

            return new TreeFound(new TreeResponse(resolved.Relative, nodes));
        }
        catch (DirectoryNotFoundException)
        {
            return new ListingMissing();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "The folder {Path} could not be read", LogSafe.OneLine(resolved.Relative));

            return new ListingUnavailable("The folder cannot be read.");
        }
    }

    private static TreeNodeResponse Node(FileSystemInfo entry, string parent)
    {
        var path = parent.Length == 0 ? entry.Name : $"{parent}/{entry.Name}";

        if (entry.Attributes.HasFlag(FileAttributes.ReparsePoint)) return new TreeNodeResponse(entry.Name, path, FileEntryKind.Link, false);
        if (entry is FileInfo) return new TreeNodeResponse(entry.Name, path, FileEntryKind.File, false);

        return new TreeNodeResponse(entry.Name, path, FileEntryKind.Folder, HasChildren(entry.FullName));
    }

    private static bool HasChildren(string directory)
    {
        try
        {
            return Directory.EnumerateFileSystemEntries(directory, "*", Everything).Any(entry => !FilePaths.IsInternal(Path.GetFileName(entry)));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private FileEntryResponse Describe(FileSystemInfo entry, string parent, List<(DownloadJob Job, string Root)> active)
    {
        var locks = active
            .Where(candidate => PathScope.Overlaps(entry.FullName, candidate.Root, WorkspaceGate.Comparison))
            .Select(candidate => new FileLockResponse(candidate.Job.Id, candidate.Job.Name, candidate.Job.Release.Season, candidate.Job.Status))
            .ToList();

        var path = parent.Length == 0 ? entry.Name : $"{parent}/{entry.Name}";
        var modified = new DateTimeOffset(entry.LastWriteTimeUtc, TimeSpan.Zero);

        if (entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            return new FileEntryResponse(entry.Name, path, FileEntryKind.Link, 0, null, modified, locks);
        }

        if (entry is FileInfo file)
        {
            return new FileEntryResponse(entry.Name, path, FileEntryKind.File, file.Length, null, modified, locks);
        }

        var inside = totals.Measure(entry.FullName);

        return new FileEntryResponse(entry.Name, path, FileEntryKind.Folder, inside.Bytes, inside.Files, modified, locks);
    }
}