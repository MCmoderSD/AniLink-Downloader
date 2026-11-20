using AniLink.Api.Data.Downloads;
using AniLink.Api.Helpers;

namespace AniLink.Api.Repositories;

public sealed class WorkspaceGate(JobRepository jobs)
{
    private readonly List<string> _claims = [];

    public Lock Sync { get; } = new();

    public bool IsBeingDeleted(string workspace)
    {
        var full = Path.GetFullPath(workspace);

        lock (Sync) return _claims.Any(claim => PathScope.Overlaps(claim, full, StringComparison.OrdinalIgnoreCase));
    }

    public WorkspaceClaim? TryClaim(string path, out IReadOnlyList<DownloadJob> blockers)
    {
        lock (Sync)
        {
            blockers = [.. jobs.ListActive().Where(job => PathScope.Overlaps(Path.GetFullPath(job.Workspace.Root), path, StringComparison.OrdinalIgnoreCase))];

            if (blockers.Count > 0 || _claims.Any(claim => PathScope.Overlaps(claim, path, StringComparison.OrdinalIgnoreCase))) return null;

            _claims.Add(path);

            return new WorkspaceClaim(this, path);
        }
    }

    internal void Release(string path)
    {
        lock (Sync) _claims.Remove(path);
    }
}

public sealed class WorkspaceClaim(WorkspaceGate gate, string path) : IDisposable
{
    public void Dispose()
    {
        gate.Release(path);
    }
}