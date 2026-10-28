using System.Text.RegularExpressions;

namespace AniLink.Api.Data.Downloads;

public sealed partial record ArchiveLink(string Url, string FileName, int PartNumber)
{
    [GeneratedRegex(@"part0*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex PartPattern();

    [GeneratedRegex(@"\.r(\d{2,3})$", RegexOptions.IgnoreCase)]
    private static partial Regex VolumePattern();

    [GeneratedRegex(@"\.(rar|r\d{2,3}|7z|zip|\d{3})$", RegexOptions.IgnoreCase)]
    private static partial Regex ArchiveExtension();

    public bool LooksLikeArchive => ArchiveExtension().IsMatch(FileName);

    public static ArchiveLink Parse(string url)
    {
        var fileName = FileNameOf(url);

        return new ArchiveLink(url, fileName, PartNumberOf(fileName));
    }

    public static bool IsWebUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && !string.IsNullOrEmpty(uri.Host);
    }

    private static string FileNameOf(string url)
    {
        var path = url;

        var cut = path.IndexOfAny(['?', '#']);
        if (cut >= 0) path = path[..cut];

        path = path.TrimEnd('/');
        var name = path[(path.LastIndexOf('/') + 1)..];

        try
        {
            name = Uri.UnescapeDataString(name);
        }
        catch (UriFormatException)
        {
            // A stray percent sign: the raw segment is still a usable name.
        }

        return name.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ? name[..^".html".Length] : name;
    }

    private static int PartNumberOf(string fileName)
    {
        var volume = VolumePattern().Match(fileName);
        if (volume.Success && int.TryParse(volume.Groups[1].ValueSpan, out var old)) return old + 1;

        var part = PartPattern().Match(fileName);
        return part.Success && int.TryParse(part.Groups[1].ValueSpan, out var number) ? number : 0;
    }
}