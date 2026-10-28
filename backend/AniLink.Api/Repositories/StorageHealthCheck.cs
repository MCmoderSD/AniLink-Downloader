using AniLink.Api.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AniLink.Api.Repositories;

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