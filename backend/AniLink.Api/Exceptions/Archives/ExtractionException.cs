namespace AniLink.Api.Exceptions.Archives;

public sealed class ExtractionException(string message, Exception? inner = null) : Exception(message, inner)
{
    public bool PasswordProblem { get; init; }
}