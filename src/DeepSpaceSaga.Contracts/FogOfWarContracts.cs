namespace DeepSpaceSaga.Contracts;

public sealed record SetLootDebugRequest(string CommandId, long ExpectedRevision, int? EngineerBonusPp = null,
    string DestructionRolls = "auto", string Attempts = "auto") : DemoCampaignCommand(CommandId, ExpectedRevision);

public sealed record SetFogOfWarRequest(string CommandId, long ExpectedRevision, bool Enabled)
    : DemoCampaignCommand(CommandId, ExpectedRevision);

/// <summary>Effective process setting. A missing section is interpreted as normal fog enabled.</summary>
public sealed record FogOfWarSnapshot(bool Enabled = true);
