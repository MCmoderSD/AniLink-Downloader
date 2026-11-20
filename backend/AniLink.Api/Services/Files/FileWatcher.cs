using System.ComponentModel;
using AniLink.Api.Helpers;

namespace AniLink.Api.Services.Files;

public sealed class FileWatcher(FilePaths paths, FileChanges changes, ILogger<FileWatcher> logger) : BackgroundService
{
    private static readonly TimeSpan MissingRoot = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan AfterFailure = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan AfterLimit = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (true)
            {
                var retry = await WatchAsync(stoppingToken);

                await Task.Delay(retry, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private async Task<TimeSpan> WatchAsync(CancellationToken stoppingToken)
    {
        if (!Directory.Exists(paths.Root)) return MissingRoot;

        var failure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var watcher = new FileSystemWatcher(paths.Root)
        {
            IncludeSubdirectories = true,
            InternalBufferSize = 64 * 1024,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.LastWrite
        };

        watcher.Created += (_, args) => changes.Report(args.FullPath);
        watcher.Changed += (_, args) => changes.Report(args.FullPath);
        watcher.Deleted += (_, args) => changes.Report(args.FullPath);

        watcher.Renamed += (_, args) =>
        {
            changes.Report(args.OldFullPath);
            changes.Report(args.FullPath);
        };

        watcher.Error += (_, args) =>
        {
            if (args.GetException() is InternalBufferOverflowException) changes.Reset();
            else failure.TrySetResult(args.GetException());
        };

        try
        {
            watcher.EnableRaisingEvents = true;
        }
        catch (Exception exception) when (exception is IOException or Win32Exception or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "The downloads folder cannot be watched, probably because the limit of watched folders is reached (fs.inotify.max_user_watches on Linux). The Files page reads the folder again every few seconds instead.");

            return AfterLimit;
        }

        changes.Reset();

        await Task.WhenAny(failure.Task, Task.Delay(Timeout.Infinite, stoppingToken));
        stoppingToken.ThrowIfCancellationRequested();

        logger.LogWarning(await failure.Task, "Watching the downloads folder stopped, trying again in {Seconds} s", AfterFailure.TotalSeconds);

        changes.Reset();

        return AfterFailure;
    }
}