namespace AniLink.Api.Data.Downloads;

// Only what the page needs to explain itself. Nothing in here is a secret: the API key and the default
// passwords never leave the server, the page learns just how many passwords are tried on its behalf.
public sealed record ConfigResponse(int MaxConcurrency, int DefaultPasswords, string DownloadsRoot);
