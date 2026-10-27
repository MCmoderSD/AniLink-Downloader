using AniLink.Api.Archives;
using AniLink.Api.Data.Downloads;
using AniLink.Api.Repositories;

namespace AniLink.Api.Services;

// At startup, finds the manifests that earlier runs left in the downloads folder and brings their jobs back.
// Jobs that still have work to do go straight on; failed and cancelled ones only reappear in the list.
public sealed class JobRecovery(
    DownloadOptions options,
    ArchiveOptions archive,
    JobManifestStore manifests,
    JobRepository jobs,
    DownloadPipeline pipeline,
    ILogger<JobRecovery> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The root may not exist yet on a fresh volume; creating it also proves it is writable.
        try
        {
            Directory.CreateDirectory(options.Root);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "The downloads folder {Root} cannot be created", options.Root);
            return;
        }

        var resumed = 0;
        var listed = 0;

        foreach (var path in manifests.Find(options.Root))
        {
            if (stoppingToken.IsCancellationRequested) return;

            var manifest = await manifests.ReadAsync(path);
            if (manifest is null || jobs.Get(manifest.Id) is not null) continue;

            var job = DownloadJob.Restore(manifest, Path.GetDirectoryName(path)!, archive.Passwords);
            jobs.Add(job);

            if (job.Items.Any(item => !item.IsTerminal))
            {
                pipeline.Start(job);
                resumed++;
            }
            else
            {
                listed++;
            }
        }

        if (resumed + listed > 0) logger.LogInformation("Recovered {Resumed} unfinished and {Listed} stopped jobs from {Root}", resumed, listed, options.Root);
    }
}
