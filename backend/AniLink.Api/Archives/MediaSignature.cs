using System.Text;

namespace AniLink.Api.Archives;

// The first bytes of the containers the organizer accepts. Used to tell a correctly decrypted video from the
// noise a wrong password produces.
internal static class MediaSignature
{
    private static readonly string[] Extensions = [".mkv", ".mp4"];

    public static bool IsKnownVideo(string? name)
    {
        return Extensions.Contains(Path.GetExtension(name ?? ""), StringComparer.OrdinalIgnoreCase);
    }

    // Anything that is not a known video cannot be judged by its first bytes, so it is given the benefit of
    // the doubt; the CRC check at the end of the unpacking has the last word on those.
    public static bool Matches(string? name, ReadOnlySpan<byte> head)
    {
        return Path.GetExtension(name ?? "").ToLowerInvariant() switch
        {
            ".mkv" => head.Length >= 4 && head[0] == 0x1A && head[1] == 0x45 && head[2] == 0xDF && head[3] == 0xA3,
            ".mp4" => head.Length >= 8 && head[4..8].SequenceEqual(Encoding.ASCII.GetBytes("ftyp")),
            _ => true
        };
    }
}
