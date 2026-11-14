namespace AniLink.Api.Data.Downloads;

public sealed record DownloadCreated(JobResponse Job);

public sealed record DownloadInvalid(string Reason);

public sealed record DownloadConflict(string Reason);

public union CreateOutcome(DownloadCreated, DownloadInvalid, DownloadConflict);