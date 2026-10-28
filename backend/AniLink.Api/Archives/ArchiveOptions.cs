namespace AniLink.Api.Archives;

public sealed class ArchiveOptions
{
    public IReadOnlyList<string> Passwords { get; init; } = [];

    // Path of the 7-Zip console binary. In Docker it is 7zz on the PATH; on a development machine it is the
    // 7z.exe of an installed 7-Zip.
    public string SevenZipPath { get; init; } = "";

    public static ArchiveOptions From(IConfiguration configuration)
    {
        var section = configuration.GetSection("Archive");

        // Compose passes every entry as Archive_Password_<n>, and an entry whose variable is unset arrives
        // as an empty string. Those are not passwords, so they are dropped here.
        var passwords = section.GetSection("Passwords").Get<string[]>() ?? [];

        var path = section["SevenZipPath"];

        return new ArchiveOptions
        {
            Passwords = [.. passwords.Where(password => !string.IsNullOrWhiteSpace(password)).Distinct(StringComparer.Ordinal)],
            SevenZipPath = string.IsNullOrWhiteSpace(path) ? "7zz" : path.Trim()
        };
    }
}
