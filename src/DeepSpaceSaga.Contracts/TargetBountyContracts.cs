using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

public sealed record AcceptBountyCommand(string CommandId, long ExpectedRevision, string BountyId)
    : DemoCampaignCommand(CommandId, ExpectedRevision);

public sealed record CancelBountyCommand(string CommandId, long ExpectedRevision, string BountyId)
    : DemoCampaignCommand(CommandId, ExpectedRevision);

/// <summary>Frozen order terms and engine-derived eligibility. Tracking never grants payment.</summary>
public sealed record BountyOrder(string BountyId, string IssuerStationId, string TargetGroupId,
    long RewardTokens, bool Tracking, string Eligibility, string? VictoryBattleId = null,
    string? SearchRegion = null, string? Reason = null, BountySearchArea? SearchArea = null);
public sealed record BountySearchArea(double X, double Y, double Radius);

public sealed record TargetBountySnapshot(ImmutableArray<BountyOrder> Orders);

public static class BountyEligibility
{
    public const string TargetAvailable = "target_available";
    public const string VictoryConfirmed = "victory_confirmed";
    public const string Unavailable = "unavailable";
    public const string Paid = "paid";
}

public static class TargetBountyReasons
{
    public const string NotFound = "bounty_not_found";
    public const string Unavailable = "bounty_unavailable";
    public const string AlreadyPaid = "bounty_already_paid";
}
