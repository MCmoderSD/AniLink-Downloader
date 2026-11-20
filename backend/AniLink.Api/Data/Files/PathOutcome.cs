namespace AniLink.Api.Data.Files;

public sealed record PathResolved(string Full, string Relative);

public sealed record PathInvalid(string Reason);

public sealed record PathMissing;

public union PathOutcome(PathResolved, PathInvalid, PathMissing);