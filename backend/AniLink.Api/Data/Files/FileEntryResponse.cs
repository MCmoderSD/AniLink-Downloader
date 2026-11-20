using System.Text.Json.Serialization;
using AniLink.Api.Data.Downloads;
using JetBrains.Annotations;

namespace AniLink.Api.Data.Files;

[JsonConverter(typeof(JsonStringEnumConverter<FileEntryKind>))]
public enum FileEntryKind
{
    Folder,
    File,
    Link
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record FileLockResponse(
    Guid JobId,
    string JobName,
    int? Season,
    JobStatus Status
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record FileEntryResponse(
    string Name,
    string Path,
    FileEntryKind Kind,
    long Size,
    int? Files,
    DateTimeOffset ModifiedAt,
    IReadOnlyList<FileLockResponse> LockedBy
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record FolderListingResponse(
    string Path,
    string Root,
    IReadOnlyList<FileEntryResponse> Entries
);