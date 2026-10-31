using AniLink.Api.Archives;
using AniLink.Api.Data.Downloads;
using AniLink.Api.Exceptions.Downloads;
using AniLink.Api.Helpers;
using AniLink.Api.Repositories;
using AniLink.Api.Services.Streaming;

namespace AniLink.Api.Services;

public sealed class DownloadService(
    DownloadOptions options,
    ArchiveOptions archive,
    JobRepository jobs,
    DownloadPipeline pipeline,
    ProgressHub hub,
    ILogger<DownloadService> logger)
{
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

    public async Task<JobResponse> CreateAsync(CreateDownloadRequest request)
    {
        var name = request.Name.Trim();
        if (FileNames.Sanitize(name).Length == 0) throw new InvalidDownloadException("The name has no characters that can be used for a folder.");

        var analysis = LinkAnalyzer.Analyze(request.Links, request.Kind, request.Format);

        if (analysis.Episodes.Count == 0) throw new InvalidDownloadException(analysis.Unmatched.Count > 0
            ? "None of the links could be assigned. Enter a season format that appears in the file names."
            : "There are no links.");

        if (analysis.Unmatched.Count > 0)
        {
            throw new InvalidDownloadException($"{analysis.Unmatched.Count} of {analysis.Links.Count} links could not be assigned to an episode, for example {LogSafe.OneLine(analysis.Unmatched[0])}.");
        }

        int? season = request.Kind is ReleaseKind.Season ? request.Season ?? analysis.Season ?? 1 : null;
        var workspace = Workspace.For(options.Root, name, request.Kind, season);

        if (jobs.FindActive(workspace.Root) is not null)
        {
            throw new DownloadConflictException($"{name} is already being downloaded into {workspace.Root}.");
        }

        var passwords = new PasswordBook(archive.Passwords);
        if (!string.IsNullOrWhiteSpace(request.Password)) passwords.Add(request.Password);

        var job = new DownloadJob(
            Guid.NewGuid(),
            name,
            DateTimeOffset.UtcNow,
            new Release(request.Kind, season, analysis.Format?.Prefix),
            workspace,
            passwords);

        foreach (var group in analysis.Episodes) job.AddItem(group.Number, group.Parts);

        try
        {
            workspace.Prepare(job.Items);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidDownloadException($"The folder {workspace.Root} could not be prepared: {exception.Message}");
        }

        jobs.Add(job);

        var parts = job.Parts.Count();
        job.Log(NoticeLevel.Info, $"{name}: {job.Items.Count} {(request.Kind is ReleaseKind.Movie ? "file" : job.Items.Count == 1 ? "episode" : "episodes")}, {parts} {(parts == 1 ? "part" : "parts")}, saving to {workspace.Root}");

        await jobs.SaveAsync(job);
        hub.Publish(DownloadEvents.Job(job));

        logger.LogInformation("Queued {Job} with {Items} items and {Parts} parts", LogSafe.OneLine(name), job.Items.Count, parts);

        pipeline.Start(job);

        return JobResponse.From(job);
    }

    public bool Cancel(Guid id)
    {
        if (jobs.Get(id) is not { } job) return false;

        pipeline.Cancel(job);

        return true;
    }

    public bool? Pause(Guid id)
    {
        return jobs.Get(id) is { } job ? pipeline.Pause(job) : null;
    }

    public bool? Resume(Guid id)
    {
        return jobs.Get(id) is { } job ? pipeline.Resume(job) : null;
    }

    public bool? Retry(Guid id)
    {
        return jobs.Get(id) is { } job ? pipeline.Retry(job) : null;
    }

    public bool? CancelItem(Guid id, int number)
    {
        return jobs.Get(id)?.FindItem(number) is { } item ? pipeline.CancelItem(item) : null;
    }

    public bool? PauseItem(Guid id, int number)
    {
        return jobs.Get(id)?.FindItem(number) is { } item ? pipeline.PauseItem(item) : null;
    }

    public bool? ResumeItem(Guid id, int number)
    {
        return jobs.Get(id)?.FindItem(number) is { } item ? pipeline.ResumeItem(item) : null;
    }

    public bool? RetryItem(Guid id, int number)
    {
        return jobs.Get(id)?.FindItem(number) is { } item ? pipeline.RetryItem(item) : null;
    }

    // Removing the only item left removes the whole job, because a job without items has nothing to show.
    public bool RemoveItem(Guid id, int number)
    {
        if (jobs.Get(id)?.FindItem(number) is not { } item) return false;

        var job = item.Job;

        if (job.Items.Count == 1) return Remove(id);

        pipeline.RemoveItem(item);
        hub.Publish(DownloadEvents.Job(job));

        return true;
    }

    public bool? SubmitPassword(Guid id, string password)
    {
        return jobs.Get(id) is { } job ? pipeline.SubmitPassword(job, password) : null;
    }

    public bool Remove(Guid id)
    {
        if (jobs.Get(id) is not { } job) return false;

        if (job.Status is JobStatus.Active) pipeline.Cancel(job);

        jobs.Remove(id);
        hub.Publish(DownloadEvents.Removed(id));

        return true;
    }
}