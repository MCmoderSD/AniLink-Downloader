namespace AniLink.Api.Exceptions.Downloads;

public sealed class InvalidDownloadException(string message) : Exception(message);