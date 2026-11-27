using System.Text.Json;
using AniLink.Api.Services.Streaming;

namespace AniLink.Api.Services.Files;

public static class FileEvents
{
    public static string Changed(IEnumerable<string> paths)
    {
        return Serialize(new { type = "changed", paths });
    }

    public static string Reset()
    {
        return Serialize(new { type = "reset" });
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, StreamJson.Options);
    }
}