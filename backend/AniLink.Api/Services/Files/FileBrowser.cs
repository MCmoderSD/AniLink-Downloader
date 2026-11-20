using AniLink.Api.Data.Downloads;
using AniLink.Api.Data.Files;
using AniLink.Api.Helpers;
using AniLink.Api.Repositories;

namespace AniLink.Api.Services.Files;

public sealed class FileBrowser(
    FilePaths paths,
    FolderTotals totals,
    JobRepository jobs,
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

            return new ListingFound(new FolderListingResponse(resolved.Relative, paths.Root, entries));
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

    private FileEntryResponse Describe(FileSystemInfo entry, string parent, List<(DownloadJob Job, string Root)> active)
    {
        var locks = active
            .Where(candidate => PathScope.Overlaps(entry.FullName, candidate.Root, StringComparison.OrdinalIgnoreCase))
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