namespace AniLink.Api.Data.Downloads;

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