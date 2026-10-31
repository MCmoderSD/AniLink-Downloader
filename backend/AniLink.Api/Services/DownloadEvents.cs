using System.Text.Json;
using AniLink.Api.Data.Downloads;
using AniLink.Api.Services.Streaming;

namespace AniLink.Api.Services;

public static class DownloadEvents
{
    public static string Job(DownloadJob job)
    {
        return Serialize(new { type = "job", job = JobResponse.From(job) });
    }

    public static string Notice(DownloadJob job, ActivityEntry entry)
    {
        return Serialize(new { type = "notice", jobId = job.Id, job = job.Name, at = entry.At, level = entry.Level, text = entry.Text });
    }

    public static string Removed(Guid jobId)
    {
        return Serialize(new { type = "removed", jobId });
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, StreamJson.Options);
    }
}