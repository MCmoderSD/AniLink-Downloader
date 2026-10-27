using System.Text.Json.Serialization;

namespace AniLink.Api.Data.Downloads;

[JsonConverter(typeof(JsonStringEnumConverter<ItemStage>))]
public enum ItemStage
{
    ResolvingLinks,
    Downloading,
    Extracting,
    PasswordRequired,
    Organizing,
    Completed,
    Failed,
    Cancelled,
    Paused
}
