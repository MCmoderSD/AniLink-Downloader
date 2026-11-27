using JetBrains.Annotations;

namespace AniLink.Api.Data.Files;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record TreeNodeResponse(
    string Name,
    string Path,
    FileEntryKind Kind,
    bool HasChildren
);

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed record TreeResponse(
    string Path,
    IReadOnlyList<TreeNodeResponse> Nodes
);

public sealed record TreeFound(TreeResponse Tree);

public union TreeOutcome(TreeFound, ListingInvalid, ListingMissing, ListingUnavailable);