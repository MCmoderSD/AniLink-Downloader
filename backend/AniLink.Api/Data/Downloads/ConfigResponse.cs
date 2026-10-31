using JetBrains.Annotations;

namespace AniLink.Api.Data.Downloads;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record ConfigResponse(int MaxConcurrency, int DefaultPasswords, string DownloadsRoot, LimitsResponse Limits);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record LimitsResponse(int NameBytes, int FormatLength, int PasswordLength, int LinksLength)
{
    public static LimitsResponse Current => new(DownloadLimits.MaxNameBytes, DownloadLimits.MaxFormatLength, DownloadLimits.MaxPasswordLength, DownloadLimits.MaxLinksLength);
}