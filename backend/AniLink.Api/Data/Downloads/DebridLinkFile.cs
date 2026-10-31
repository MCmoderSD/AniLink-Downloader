namespace AniLink.Api.Data.Downloads;

public sealed record DebridLinkFile(string Name, string DownloadUrl, string? Host, long Size);