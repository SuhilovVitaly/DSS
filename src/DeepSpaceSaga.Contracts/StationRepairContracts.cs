using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

public sealed record RepairQuoteRequest(string CommandId, long ExpectedRevision, string TargetId)
    : DemoCampaignCommand(CommandId, ExpectedRevision);
public sealed record RepairCommand(string CommandId, long ExpectedRevision, string QuoteId, string TargetId)
    : DemoCampaignCommand(CommandId, ExpectedRevision);
public sealed record RepairCondition(int Hull, int Armor, int DamagedEngines, bool Jammed);
public sealed record RepairLine(string Kind, long Quantity, long UnitPrice, long Total);
public sealed record RepairQuote(string Id, string StationId, string TargetId, long Revision,
    long ExpiresAt, long Total, ImmutableArray<RepairLine> Items, RepairCondition Before);
public sealed record RepairResult(string TargetId, RepairCondition Before, RepairCondition After, string PostingId, long Paid);
public sealed record CompanyServicePosting(string Id, string Kind, string TargetId, long GameTimeMs, long Amount);
public sealed record StationRepairSnapshot(RepairQuote? Quote = null, RepairResult? LastResult = null,
    string? Reason = null, ImmutableArray<CompanyServicePosting> Postings = default);

public static class CompanyServiceReasons
{
    public const string NotDocked = "service_not_docked";
    public const string Unavailable = "service_unavailable";
    public const string InvalidTarget = "service_invalid_target";
    public const string Destroyed = "service_destroyed";
    public const string NoDamage = "service_no_damage";
    public const string InvalidQuote = "service_invalid_quote";
    public const string InsufficientFunds = "service_insufficient_funds";
}
