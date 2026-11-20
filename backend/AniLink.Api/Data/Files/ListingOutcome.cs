namespace AniLink.Api.Data.Files;

public sealed record ListingFound(FolderListingResponse Listing);

public sealed record ListingInvalid(string Reason);

public sealed record ListingMissing;

public sealed record ListingUnavailable(string Reason);

public union ListingOutcome(ListingFound, ListingInvalid, ListingMissing, ListingUnavailable);