using System.Collections.Concurrent;
using System.Diagnostics;
using AniLink.Api.Archives;
using AniLink.Api.Data.Downloads;
using AniLink.Api.Exceptions.Archives;
using AniLink.Api.Exceptions.DebridLink;
using AniLink.Api.Exceptions.Downloads;
using AniLink.Api.Helpers;
using AniLink.Api.Repositories;
using AniLink.Api.Services.Streaming;

namespace AniLink.Api.Services;

public sealed class DownloadPipeline(
    LinkResolver resolver,
    DownloadScheduler scheduler,
    PartDownloader downloader,
    PasswordResolver passwords,
    ArchiveExtractor extractor,
    MediaOrganizer organizer,
    JobRepository jobs,
    ProgressHub hub,
    IHostApplicationLifetime lifetime,
    ILogger<DownloadPipeline> logger)
{
    private const int MaxAttempts = 3;

    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runs = new();
    private readonly ConcurrentDictionary<MediaItem, CancellationTokenSource> _itemRuns = new();

    public void Start(DownloadJob job, IEnumerable<MediaItem>? only = null)
    {
        var items = (only ?? job.Items).Where(item => item.NeedsWork).OrderBy(item => item.Number).ToList();
        var source = _runs.GetOrAdd(job.Id, _ => new CancellationTokenSource());

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

    public bool Unpack(DownloadJob job)
    {
        var items = job.Items.Where(item => item.RequestUnpack()).ToList();
        if (items.Count == 0) return false;

        Notify(job, NoticeLevel.Info, $"Unpacking {items.Count} {(items.Count == 1 ? "item" : "items")}");
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
        else _ = jobs.SaveAsync(job);

        Notify(job, NoticeLevel.Warning, $"{job.Name}: canceled");
    }

    public bool Pause(DownloadJob job)
    {
        var paused = job.Items.Count(item => item.Pause());
        if (paused == 0) return false;

        if (_runs.TryRemove(job.Id, out var source)) source.Cancel();
        else _ = jobs.SaveAsync(job);

        Notify(job, NoticeLevel.Info, $"{job.Name}: paused");

        return true;
    }

    public bool Resume(DownloadJob job)
    {
        var items = job.Items.Where(item => item.Resume()).ToList();
        if (items.Count == 0) return false;

        Notify(job, NoticeLevel.Info, $"{job.Name}: resumed");
        Start(job, items);

        return true;
    }

    public bool CancelItem(MediaItem item)
    {
        if (!item.Cancel()) return false;

        Interrupt(item);
        Notify(item.Job, NoticeLevel.Warning, $"{item.Label}: canceled");

        return true;
    }

    public bool PauseItem(MediaItem item)
    {
        if (!item.Pause()) return false;

        Interrupt(item);
        Notify(item.Job, NoticeLevel.Info, $"{item.Label}: paused");

        return true;
    }

    public bool ResumeItem(MediaItem item)
    {
        if (!item.Resume()) return false;

        Notify(item.Job, NoticeLevel.Info, $"{item.Label}: resumed");
        Start(item.Job, [item]);

        return true;
    }

    public bool RetryItem(MediaItem item)
    {
        if (!item.ResetForRetry()) return false;

        Notify(item.Job, NoticeLevel.Info, $"Retrying {item.Label}");
        Start(item.Job, [item]);

        return true;
    }

    public bool UnpackItem(MediaItem item)
    {
        if (!item.RequestUnpack()) return false;

        Notify(item.Job, NoticeLevel.Info, $"Unpacking {item.Label}");
        Start(item.Job, [item]);

        return true;
    }

    public void RemoveItem(MediaItem item)
    {
        var job = item.Job;

        item.Cancel();

        var running = _itemRuns.TryRemove(item, out var source);
        source?.Cancel();

        job.RemoveItem(item);
        job.Log(NoticeLevel.Info, $"{item.Label}: removed from the list");

        if (!running && job.Status is JobStatus.Completed) _ = FinalizeAsync(job);
        else _ = jobs.SaveAsync(job);
    }

    private void Interrupt(MediaItem item)
    {
        if (_itemRuns.TryRemove(item, out var source)) source.Cancel();
        else _ = jobs.SaveAsync(item.Job);
    }

    private async Task ProcessAsync(DownloadJob job, MediaItem item, CancellationToken jobToken)
    {
        var own = new CancellationTokenSource();
        _itemRuns[item] = own;

        using var run = new ItemRun(jobToken, own.Token);

        var pending = item.Parts.Where(part => !part.HasLink).ToList();
        var resolutions = pending.Select(part => resolver.ResolveAsync(part, false, run.Token)).ToList();

        try
        {
            if (pending.Count > 0) await AwaitLinksAsync(job, item, pending, resolutions);

            await DownloadAsync(job, item, run);

            if (item.UnpackWanted) await UnpackAsync(job, item, run.Token);
            else if (item.MarkDownloaded()) Notify(job, NoticeLevel.Success, $"Downloaded: {item.Label}, ready to unpack");
        }
        catch (Exception exception)
        {
            run.Abort(exception);
            HandleFailure(job, item, run.Failure ?? exception, run.Interrupted);
        }
        finally
        {
            _itemRuns.TryRemove(new KeyValuePair<MediaItem, CancellationTokenSource>(item, own));

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

            jobs.SaveProgress(job);
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
                await scheduler.RunDownloadAsync(token => downloader.DownloadAsync(part, token), run.Token);
                jobs.SaveProgress(job);

                return;
            }
            catch (DownloadLinkException exception) when (attempt < MaxAttempts)
            {
                logger.LogWarning("The download link of {File} was refused ({Status}), asking Debrid-Link for a new one", LogSafe.OneLine(part.StorageName), exception.StatusCode);

                part.InvalidateLink();
                await resolver.ResolveAsync(part, true, run.Token);
                jobs.SaveProgress(job);
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

        var label = item.LogLabel;
        var queued = Stopwatch.StartNew();

        if (scheduler.ExtractionsRunning >= DownloadScheduler.ExtractionLimit)
        {
            logger.LogInformation("{Label} waits to be unpacked: all {Limit} unpacking slots are in use", label, DownloadScheduler.ExtractionLimit);
        }

        await scheduler.RunExtractionAsync(async token =>
        {
            if (!item.BeginExtracting()) return;

            var unpacking = Stopwatch.StartNew();

            logger.LogInformation(
                "Unpacking {Label} starts: {Volumes} volumes, {Size}, waited {Waited} for a slot, {Running} of {Limit} slots in use",
                label,
                volumes.Count,
                ByteSize.Format(item.Parts.Sum(part => part.BytesTotal)),
                DurationText.Format(queued.Elapsed),
                scheduler.ExtractionsRunning,
                DownloadScheduler.ExtractionLimit);

            try
            {
                var password = await passwords.ResolveAsync(volumes, job, label, token);

                if (password.Outcome is PasswordOutcome.Missing)
                {
                    if (item.RequirePassword()) Notify(job, NoticeLevel.Warning, $"{item.Label}: none of the known passwords opens the archive, enter one");

                    return;
                }

                var output = workspace.OutputDirectory(item);
                if (Directory.Exists(output)) Directory.Delete(output, true);

                if (DiskSpace.Available(output) is { } free && password.UnpackedBytes > free)
                {
                    throw new ExtractionException($"Unpacking needs {ByteSize.Format(password.UnpackedBytes)}, but only {ByteSize.Format(free)} are free in {workspace.Root}. Free up space and retry.");
                }

                await extractor.ExtractAsync(volumes, output, password.Password, label, item.ReportExtraction, token);

                if (!item.BeginOrganizing()) return;

                var files = organizer.Organize(workspace, item);

                item.Complete(files);

                logger.LogInformation("Unpacking {Label} is done after {Elapsed}: {Files}", label, DurationText.Format(unpacking.Elapsed), string.Join(", ", files));
                Notify(job, NoticeLevel.Success, $"Completed: {files[0]}");
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Unpacking {Label} was stopped after {Elapsed}", label, DurationText.Format(unpacking.Elapsed));

                throw;
            }
        }, cancellationToken);
    }

    private void HandleFailure(DownloadJob job, MediaItem item, Exception cause, bool interrupted)
    {
        if (interrupted) return;

        if (cause is OperationCanceledException)
        {
            if (!lifetime.ApplicationStopping.IsCancellationRequested) item.Cancel();

            return;
        }

        if (cause is ExtractionException { PasswordProblem: true })
        {
            item.RequirePassword();
            Notify(job, NoticeLevel.Warning, $"{item.Label}: the password was rejected while unpacking, enter another one");

            return;
        }

        logger.LogWarning(cause, "{Item} failed", item.LogLabel);

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
        if (job.IsRemoved || job.Items.Any(item => item.IsActive || item.Stage is ItemStage.Paused)) return;

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

            case JobStatus.Downloaded:
                var ready = job.Items.Count(item => item.Stage is ItemStage.Downloaded);
                Notify(job, NoticeLevel.Info, $"{job.Name}: {ready} of {job.Items.Count} {(job.Release.Kind is ReleaseKind.Movie ? "files" : "episodes")} downloaded, ready to unpack");
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

    private sealed class ItemRun(CancellationToken job, CancellationToken own) : IDisposable
    {
        private readonly CancellationTokenSource _source = CancellationTokenSource.CreateLinkedTokenSource(job, own);

        private Exception? _failure;

        public CancellationToken Token => _source.Token;

        public Exception? Failure => _failure;

        public bool Interrupted => job.IsCancellationRequested || own.IsCancellationRequested;

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