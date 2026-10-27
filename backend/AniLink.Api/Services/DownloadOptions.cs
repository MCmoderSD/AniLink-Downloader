namespace AniLink.Api.Services;

public sealed class DownloadOptions
{
    public const int MaxAllowedConcurrency = 16;

    public string Root { get; init; } = "";

    public int MaxConcurrency { get; init; }

    public static DownloadOptions From(IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection("Downloads");

        var root = section["Root"] ?? "";
        var concurrency = section.GetValue("MaxConcurrency", 3);

        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException("Downloads:Root is empty. It is the folder finished media is written to; in Docker it is the /downloads volume.");
        }

        if (concurrency is < 1 or > MaxAllowedConcurrency)
        {
            throw new InvalidOperationException($"Downloads:MaxConcurrency must be between 1 and {MaxAllowedConcurrency}, got {concurrency}.");
        }

        return new DownloadOptions
        {
            // A relative root is anchored at the project folder, not at wherever dotnet happened to be started.
            Root = Path.GetFullPath(root, environment.ContentRootPath),
            MaxConcurrency = concurrency
        };
    }
}
