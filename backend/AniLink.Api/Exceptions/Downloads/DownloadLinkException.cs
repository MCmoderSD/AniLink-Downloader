namespace AniLink.Api.Exceptions.Downloads;

// The hoster refused the direct download link (expired, revoked, never valid). A fresh link from Debrid-Link
// may help; asking the same URL again will not.
public sealed class DownloadLinkException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
