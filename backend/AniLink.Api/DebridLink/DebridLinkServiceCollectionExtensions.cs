using System.Net.Http.Headers;

namespace AniLink.Api.DebridLink;

public static class DebridLinkServiceCollectionExtensions
{
    public static IServiceCollection AddAniLinkDebridLink(this IServiceCollection services, DebridLinkOptions options)
    {
        services.AddSingleton(options);

        // The API client carries the key. The file client deliberately does not: download links point at
        // Debrid-Link's CDN hosts and the key has no business there.
        services.AddHttpClient(DebridLinkClient.ApiClient, client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        });

        // A multi-gigabyte part takes as long as it takes, so there is no overall timeout, only a connect timeout.
        services.AddHttpClient(DebridLinkClient.FileClient, client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(30),
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            });

        services.AddSingleton<DebridLinkClient>();

        return services;
    }
}
