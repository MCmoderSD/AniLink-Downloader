namespace AniLink.Api.Data.Downloads;

public sealed record ActionDone(JobResponse Job);

public sealed record ActionRejected(string Reason);

public sealed record ActionMissing;

public union ActionOutcome(ActionDone, ActionRejected, ActionMissing);