using System.ComponentModel.DataAnnotations;

namespace AniLink.Api.Data.Downloads;

public sealed record AnalyzeRequest
{
    public ReleaseKind Kind { get; init; }

    [MaxLength(DownloadLimits.MaxLinksLength)]
    public string Links { get; init; } = "";

    [MaxLength(DownloadLimits.MaxFormatLength)]
    public string? Format { get; init; }
}

public sealed record CreateDownloadRequest
{
    [Required(AllowEmptyStrings = false)]
    [MaxBytes(DownloadLimits.MaxNameBytes)]
    public string Name { get; init; } = "";

    public ReleaseKind Kind { get; init; }

    [Required(AllowEmptyStrings = false)]
    [MaxLength(DownloadLimits.MaxLinksLength)]
    public string Links { get; init; } = "";

    [MaxLength(DownloadLimits.MaxFormatLength)]
    public string? Format { get; init; }

    [Range(0, 999)]
    public int? Season { get; init; }

    [MaxLength(DownloadLimits.MaxPasswordLength)]
    public string? Password { get; init; }
}

public sealed record PasswordRequest
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(DownloadLimits.MaxPasswordLength)]
    public string Password { get; init; } = "";
}

public static class DownloadLimits
{
    public const int MaxNameBytes = 251;
    public const int MaxFormatLength = 255;
    public const int MaxPasswordLength = 127;
    public const int MaxLinksLength = 1_000_000;
}