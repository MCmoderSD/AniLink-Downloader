using System.Net;
using System.Text.Json;
using AniLink.Api.Data.Downloads;
using AniLink.Api.Exceptions.DebridLink;
using AniLink.Api.Helpers;

namespace AniLink.Api.DebridLink;

// The one Debrid-Link endpoint the app needs: turn a hoster link into a direct download link. It is a singleton
// so the pause between two requests is kept across every job; Debrid-Link answers floods with an hour-long ban.
public sealed class DebridLinkClient(IHttpClientFactory httpClients, DebridLinkOptions options, ILogger<DebridLinkClient> logger)
{
    public const string ApiClient = "DebridLink";
    public const string FileClient = "DebridLinkFiles";

    private readonly SemaphoreSlim _turn = new(1, 1);
    private long _nextRequestAt;

    public async Task<DebridLinkFile> AddAsync(string url, CancellationToken cancellationToken)
    {
        await WaitForTurnAsync(cancellationToken);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "downloader/add")
            {
                Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("url", url)])
            };

            using var response = await httpClients.CreateClient(ApiClient).SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            var file = Parse(response.StatusCode, body);
            logger.LogInformation("Debrid-Link resolved {Name} ({Size} bytes) from {Host}", LogSafe.OneLine(file.Name), file.Size, LogSafe.OneLine(file.Host));

            return file;
        }
        catch (HttpRequestException exception)
        {
            throw new DebridLinkException("network", $"Debrid-Link could not be reached: {LogSafe.OneLine(exception.Message)}", true, exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DebridLinkException("timeout", "Debrid-Link did not answer in time.", true, exception);
        }
    }

    private async Task WaitForTurnAsync(CancellationToken cancellationToken)
    {
        await _turn.WaitAsync(cancellationToken);

        try
        {
            var wait = _nextRequestAt - Environment.TickCount64;
            if (wait > 0) await Task.Delay(TimeSpan.FromMilliseconds(wait), cancellationToken);

            _nextRequestAt = Environment.TickCount64 + options.RequestDelayMs;
        }
        finally
        {
            _turn.Release();
        }
    }

    private static DebridLinkFile Parse(HttpStatusCode status, string body)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            throw new DebridLinkException("badResponse", $"Debrid-Link answered HTTP {(int)status} with something that is not JSON.", (int)status >= 500);
        }

        using (document)
        {
            var root = document.RootElement;

            if (root.ValueKind is not JsonValueKind.Object || !root.TryGetProperty("success", out var success) || success.ValueKind is not JsonValueKind.True)
            {
                var code = root.ValueKind is JsonValueKind.Object && root.TryGetProperty("error", out var error) && error.ValueKind is JsonValueKind.String
                    ? error.GetString() ?? "unknown"
                    : $"http{(int)status}";

                throw DebridLinkFailures.For(code);
            }

            if (!root.TryGetProperty("value", out var value)) throw new DebridLinkException("badResponse", "Debrid-Link answered without a value.", false);

            // A folder link answers with an array of links; the app only ever asks for single files.
            if (value.ValueKind is JsonValueKind.Array) throw new DebridLinkException("multipleLinks", "This link is a folder with several files. Paste the links of the single files instead.", false);
            if (value.ValueKind is not JsonValueKind.Object) throw new DebridLinkException("badResponse", "Debrid-Link answered with an unexpected value.", false);

            if (Flag(value, "expired")) throw new DebridLinkException("expired", "Debrid-Link marked the link as expired right away.", true);
            if (Flag(value, "isProcessing")) throw new DebridLinkException("processing", "Debrid-Link is still preparing the file.", true);

            var downloadUrl = Text(value, "downloadUrl");
            if (string.IsNullOrWhiteSpace(downloadUrl)) throw new DebridLinkException("badResponse", "Debrid-Link answered without a download URL.", false);

            return new DebridLinkFile(
                Text(value, "id") ?? "",
                Text(value, "name") ?? "",
                Text(value, "url") ?? "",
                downloadUrl,
                Text(value, "host"),
                Number(value, "size"),
                Number(value, "created"),
                false,
                (int)Number(value, "chunk"));
        }
    }

    private static bool Flag(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.ValueKind is JsonValueKind.True;
    }

    private static string? Text(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.ValueKind is JsonValueKind.String ? property.GetString() : null;
    }

    private static long Number(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.ValueKind is JsonValueKind.Number && property.TryGetInt64(out var number) ? number : 0;
    }
}
