using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

public sealed record ArrestPilotCommand(string CommandId, long ExpectedRevision, string BattleId, string DroneId)
    : DemoCampaignCommand(CommandId, ExpectedRevision), ILootCampaignCommand;
public sealed record PrisonerSnapshot(string Id, string ShipId, string SourceBattleId, string SourceDroneId,
    string SourcePilotId, string DisplayName, string? PortraitAssetId, long CapturedAt, CrewGender Gender = CrewGender.Female);
public sealed record PrisonerCustodySnapshot(ImmutableArray<PrisonerSnapshot> Prisoners);
public static class PrisonerCustodyReasons
{
    public const string NoCabin = "prisoner_no_cabin";
    public const string NotCapturable = "prisoner_not_capturable";
    public const string AlreadyCaptured = "prisoner_already_captured";
}
