namespace AniLink.Api.Helpers;

public static class FileNames
{
    private const string Forbidden = "\\/:*?\"<>|";

    // Turns a user-typed title into one path segment. Windows rules are applied everywhere, so a library
    // written on Linux can still be opened from a Windows share.
    public static string Sanitize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";

        var replaced = new string(name
            .Select(character => char.IsControl(character) || Forbidden.Contains(character) ? '-' : character)
            .ToArray());

        return replaced.Trim().TrimEnd('.', ' ');
    }
}
