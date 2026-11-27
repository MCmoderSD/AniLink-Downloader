using AniLink.Api.Services.Streaming;

namespace AniLink.Api.Services.Files;

public static class FilesServiceCollectionExtensions
{
    public static IServiceCollection AddAniLinkFiles(this IServiceCollection services)
    {
        services.AddSingleton<FileHub>();
        services.AddSingleton<FilePaths>();
        services.AddSingleton<FolderTotals>();

        services.AddSingleton<FileChanges>();
        services.AddHostedService(provider => provider.GetRequiredService<FileChanges>());

        services.AddSingleton<FileBrowser>();
        services.AddHostedService<FileWatcher>();

        return services;
    }
}