namespace AniLink.Api.Archives;

public sealed class ArchiveOptions
{
    public IReadOnlyList<string> Passwords { get; private init; } = [];

    public string SevenZipPath { get; private init; } = "";

    public static ArchiveOptions From(IConfiguration configuration)
    {
        var section = configuration.GetSection("Archive");

        var passwords = section.GetSection("Passwords").Get<string[]>() ?? [];

        var path = section["SevenZipPath"];

        return new ArchiveOptions
        {
            Passwords = [.. passwords.Where(password => !string.IsNullOrWhiteSpace(password)).Distinct(StringComparer.Ordinal)],
            SevenZipPath = string.IsNullOrWhiteSpace(path) ? "7zz" : path.Trim()
        };
    }
}