using System.Collections.Concurrent;
using System.Text.Json;
using AniLink.Api.Data.Downloads;
using AniLink.Api.Helpers;

namespace AniLink.Api.Repositories;

// Reads and writes the .anilink.json of a job. A write goes to a temporary file first and replaces the real one
// in a single rename, so a container that is killed halfway never leaves a manifest that cannot be parsed.
public sealed class JobManifestStore(ILogger<JobManifestStore> logger)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _gates = new();

    public async Task WriteAsync(DownloadJob job)
    {
        var gate = _gates.GetOrAdd(job.Id, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();

        try
        {
            // A job that was removed while a save was queued must not bring its manifest back.
            if (job.IsRemoved) return;

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
        finally
        {
            gate.Release();
        }
    }

    public void Delete(DownloadJob job)
    {
        try
        {
            File.Delete(job.Workspace.Manifest);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "The manifest of {Job} could not be deleted", LogSafe.OneLine(job.Name));
        }

        _gates.TryRemove(job.Id, out _);
    }

    // Season jobs keep their manifest in <name>/<season>, films in <name>, so two levels cover everything.
    public IEnumerable<string> Find(string root)
    {
        if (!Directory.Exists(root)) return [];

        return Directory.EnumerateFiles(root, Workspace.ManifestName, new EnumerationOptions
        {
            RecurseSubdirectories = true,
            MaxRecursionDepth = 2,
            IgnoreInaccessible = true
        });
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
}
