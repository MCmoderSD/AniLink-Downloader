using AniLink.Api.Helpers;

namespace AniLink.Api.DebridLink;

public sealed class DebridLinkOptions
{
    public string ApiKey { get; init; } = "";

    public string BaseUrl { get; init; } = "";

    public int RequestDelayMs { get; init; }

    public static DebridLinkOptions From(IConfiguration configuration)
    {
        var section = configuration.GetSection("DebridLink");

        var apiKey = configuration.GetRequiredValue("DebridLink:ApiKey", "Debrid-Link private API key, created at https://debrid-link.com/webapp/apikey");
        var baseUrl = section["BaseUrl"] ?? "";
        var delay = section.GetValue("RequestDelayMs", 500);

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException($"DebridLink:BaseUrl must be an absolute http(s) URL, got '{LogSafe.OneLine(baseUrl)}'.");
        }

        if (delay is < 0 or > 60_000)
        {
            throw new InvalidOperationException($"DebridLink:RequestDelayMs must be between 0 and 60000 milliseconds, got {delay}.");
        }

        return new DebridLinkOptions
        {
            ApiKey = apiKey.Trim(),
            BaseUrl = baseUrl.TrimEnd('/') + "/",
            RequestDelayMs = delay
        };
    }
}
