using System.Collections.Concurrent;
using System.IO.Enumeration;
using System.Text.Json;
using AniLink.Api.Data.Downloads;
using AniLink.Api.Helpers;

namespace AniLink.Api.Repositories;

public sealed class JobManifestStore(ILogger<JobManifestStore> logger)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly TimeSpan ProgressDelay = TimeSpan.FromSeconds(2);

    private readonly ConcurrentDictionary<Guid, Slot> _slots = new();

    private sealed class Slot(DownloadJob job)
    {
        public DownloadJob Job { get; } = job;
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public Lock Sync { get; } = new();
        public TaskCompletionSource? Queued { get; set; }
        public bool ProgressScheduled { get; set; }
        public bool Deleted { get; set; }
    }

    public Task WriteAsync(DownloadJob job)
    {
        return WriteAsync(SlotOf(job));
    }

    public void WriteProgress(DownloadJob job)
    {
        var slot = SlotOf(job);

        lock (slot.Sync)
        {
            if (slot.ProgressScheduled) return;
            slot.ProgressScheduled = true;
        }

        _ = WriteLaterAsync(slot);
    }

    public Task FlushAsync()
    {
        return Task.WhenAll(_slots.Values.Where(slot => slot.ProgressScheduled).Select(WriteAsync));
    }

    public void Delete(DownloadJob job)
    {
        if (_slots.TryRemove(job.Id, out var slot)) slot.Deleted = true;

        try
        {
            File.Delete(job.Workspace.Manifest);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "The manifest of {Job} could not be deleted", LogSafe.OneLine(job.Name));
        }
    }

    public static IEnumerable<string> Find(string root)
    {
        if (!Directory.Exists(root)) return [];

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            MaxRecursionDepth = 2,
            IgnoreInaccessible = true,
            AttributesToSkip = 0
        };

        return new FileSystemEnumerable<string>(root, (ref entry) => entry.ToFullPath(), options)
        {
            ShouldIncludePredicate = (ref entry) => !entry.IsDirectory && entry.FileName.Equals(Workspace.ManifestName, StringComparison.Ordinal),
            ShouldRecursePredicate = (ref entry) => !HoldsArchiveContent(ref entry)
        };
    }

    private static bool HoldsArchiveContent(ref FileSystemEntry entry)
    {
        var name = entry.FileName;

        if (!name.Equals(Workspace.ArchivesFolder, StringComparison.Ordinal) && !name.Equals(Workspace.OutputFolder, StringComparison.Ordinal)) return false;

        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        return !Path.TrimEndingDirectorySeparator(entry.Directory).Equals(Path.TrimEndingDirectorySeparator(entry.RootDirectory), comparison);
    }

    public async Task<JobManifest?> ReadAsync(string path)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            var manifest = await JsonSerializer.DeserializeAsync<JobManifest>(stream, Options);

            return manifest is { Version: JobManifest.CurrentVersion } ? manifest : null;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "The manifest {Path} could not be read and is skipped", LogSafe.OneLine(path));
            return null;
        }
    }

    private Slot SlotOf(DownloadJob job)
    {
        return _slots.GetOrAdd(job.Id, _ => new Slot(job));
    }

    private async Task WriteLaterAsync(Slot slot)
    {
        try
        {
            await Task.Delay(ProgressDelay);

            lock (slot.Sync) slot.ProgressScheduled = false;

            await WriteAsync(slot);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The progress of {Job} could not be saved", LogSafe.OneLine(slot.Job.Name));
        }
    }

    private Task WriteAsync(Slot slot)
    {
        TaskCompletionSource turn;

        lock (slot.Sync)
        {
            if (slot.Queued is { } queued) return queued.Task;

            slot.Queued = turn = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        _ = RunAsync(slot, turn);

        return turn.Task;
    }

    private async Task RunAsync(Slot slot, TaskCompletionSource turn)
    {
        await slot.Gate.WaitAsync();

        try
        {
            lock (slot.Sync) slot.Queued = null;

            if (slot is { Deleted: false, Job.IsRemoved: false }) await PersistAsync(slot.Job);

            turn.SetResult();
        }
        catch (Exception exception)
        {
            turn.SetException(exception);
        }
        finally
        {
            slot.Gate.Release();
        }
    }

    private async Task PersistAsync(DownloadJob job)
    {
        try
        {
            var path = job.Workspace.Manifest;
            var temporary = path + ".tmp";

            Directory.CreateDirectory(job.Workspace.Root);
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(JobManifest.From(job), Options));
            File.Move(temporary, path, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "The manifest of {Job} could not be written", LogSafe.OneLine(job.Name));
        }
    }
}