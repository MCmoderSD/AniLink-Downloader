using System.Text.Json.Serialization;

namespace AniLink.Api.Data.Downloads;

[JsonConverter(typeof(JsonStringEnumConverter<ReleaseKind>))]
public enum ReleaseKind
{
    Season,
    Movie
}
