using AniLink.Api.Archives;
using AniLink.Api.Repositories;
using AniLink.Api.Services.Streaming;

namespace AniLink.Api.Services;

public static class FeatureServiceCollectionExtensions
{
    public static IServiceCollection AddAniLinkDownloads(this IServiceCollection services, DownloadOptions downloads, ArchiveOptions archives)
    {
        services.AddSingleton(downloads);
        services.AddSingleton(archives);

        services.AddSingleton<JobManifestStore>();
        services.AddSingleton<JobRepository>();
        services.AddSingleton<ProgressHub>();

        services.AddSingleton<DownloadScheduler>();
        services.AddSingleton<PartDownloader>();

        services.AddSingleton<LinkResolver>();
        services.AddHostedService(provider => provider.GetRequiredService<LinkResolver>());

        services.AddSingleton<SevenZipProcess>();
        services.AddSingleton<ArchiveExtractor>();
        services.AddSingleton<PasswordResolver>();
        services.AddSingleton<MediaOrganizer>();

        services.AddSingleton<DownloadPipeline>();
        services.AddSingleton<DownloadService>();

        services.AddHostedService<ProgressPublisher>();
        services.AddHostedService<JobRecovery>();

        services.AddHealthChecks()
            .AddCheck<StorageHealthCheck>("storage", tags: ["ready"])
            .AddCheck<SevenZipHealthCheck>("7zip", tags: ["ready"]);

        return services;
    }
}