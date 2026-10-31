using System.Text.RegularExpressions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AniLink.Api.Archives;

public sealed partial class SevenZipHealthCheck(SevenZipProcess sevenZip) : IHealthCheck
{
    [GeneratedRegex(@"7-Zip[^\r\n]*")]
    private static partial Regex Banner();

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await sevenZip.RunAsync([], null, cancellationToken);
            var banner = Banner().Match(result.Combined);

            return banner.Success
                ? HealthCheckResult.Healthy(banner.Value.Trim())
                : HealthCheckResult.Unhealthy($"{sevenZip.Executable} did not answer like 7-Zip.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy($"7-Zip is not available at {sevenZip.Executable}.", exception);
        }
    }
}