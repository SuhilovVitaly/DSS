namespace DeepSpaceSaga.Contracts;

/// <summary>Identity and optimistic concurrency shared by explicitly supported campaign actions.</summary>
public abstract record DemoCampaignCommand(string CommandId, long ExpectedRevision);

public sealed record DemoCampaignResponse(string CommandId, bool Applied, string? Reason, long Revision,
    StationRepairSnapshot? StationRepair = null, SupplySnapshot? Supply = null, ReplacementSnapshot? Replacement = null,
    RecruitmentSnapshot? Recruitment = null, CrewUpkeepSnapshot? Upkeep = null);

/// <summary>Absent sections indicate unavailable features, never implicit resources or entitlements.</summary>
public sealed record DemoCampaignSnapshot(
    long Revision,
    long CommitSequence,
    DemoCompanySnapshot? Company = null,
    DemoTransportSnapshot? Transport = null,
    DemoEncountersSnapshot? Encounters = null,
    DemoRewardsSnapshot? Rewards = null,
    DemoProgressionSnapshot? Progression = null,
    DemoStorySnapshot? Story = null,
    DemoStartSnapshot? Start = null);

public sealed record DemoCompanySnapshot(CampaignRoundtripSnapshot? Roundtrip = null, PilotLossSnapshot? PilotLoss = null,
    CampaignOutcomeSnapshot? Outcome = null, StationRepairSnapshot? StationRepair = null, SupplySnapshot? Supply = null,
    ReplacementSnapshot? Replacement = null, RecruitmentSnapshot? Recruitment = null, CrewUpkeepSnapshot? Upkeep = null);
public sealed record DemoTransportSnapshot;
public sealed record DemoEncountersSnapshot;
public sealed record DemoRewardsSnapshot;
public sealed record DemoProgressionSnapshot;
public sealed record DemoStorySnapshot;

public static class DemoCampaignReasons
{
    public const string Unsupported = "unsupported";
    public const string InvalidIdentity = "invalid_identity";
    public const string IdentityConflict = "identity_conflict";
    public const string StaleRevision = "stale_revision";
    public const string CarrierDestroyed = "carrier_destroyed";
    public const string BattleActive = "battle_active";
    public const string Unavailable = "unavailable";
    public const string AlreadyApplied = "already_applied";
}
