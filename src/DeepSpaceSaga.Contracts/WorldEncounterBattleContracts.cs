namespace DeepSpaceSaga.Contracts;

public sealed record WorldBattleBinding(string BattleId, string EncounterId, string GroupId);
public sealed record WorldEncounterSnapshot(WorldBattleBinding Binding, string Phase, bool CanContinue,
    string? UnavailableReason = null);
public sealed record BeginEncounterBattleCommand(string CommandId, long ExpectedRevision, string EncounterId)
    : DemoCampaignCommand(CommandId, ExpectedRevision);

public static class WorldEncounterPhases
{
    public const string Captured = "captured";
    public const string PendingBattle = "pending_battle";
    public const string Finished = "finished";
}
