namespace AniLink.Api.Exceptions.Downloads;

public sealed class DownloadConflictException(string message) : Exception(message);
