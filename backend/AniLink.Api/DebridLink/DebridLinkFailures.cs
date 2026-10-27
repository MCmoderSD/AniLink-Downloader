using AniLink.Api.Exceptions.DebridLink;
using AniLink.Api.Helpers;

namespace AniLink.Api.DebridLink;

// Maps the error codes of the Debrid-Link API to a readable message and to whether asking again can help.
internal static class DebridLinkFailures
{
    public static DebridLinkException For(string code)
    {
        var safe = LogSafe.OneLine(code);

        return code switch
        {
            "badToken" => new DebridLinkException(code, "Debrid-Link rejected the API key (badToken). Check DebridLink__ApiKey.", false),
            "floodDetected" => new DebridLinkException(code, "Debrid-Link blocks the account for about an hour (floodDetected). Raise DebridLink__RequestDelayMs.", false),
            "unverifiedEmail" => new DebridLinkException(code, "Debrid-Link wants the account email verified (unverifiedEmail).", false),
            "hostNotValid" => new DebridLinkException(code, "Debrid-Link does not support this host (hostNotValid).", false),
            "notFreeHost" => new DebridLinkException(code, "This host needs a premium Debrid-Link account (notFreeHost).", false),
            "fileNotFound" => new DebridLinkException(code, "The file is gone from the hoster (fileNotFound).", false),
            "badFileUrl" => new DebridLinkException(code, "Debrid-Link does not understand this link (badFileUrl).", false),
            "badFilePassword" => new DebridLinkException(code, "The hoster protects this file with a password (badFilePassword).", false),
            "infringingFile" => new DebridLinkException(code, "The file is unavailable for legal reasons (infringingFile).", false),
            "maxLink" or "maxLinkHost" => new DebridLinkException(code, $"Debrid-Link daily link limit reached ({safe}).", false),
            "maxData" or "maxDataHost" => new DebridLinkException(code, $"Debrid-Link daily data limit reached ({safe}).", false),
            "fileNotAvailable" => new DebridLinkException(code, "The file is temporarily unavailable on the hoster (fileNotAvailable).", true),
            "maintenanceHost" => new DebridLinkException(code, "The hoster is under maintenance (maintenanceHost).", true),
            "internalError" or "freeServerOverload" or "notDebrid" => new DebridLinkException(code, $"Debrid-Link has a temporary problem ({safe}).", true),
            _ => new DebridLinkException(code, $"Debrid-Link answered with the error {safe}.", code.StartsWith("http5", StringComparison.Ordinal))
        };
    }
}
