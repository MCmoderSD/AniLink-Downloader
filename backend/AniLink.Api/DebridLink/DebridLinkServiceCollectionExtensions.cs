using System.Net.Http.Headers;

namespace AniLink.Api.DebridLink;

public static class DebridLinkServiceCollectionExtensions
{
    public static IServiceCollection AddAniLinkDebridLink(this IServiceCollection services, DebridLinkOptions options)
    {
        services.AddSingleton(options);

        services.AddHttpClient(DebridLinkClient.ApiClient, client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        });

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