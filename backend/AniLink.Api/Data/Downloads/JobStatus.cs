using System.Text.Json.Serialization;

namespace AniLink.Api.Data.Downloads;

[JsonConverter(typeof(JsonStringEnumConverter<JobStatus>))]
public enum JobStatus
{
    Active,
    Paused,
    PasswordRequired,
    Failed,
    Cancelled,
    Downloaded,
    Completed
}