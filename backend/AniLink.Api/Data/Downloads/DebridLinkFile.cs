namespace AniLink.Api.Data.Downloads;

// What Debrid-Link answered for one link. It is stored in the job manifest, so a restarted container can
// keep downloading without asking (and paying quota) for the same links again.
public sealed record DebridLinkFile(
    string Id,
    string Name,
    string Url,
    string DownloadUrl,
    string? Host,
    long Size,
    long Created,
    bool Expired,
    int Chunk
);
