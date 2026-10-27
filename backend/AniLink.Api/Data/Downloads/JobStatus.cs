using System.Text.Json.Serialization;

namespace AniLink.Api.Data.Downloads;

[JsonConverter(typeof(JsonStringEnumConverter<JobStatus>))]
public enum JobStatus
{
    Active,
    PasswordRequired,
    Failed,
    Cancelled,
    Completed
}
