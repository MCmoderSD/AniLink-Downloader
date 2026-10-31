using System.Text.RegularExpressions;

namespace AniLink.Api.Helpers;

public static partial class LogSafe
{
    private const int Limit = 256;

    [GeneratedRegex(" {2,}")]
    private static partial Regex Spaces();

    public static string OneLine(string? value)
    {
        if (value is null) return "<null>";
        if (value.Length is 0) return "<empty>";

        var flattened = value
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

        var printable = new string(flattened.Select(character => char.IsControl(character) ? ' ' : character).ToArray());

        return printable.Length <= Limit ? printable : printable[..Limit] + "…";
    }

    public static string Excerpt(string? value, int limit, bool fromEnd = false)
    {
        var lines = (value ?? "")
            .Split('\n')
            .Select(line => Spaces().Replace(new string(line.Select(character => char.IsControl(character) ? ' ' : character).ToArray()), " ").Trim())
            .Where(line => line.Length > 0);

        var text = string.Join(" | ", lines);

        if (text.Length == 0) return "<empty>";
        if (text.Length <= limit) return text;

        return fromEnd ? "…" + text[^limit..] : text[..limit] + "…";
    }
}