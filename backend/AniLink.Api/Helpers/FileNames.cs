namespace AniLink.Api.Helpers;

public static class FileNames
{
    private const string Forbidden = "\\/:*?\"<>|";

    public static string Sanitize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";

        var replaced = new string(name
            .Select(character => char.IsControl(character) || Forbidden.Contains(character) ? '-' : character)
            .ToArray());

        return replaced.Trim().TrimEnd('.', ' ');
    }
}