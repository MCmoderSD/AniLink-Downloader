using AniLink.Api.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AniLink.Api.Repositories;

// The downloads folder is the only state this app has, so it is what decides whether the app is ready: it has to
// exist and accept a write. A volume mounted read-only or with the wrong owner shows up here, not halfway through a season.
public sealed class StorageHealthCheck(DownloadOptions options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var probe = Path.Combine(options.Root, ".anilink-health");

        try
        {
            Directory.CreateDirectory(options.Root);

            await File.WriteAllTextAsync(probe, "ok", cancellationToken);
            File.Delete(probe);

            return HealthCheckResult.Healthy(options.Root);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return HealthCheckResult.Unhealthy($"{options.Root} is not writable.", exception);
        }
    }
}
