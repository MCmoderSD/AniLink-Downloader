using AniLink.Api.Archives;
using AniLink.Api.Data.Downloads;
using AniLink.Api.Helpers;
using AniLink.Api.Repositories;
using AniLink.Api.Services.Streaming;

namespace AniLink.Api.Services;

public sealed class DownloadService(
    DownloadOptions options,
    ArchiveOptions archive,
    JobRepository jobs,
    WorkspaceGate gate,
    DownloadPipeline pipeline,
    ProgressHub hub,
    ILogger<DownloadService> logger
) {
    private const string FolderBeingDeleted = "The folder of this download is being deleted. Try again in a moment.";

    public ConfigResponse GetConfig()
    {
        return new ConfigResponse(options.MaxConcurrency, archive.Passwords.Count, options.Root, LimitsResponse.Current);
    }

    public static AnalysisResponse Analyze(AnalyzeRequest request)
    {
        return AnalysisResponse.From(LinkAnalyzer.Analyze(request.Links, request.Kind, request.Format));
    }

    public IReadOnlyList<JobResponse> List()
    {
        return [.. jobs.List().Select(JobResponse.From)];
    }

    public JobResponse? Get(Guid id)
    {
        return jobs.Get(id) is { } job ? JobResponse.From(job) : null;
    }

    public async Task<CreateOutcome> CreateAsync(CreateDownloadRequest request)
    {
        var name = request.Name.Trim();
        if (FileNames.Sanitize(name).Length == 0) return new DownloadInvalid("The name has no characters that can be used for a folder.");

        if (request.Passwords?.Any(password => password is { Length: > DownloadLimits.MaxPasswordLength }) is true)
        {
            return new DownloadInvalid($"A password may have at most {DownloadLimits.MaxPasswordLength} characters.");
        }

        var analysis = LinkAnalyzer.Analyze(request.Links, request.Kind, request.Format);

        if (analysis.Episodes.Count == 0) return new DownloadInvalid(analysis.Unmatched.Count > 0
            ? "None of the links could be assigned. Enter a season format that appears in the file names."
            : "There are no links.");

        if (analysis.Unmatched.Count > 0)
        {
            return new DownloadInvalid($"{analysis.Unmatched.Count} of {analysis.Links.Count} links could not be assigned to an episode, for example {LogSafe.OneLine(analysis.Unmatched[0])}.");
        }

        int? season = request.Kind is ReleaseKind.Season ? request.Season ?? analysis.Season ?? 1 : null;
        var workspace = Workspace.For(options.Root, name, request.Kind, season);

        DownloadJob job;

        lock (gate.Sync)
        {
            if (gate.IsBeingDeleted(workspace.Root)) return new DownloadConflict($"{name}: {FolderBeingDeleted}");

            if (jobs.FindActive(workspace.Root) is not null)
            {
                return new DownloadConflict($"{name} is already being downloaded into {workspace.Root}.");
            }

            var passwords = new PasswordBook(archive.Passwords);
            foreach (var password in (request.Passwords ?? []).Reverse()) passwords.Add(password);
            if (!string.IsNullOrWhiteSpace(request.Password)) passwords.Add(request.Password);

            job = new DownloadJob(
                Guid.NewGuid(),
                name,
                DateTimeOffset.UtcNow,
                new Release(request.Kind, season, analysis.Format?.Prefix),
                workspace,
                passwords);

            foreach (var group in analysis.Episodes) job.AddItem(group.Number, group.Parts, request.Unpack);

            try
            {
                workspace.Prepare(job.Items);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return new DownloadInvalid($"The folder {workspace.Root} could not be prepared: {exception.Message}");
            }

            jobs.Add(job);
        }

        var parts = job.Parts.Count();
        job.Log(NoticeLevel.Info, $"{name}: {job.Items.Count} {(request.Kind is ReleaseKind.Movie ? "file" : job.Items.Count == 1 ? "episode" : "episodes")}, {parts} {(parts == 1 ? "part" : "parts")}, saving to {workspace.Root}{(request.Unpack ? "" : ", unpacking on request")}");

        await jobs.SaveAsync(job);
        hub.Publish(DownloadEvents.Job(job));

        logger.LogInformation("Queued {Job} with {Items} items and {Parts} parts", LogSafe.OneLine(name), job.Items.Count, parts);

        pipeline.Start(job);

        return new DownloadCreated(JobResponse.From(job));
    }

    public ActionOutcome Cancel(Guid id)
    {
        if (jobs.Get(id) is not { } job) return new ActionMissing();

        pipeline.Cancel(job);

        return new ActionDone(JobResponse.From(job));
    }

    public ActionOutcome Pause(Guid id)
    {
        return Apply(id, pipeline.Pause, "Nothing in this download is running that could be paused.");
    }

    public ActionOutcome Resume(Guid id)
    {
        return Apply(id, pipeline.Resume, "This download is not paused.");
    }

    public ActionOutcome Retry(Guid id)
    {
        lock (gate.Sync)
        {
            if (jobs.Get(id) is { } job && gate.IsBeingDeleted(job.Workspace.Root)) return new ActionRejected(FolderBeingDeleted);

            return Apply(id, pipeline.Retry, "Nothing failed or was canceled in this download.");
        }
    }

    public ActionOutcome SetUnpack(Guid id, bool wanted)
    {
        return Apply(id, job => pipeline.SetUnpack(job, wanted), "Whether this download is unpacked can only be changed until its download is complete.");
    }

    public ActionOutcome Unpack(Guid id)
    {
        return Apply(id, pipeline.Unpack, "Nothing in this download is waiting to be unpacked.");
    }

    public ActionOutcome CancelUnpack(Guid id)
    {
        return Apply(id, pipeline.CancelUnpack, "Nothing in this download is being unpacked.");
    }

    public ActionOutcome SubmitPassword(Guid id, string password)
    {
        return Apply(id, job => pipeline.SubmitPassword(job, password), "No item of this download is waiting for a password.");
    }

    public ActionOutcome SetItemUnpack(Guid id, int number, bool wanted)
    {
        return ApplyToItem(id, number, item => pipeline.SetItemUnpack(item, wanted), "Whether this item is unpacked can only be changed until its download is complete.");
    }

    public ActionOutcome UnpackItem(Guid id, int number)
    {
        return ApplyToItem(id, number, pipeline.UnpackItem, "This item is not waiting to be unpacked.");
    }

    public ActionOutcome CancelUnpackItem(Guid id, int number)
    {
        return ApplyToItem(id, number, pipeline.CancelUnpackItem, "This item is not being unpacked.");
    }

    public ActionOutcome CancelItem(Guid id, int number)
    {
        return ApplyToItem(id, number, pipeline.CancelItem, "This item has already ended.");
    }

    public ActionOutcome PauseItem(Guid id, int number)
    {
        return ApplyToItem(id, number, pipeline.PauseItem, "This item is not running, so it cannot be paused.");
    }

    public ActionOutcome ResumeItem(Guid id, int number)
    {
        return ApplyToItem(id, number, pipeline.ResumeItem, "This item is not paused.");
    }

    public ActionOutcome RetryItem(Guid id, int number)
    {
        lock (gate.Sync)
        {
            if (jobs.Get(id) is { } job && gate.IsBeingDeleted(job.Workspace.Root)) return new ActionRejected(FolderBeingDeleted);

            return ApplyToItem(id, number, pipeline.RetryItem, "This item neither failed nor was canceled.");
        }
    }

    public bool RemoveItem(Guid id, int number)
    {
        if (jobs.Get(id)?.FindItem(number) is not { } item) return false;

        var job = item.Job;

        if (job.Items.Count == 1) return Remove(id);

        pipeline.RemoveItem(item);
        hub.Publish(DownloadEvents.Job(job));

        return true;
    }

    public bool Remove(Guid id)
    {
        if (jobs.Get(id) is not { } job) return false;

        if (job.Status is JobStatus.Active) pipeline.Cancel(job);

        jobs.Remove(id);
        hub.Publish(DownloadEvents.Removed(id));

        return true;
    }

    private ActionOutcome Apply(Guid id, Func<DownloadJob, bool> action, string rejection)
    {
        if (jobs.Get(id) is not { } job) return new ActionMissing();

        return action(job) ? new ActionDone(JobResponse.From(job)) : new ActionRejected(rejection);
    }

    private ActionOutcome ApplyToItem(Guid id, int number, Func<MediaItem, bool> action, string rejection)
    {
        if (jobs.Get(id)?.FindItem(number) is not { } item) return new ActionMissing();

        return action(item) ? new ActionDone(JobResponse.From(item.Job)) : new ActionRejected(rejection);
    }
}