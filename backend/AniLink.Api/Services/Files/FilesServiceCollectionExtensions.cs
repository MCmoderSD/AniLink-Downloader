namespace AniLink.Api.Services.Files;

public static class FilesServiceCollectionExtensions
{
    public static IServiceCollection AddAniLinkFiles(this IServiceCollection services)
    {
        services.AddSingleton<FilePaths>();
        services.AddSingleton<FolderTotals>();
        services.AddSingleton<FileBrowser>();

        return services;
    }
}