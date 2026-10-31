using System.Collections.Concurrent;
using AniLink.Api.Data.Downloads;

namespace AniLink.Api.Repositories;

public sealed class JobRepository(JobManifestStore manifests)
{
    private readonly ConcurrentDictionary<Guid, DownloadJob> _jobs = new();

    public IReadOnlyList<DownloadJob> List()
    {
        return [.. _jobs.Values.OrderByDescending(job => job.CreatedAt)];
    }

    public DownloadJob? Get(Guid id)
    {
        return _jobs.GetValueOrDefault(id);
    }

    public void Add(DownloadJob job)
    {
        _jobs[job.Id] = job;
    }

    public DownloadJob? FindActive(string workdir)
    {
        return _jobs.Values.FirstOrDefault(job =>
            string.Equals(Path.GetFullPath(job.Workspace.Root), Path.GetFullPath(workdir), StringComparison.OrdinalIgnoreCase)
            && job.Status is JobStatus.Active or JobStatus.Paused or JobStatus.PasswordRequired or JobStatus.Downloaded);
    }

    public DownloadJob? Remove(Guid id)
    {
        if (!_jobs.TryRemove(id, out var job)) return null;

        job.MarkRemoved();
        manifests.Delete(job);

        return job;
    }

    public Task SaveAsync(DownloadJob job)
    {
        return manifests.WriteAsync(job);
    }

    public void SaveProgress(DownloadJob job)
    {
        manifests.WriteProgress(job);
    }

    public void DeleteManifest(DownloadJob job)
    {
        manifests.Delete(job);
    }
}