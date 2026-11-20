using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    internal static string LootPilotIdentity(string battle, string drone, string pilot) => battle + "/" + drone + "/pilot/" + pilot;
    private int GetPrisonerCount(string shipId) => _demoCampaign.Prisoners.Count(p => p.ShipId == shipId);
    private int GetAvailablePrisonerCabins(SpaceObjectRuntime ship, DemoCampaignData state)
    {
        return BuildCabinReservationProjection(ship, state).FreeCabins;
    }
    private string? ArrestReason(DemoCampaignData state, BattleLootData loot, LootSourceData? source, int? freeCabins = null, bool? shipAvailable = null)
    {
        if (loot.Phase != LootPhases.Open) return BattleLootReasons.LootClosed;
        if (source is not { Kind: "DisabledDrone", Pilot: { Alive: true } pilot }) return PrisonerCustodyReasons.NotCapturable;
        if (state.Prisoners.Any(p => p.SourceBattleId == loot.BattleId && p.SourceDroneId == source.ObjectId)) return PrisonerCustodyReasons.AlreadyCaptured;
        if (shipAvailable is { } available)
            return !available ? DemoCampaignReasons.CarrierDestroyed : freeCabins < 1 ? PrisonerCustodyReasons.NoCabin : null;
        var ship = _objects.FirstOrDefault(s => s.InitialMotion.ObjectId == PlayerShipObjectId && !s.IsDestroyed);
        return ship is null ? DemoCampaignReasons.CarrierDestroyed : GetAvailablePrisonerCabins(ship, state) < 1 ? PrisonerCustodyReasons.NoCabin : null;
    }
    private void PrepareArrestPilot(ArrestPilotCommand command, ref DemoCampaignData candidate, ref string? reason)
    {
        if (candidate.LootSession is not { } loot || loot.BattleId != command.BattleId) { reason = BattleLootReasons.WrongBattle; return; }
        var source = loot.Sources.FirstOrDefault(s => s.ObjectId == command.DroneId);
        reason = ArrestReason(candidate, loot, source);
        if (reason is not null) return;
        var debugSources = candidate.DebugSortiePrisonerSources;
        if (loot.DebugSortie && !debugSources.Any(entry => entry.BattleId == loot.BattleId))
        {
            var map = loot.Map;
            debugSources = debugSources.Add(new(loot.BattleId, PlayerShipObjectId!, map.PlayerSide,
                map.Units.Where(unit => unit.Side != map.PlayerSide && unit.Kind == TacticalUnitKind.Drone &&
                    unit.State == TacticalUnitState.Disabled).ToImmutableArray()));
        }
        var pilot = source!.Pilot!;
        candidate = candidate with
        {
            DebugSortiePrisonerSources = debugSources,
            Prisoners = candidate.Prisoners.Add(new("prisoner:" + pilot.SourcePilotId,
            PlayerShipObjectId!, loot.BattleId, source.ObjectId, pilot.SourcePilotId, pilot.DisplayName, pilot.PortraitAssetId,
            _clock.GameTimeMs, pilot.Gender))
        };
    }
    private LootPilotSnapshot? ProjectLootPilot(BattleLootData loot, LootSourceData source, SpaceObjectRuntime? ship, int freeCabins) => source.Pilot is not { } pilot ? null : pilot with
    {
        Captured = _demoCampaign.Prisoners.Any(p => p.SourceBattleId == loot.BattleId && p.SourceDroneId == source.ObjectId),
        CaptureReason = ArrestReason(_demoCampaign, loot, source, freeCabins, ship is not null)
    };
}
