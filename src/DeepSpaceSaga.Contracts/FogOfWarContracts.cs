namespace DeepSpaceSaga.Contracts;

public sealed record SetFogOfWarRequest(string CommandId, long ExpectedRevision, bool Enabled)
    : DemoCampaignCommand(CommandId, ExpectedRevision);

/// <summary>Effective process setting. A missing section is interpreted as normal fog enabled.</summary>
public sealed record FogOfWarSnapshot(bool Enabled = true);
