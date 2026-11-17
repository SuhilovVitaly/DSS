using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

public sealed record ClaimBountyCommand(string CommandId, long ExpectedRevision, string BountyId)
    : DemoCampaignCommand(CommandId, ExpectedRevision);

public sealed record BountyPaymentEntry(string BountyId, string IssuerStationId, string TargetGroupId,
    string? VictoryBattleId, string State, long DueTokens, long PaidTokens, string? PostingId,
    long? PaidAtGameTimeMs, string? Reason);

public sealed record BountyPaymentSnapshot(ImmutableArray<BountyPaymentEntry> Entries);

public static class BountyPaymentStates
{
    public const string Unavailable = "unavailable";
    public const string Claimable = "claimable";
    public const string Paid = "paid";
}

public static class BountyPaymentReasons
{
    public const string NoVerifiedVictory = "bounty_no_verified_victory";
    public const string WrongStation = "bounty_wrong_station";
    public const string CreditOverflow = "bounty_credit_overflow";
}
