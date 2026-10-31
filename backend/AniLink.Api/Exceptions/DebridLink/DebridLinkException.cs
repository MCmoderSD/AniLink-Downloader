namespace AniLink.Api.Exceptions.DebridLink;

public sealed class DebridLinkException(string code, string message, bool retryable, Exception? inner = null) : Exception(message, inner)
{
    public string Code { get; } = code;

    public bool Retryable { get; } = retryable;
}