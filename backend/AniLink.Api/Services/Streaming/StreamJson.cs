using System.Text.Json;

namespace AniLink.Api.Services.Streaming;

public static class StreamJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}