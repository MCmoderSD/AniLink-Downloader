using System.Collections.Concurrent;
using AniLink.Api.Archives;
using AniLink.Api.Data.Downloads;
using AniLink.Api.Exceptions.Archives;
using AniLink.Api.Exceptions.DebridLink;
using AniLink.Api.Exceptions.Downloads;
using AniLink.Api.Helpers;
using AniLink.Api.Repositories;
using AniLink.Api.Services.Streaming;

namespace AniLink.Api.Services;

// Drives every item of a job through its stages: links, downloads, unpacking, organizing. Each item is one
// async workflow that waits on the shared queues (the link resolver, the download limit, the extraction slot),
// so the order, the limits and the cancellation all come from those queues instead of from bookkeeping here.
public sealed class DownloadPipeline(
    LinkResolver resolver,
    DownloadScheduler scheduler,
    PartDownloader downloader,
    PasswordResolver passwords,
    ArchiveExtractor extractor,
    MediaOrganizer organizer,
    JobRepository jobs,
    ProgressHub hub,
    ILogger<DownloadPipeline> logger)
{
    private const int MaxAttempts = 3;

    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runs = new();

    // Starts the given items of a job (all unfinished ones by default). Returns at once; the work runs on.
    public void Start(DownloadJob job, IEnumerable<MediaItem>? only = null)
    {
        var items = (only ?? job.Items).Where(item => !item.IsTerminal).OrderBy(item => item.Number).ToList();
        var source = _runs.GetOrAdd(job.Id, _ => new CancellationTokenSource());

        // ToList runs each workflow up to its first await right here, in episode order. That is what puts
        // the first episode ahead of the second in the resolver queue (see ProcessAsync).
        var tasks = items.Select(item => ProcessAsync(job, item, source.Token)).ToList();

        _ = FinishAsync(job, tasks);
    }

    public bool Retry(DownloadJob job)
    {
        var items = job.Items.Where(item => item.ResetForRetry()).ToList();
        if (items.Count == 0) return false;

        Notify(job, NoticeLevel.Info, $"Retrying {items.Count} {(items.Count == 1 ? "item" : "items")}");
        Start(job, items);

        return true;
    }

    public bool SubmitPassword(DownloadJob job, string password)
    {
        var waiting = job.Items.Where(item => item.Stage is ItemStage.PasswordRequired).ToList();
        if (waiting.Count == 0) return false;

        job.Passwords.Add(password);
        foreach (var item in waiting) item.BeginDownloading();

        Notify(job, NoticeLevel.Info, $"Trying the new password on {waiting.Count} {(waiting.Count == 1 ? "item" : "items")}");
        Start(job, waiting);

        return true;
    }

    public void Cancel(DownloadJob job)
    {
        foreach (var item in job.Items) item.Cancel();

        if (_runs.TryRemove(job.Id, out var source)) source.Cancel();

        Notify(job, NoticeLevel.Warning, $"{job.Name}: cancelled");
    }

    private async Task ProcessAsync(DownloadJob job, MediaItem item, CancellationToken cancellationToken)
    {
        using var run = new ItemRun(cancellationToken);

        // Nothing above this line may await: the links are queued here, and the resolver serves its queue in the
        // order requests arrive, so an item that is started earlier gets all of its links earlier.
        var pending = item.Parts.Where(part => !part.HasLink).ToList();
        var resolutions = pending.Select(part => resolver.ResolveAsync(part, false, run.Token)).ToList();

        try
        {
            if (pending.Count > 0) await AwaitLinksAsync(job, item, pending, resolutions);

            await DownloadAsync(job, item, run);
            await UnpackAsync(job, item, run.Token);
        }
        catch (Exception exception)
        {
            run.Abort(exception);
            HandleFailure(job, item, run.Failure ?? exception, cancellationToken);
        }
        finally
        {
            await jobs.SaveAsync(job);
        }
    }

    private async Task AwaitLinksAsync(DownloadJob job, MediaItem item, List<ArchivePart> pending, List<Task> resolutions)
    {
        item.BeginResolving();

        for (var index = 0; index < pending.Count; index++)
        {
            try
            {
                await resolutions[index];
            }
            catch (DebridLinkException exception)
            {
                throw new DebridLinkException(exception.Code, $"{pending[index].Link.FileName}: {exception.Message}", exception.Retryable, exception);
            }

            // Every link is on disk before the next one is awaited, so a restart loses at most one.
            await jobs.SaveAsync(job);
        }

        Notify(job, NoticeLevel.Success, $"{item.Label}: all {item.Parts.Count} Debrid links created");

        if (job.Items.Count > 1 && job.Items.All(candidate => candidate.AllLinksResolved) && job.TryAnnounceLinks())
        {
            Notify(job, NoticeLevel.Success, $"{job.Name}: all {job.LinksTotal} Debrid links created");
        }
    }

    private async Task DownloadAsync(DownloadJob job, MediaItem item, ItemRun run)
    {
        item.BeginDownloading();

        // A part recorded as downloaded whose file is gone (or truncated) has to come again.
        foreach (var part in item.Parts.Where(part => part.State is PartState.Downloaded))
        {
            var path = Path.Combine(job.Workspace.ArchiveDirectory(item), part.StorageName);
            if (!File.Exists(path) || new FileInfo(path).Length != part.BytesTotal) part.ForgetDownload();
        }

        var downloads = item.Parts
            .Where(part => part.State is not PartState.Downloaded)
            .Select(part => DownloadWithRetryAsync(job, part, run))
            .ToList();

        await Task.WhenAll(downloads);
    }

    private async Task DownloadWithRetryAsync(DownloadJob job, ArchivePart part, ItemRun run)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                // The retry loop sits outside the download slot, so a part that waits for a new link does not
                // hold up the others.
                await scheduler.RunDownloadAsync(token => downloader.DownloadAsync(part, token), run.Token);
                await jobs.SaveAsync(job);

                return;
            }
            catch (DownloadLinkException exception) when (attempt < MaxAttempts)
            {
                logger.LogWarning("The download link of {File} was refused ({Status}), asking Debrid-Link for a new one", LogSafe.OneLine(part.StorageName), exception.StatusCode);

                part.InvalidateLink();
                await resolver.ResolveAsync(part, true, run.Token);
                await jobs.SaveAsync(job);
            }
            catch (Exception exception) when (IsTransient(exception) && attempt < MaxAttempts)
            {
                logger.LogWarning("Downloading {File} failed on attempt {Attempt}: {Message}", LogSafe.OneLine(part.StorageName), attempt, LogSafe.OneLine(exception.Message));

                await Task.Delay(TimeSpan.FromSeconds(3 * attempt), run.Token);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                part.Fail(exception.Message);
                throw new IOException($"{part.StorageName}: {exception.Message}", exception);
            }
        }
    }

    private async Task UnpackAsync(DownloadJob job, MediaItem item, CancellationToken cancellationToken)
    {
        var workspace = job.Workspace;
        var volumes = item.Parts.Select(part => Path.Combine(workspace.ArchiveDirectory(item), part.StorageName)).ToList();

        await scheduler.RunExtractionAsync(async token =>
        {
            item.BeginExtracting();

            var password = await passwords.ResolveAsync(volumes, job, token);

            if (password.Outcome is PasswordOutcome.Missing)
            {
                item.RequirePassword();
                Notify(job, NoticeLevel.Warning, $"{item.Label}: none of the known passwords opens the archive, enter one");

                return;
            }

            // Whatever an earlier, interrupted attempt left behind must not end up in the result.
            var output = workspace.OutputDirectory(item);
            if (Directory.Exists(output)) Directory.Delete(output, true);

            await extractor.ExtractAsync(volumes, output, password.Password, percent => item.ReportExtraction(percent), token);

            item.BeginOrganizing();
            var files = organizer.Organize(workspace, item);

            item.Complete(files);
            Notify(job, NoticeLevel.Success, $"Completed: {files[0]}");
        }, cancellationToken);
    }

    private void HandleFailure(DownloadJob job, MediaItem item, Exception cause, CancellationToken jobToken)
    {
        if (jobToken.IsCancellationRequested || cause is OperationCanceledException)
        {
            item.Cancel();
            return;
        }

        if (cause is ExtractionException { PasswordProblem: true })
        {
            item.RequirePassword();
            Notify(job, NoticeLevel.Warning, $"{item.Label}: the password was rejected while unpacking, enter another one");

            return;
        }

        logger.LogWarning(cause, "{Item} of {Job} failed", item.Label, LogSafe.OneLine(job.Name));

        item.Fail(cause.Message);
        Notify(job, NoticeLevel.Error, $"{item.Label} failed: {cause.Message}");
    }

    private async Task FinishAsync(DownloadJob job, List<Task> tasks)
    {
        try
        {
            await Task.WhenAll(tasks);
            await FinalizeAsync(job);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Finishing {Job} failed", LogSafe.OneLine(job.Name));
        }
    }

    private async Task FinalizeAsync(DownloadJob job)
    {
        // Another batch of this job (a retry, a password) may still be running; the last one to end finishes the job.
        if (job.IsRemoved || job.Items.Any(item => item.IsActive)) return;

        _runs.TryRemove(job.Id, out _);

        switch (job.Status)
        {
            case JobStatus.Completed:
                var kept = false;

                try
                {
                    kept = !job.Workspace.CleanUp();
                }
                catch (IOException exception)
                {
                    logger.LogWarning(exception, "Cleaning up {Job} failed", LogSafe.OneLine(job.Name));
                    kept = true;
                }

                jobs.DeleteManifest(job);

                Notify(
                    job,
                    NoticeLevel.Success,
                    $"{job.Name}: downloaded {job.Items.Count} of {job.Items.Count} {(job.Release.Kind is ReleaseKind.Movie ? "files" : "episodes")}" +
                    (kept ? ", archives and unrecognized files were kept" : ""));

                return;

            case JobStatus.Failed:
                var failed = job.Items.Count(item => item.Stage is ItemStage.Failed);
                Notify(job, NoticeLevel.Error, $"{job.Name}: {failed} of {job.Items.Count} failed, the rest is done");
                break;
        }

        await jobs.SaveAsync(job);
    }

    private void Notify(DownloadJob job, NoticeLevel level, string text)
    {
        var entry = job.Log(level, text);

        hub.Publish(DownloadEvents.Notice(job, entry));
        logger.LogInformation("[{Job}] {Text}", LogSafe.OneLine(job.Name), LogSafe.OneLine(text));
    }

    private static bool IsTransient(Exception exception)
    {
        return exception is HttpRequestException or IOException or TimeoutException;
    }

    // One workflow's own cancellation: it ends with the job, and also when any of its parts fails for good, so the
    // other parts of the same episode stop downloading something that can no longer be unpacked.
    private sealed class ItemRun(CancellationToken parent) : IDisposable
    {
        private readonly CancellationTokenSource _source = CancellationTokenSource.CreateLinkedTokenSource(parent);

        private Exception? _failure;

        public CancellationToken Token => _source.Token;

        public Exception? Failure => _failure;

        public void Abort(Exception exception)
        {
            if (exception is not OperationCanceledException) Interlocked.CompareExchange(ref _failure, exception, null);

            _source.Cancel();
        }

        public void Dispose()
        {
            _source.Dispose();
        }
    }
}
