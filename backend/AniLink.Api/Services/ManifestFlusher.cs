using AniLink.Api.Repositories;

namespace AniLink.Api.Services;

public sealed class ManifestFlusher(JobManifestStore manifests) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return manifests.FlushAsync();
    }
}