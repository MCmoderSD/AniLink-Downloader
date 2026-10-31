namespace AniLink.Api.Data.Downloads;

public sealed record JobManifest(
    int Version,
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    ReleaseKind Kind,
    int? Season,
    string? Format,
    IReadOnlyList<string> Passwords,
    string? WorkingPassword,
    IReadOnlyList<ItemManifest> Items,
    IReadOnlyList<ActivityEntry> Activity
) {
    public const int CurrentVersion = 1;

    public static JobManifest From(DownloadJob job)
    {
        lock (job.Gate)
        {
            return new JobManifest(
                CurrentVersion,
                job.Id,
                job.Name,
                job.CreatedAt,
                job.Release.Kind,
                job.Release.Season,
                job.Release.Format,
                job.Passwords.Supplied,
                job.Passwords.Working,
                [.. job.Items.Select(ItemManifest.From)],
                job.Activity.Entries());
        }
    }
}

public sealed record ItemManifest(
    int Number,
    ItemStage Stage,
    string? Error,
    IReadOnlyList<string> Files,
    IReadOnlyList<PartManifest> Parts,
    bool Unpack = true
) {
    public static ItemManifest From(MediaItem item)
    {
        return new ItemManifest(item.Number, item.Stage, item.Error, item.Files, [.. item.Parts.Select(PartManifest.From)], item.UnpackWanted);
    }
}

public sealed record PartManifest(
    int Index,
    string Url,
    string FileName,
    int PartNumber,
    PartState State,
    long BytesTotal,
    int Attempts,
    string? Error,
    DebridLinkFile? Debrid
) {
    public static PartManifest From(ArchivePart part)
    {
        return new PartManifest(
            part.Index,
            part.Link.Url,
            part.Link.FileName,
            part.Link.PartNumber,
            part.State,
            part.BytesTotal,
            part.Attempts,
            part.Error,
            part.Debrid);
    }
}