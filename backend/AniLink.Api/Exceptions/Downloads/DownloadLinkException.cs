namespace AniLink.Api.Exceptions.Downloads;

public sealed class DownloadLinkException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}