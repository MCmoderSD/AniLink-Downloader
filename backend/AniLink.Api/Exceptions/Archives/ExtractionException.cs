namespace AniLink.Api.Exceptions.Archives;

public sealed class ExtractionException(string message, Exception? inner = null) : Exception(message, inner)
{
    // True when the failure looks like a wrong password (bad decryption, CRC mismatch on an encrypted file),
    // which the pipeline answers by asking the user instead of failing the item.
    public bool PasswordProblem { get; init; }
}
