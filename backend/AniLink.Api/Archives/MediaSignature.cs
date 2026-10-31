using System.Text;

namespace AniLink.Api.Archives;

internal static class MediaSignature
{
    private static readonly string[] Extensions = [".mkv", ".mp4"];

    public static bool IsKnownVideo(string? name)
    {
        return Extensions.Contains(Path.GetExtension(name ?? ""), StringComparer.OrdinalIgnoreCase);
    }

    public static bool Matches(string? name, ReadOnlySpan<byte> head)
    {
        return Path.GetExtension(name ?? "").ToLowerInvariant() switch
        {
            ".mkv" => head.Length >= 4 && head[0] == 0x1A && head[1] == 0x45 && head[2] == 0xDF && head[3] == 0xA3,
            ".mp4" => head.Length >= 8 && head[4..8].SequenceEqual("ftyp"u8),
            _ => true
        };
    }
}