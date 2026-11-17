namespace DeepSpaceSaga.Contracts;

public enum CampaignOutcomeKind { Active, CarrierDestroyed }

/// <summary>World continuation availability, not a claim that a disk checkpoint exists.</summary>
public sealed record CampaignOutcomeSnapshot(CampaignOutcomeKind Status, string? LastCommittedBattleId,
    bool ContinuationAvailable, string? RecoveryReason);
