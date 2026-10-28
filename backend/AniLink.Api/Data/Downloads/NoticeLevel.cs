using System.Text.Json.Serialization;

namespace AniLink.Api.Data.Downloads;

[JsonConverter(typeof(JsonStringEnumConverter<NoticeLevel>))]
public enum NoticeLevel
{
    Info,
    Success,
    Warning,
    Error
}