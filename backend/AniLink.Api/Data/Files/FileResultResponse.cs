using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace AniLink.Api.Data.Files;

[JsonConverter(typeof(JsonStringEnumConverter<FileOutcome>))]
public enum FileOutcome
{
    Done,
    Locked,
    Missing,
    Invalid,
    Failed
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record FileResultResponse(
    string Path,
    FileOutcome Outcome,
    string? Reason,
    IReadOnlyList<FileLockResponse> LockedBy
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record FileBatchResponse(IReadOnlyList<FileResultResponse> Results);