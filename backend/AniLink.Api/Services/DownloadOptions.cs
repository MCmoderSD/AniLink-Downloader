namespace AniLink.Api.Services;

public sealed class DownloadOptions
{
    public static int MaxAllowedConcurrency => Environment.ProcessorCount;

    public string Root { get; private init; } = "";

    public int MaxConcurrency { get; private init; }

    public int RequestedConcurrency { get; private init; }

    public static DownloadOptions From(IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection("Downloads");

        var root = section["Root"] ?? "";
        var concurrency = section.GetValue("MaxConcurrency", 3);

        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException("Downloads:Root is empty. It is the folder finished media is written to; in Docker it is the /downloads volume.");
        }

        if (concurrency < 1)
        {
            throw new InvalidOperationException($"Concurrent_Downloads (Downloads:MaxConcurrency) must be at least 1, got {concurrency}.");
        }

        return new DownloadOptions
        {
            Root = Path.GetFullPath(root, environment.ContentRootPath),
            MaxConcurrency = Math.Min(concurrency, MaxAllowedConcurrency),
            RequestedConcurrency = concurrency
        };
    }
}