namespace AniLink.Api.Data.Downloads;

public sealed record ConfigResponse(int MaxConcurrency, int DefaultPasswords, string DownloadsRoot);