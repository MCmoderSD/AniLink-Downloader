using System.Text.Json.Serialization;

namespace AniLink.Api.Data.Downloads;

[JsonConverter(typeof(JsonStringEnumConverter<PartState>))]
public enum PartState
{
    Pending,
    Resolving,
    Resolved,
    Downloading,
    Downloaded,
    Failed
}
