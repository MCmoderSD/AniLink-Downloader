namespace AniLink.Api.Exceptions.DebridLink;

// Retryable means asking again later can help (overload, maintenance, a flaky connection). Everything else
// is a verdict on the link or the account, and repeating the request would only burn the daily quota.
public sealed class DebridLinkException(string code, string message, bool retryable, Exception? inner = null)
    : Exception(message, inner)
{
    public string Code { get; } = code;

    public bool Retryable { get; } = retryable;
}
