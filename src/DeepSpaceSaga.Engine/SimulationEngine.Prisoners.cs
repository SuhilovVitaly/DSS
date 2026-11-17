using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    internal static string LootPilotIdentity(string battle, string drone, string pilot) => battle + "/" + drone + "/pilot/" + pilot;
    private int GetPrisonerCount(string shipId) => _demoCampaign.Prisoners.Count(p => p.ShipId == shipId);
    private int GetAvailablePrisonerCabins(SpaceObjectRuntime ship, DemoCampaignData state)
    {
        long total = ship.Modules.Where(m => !m.Destroyed && m.StructurePoints > 0)
            .Sum(m => (long)(_registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex).CabinesCount ?? 0));
        long promised = PassengerSeatsAwaitingBoarding(state.StationJobs?.Obligations.Where(o => o.Status == StationJobStatuses.Active) ?? []);
        return (int)Math.Max(0, total - ship.Crew.Length - (ship.Passengers.IsDefault ? 0 : ship.Passengers.Length) -
            state.Prisoners.Count(p => p.ShipId == ship.InitialMotion.ObjectId) - promised);
    }
    private string? ArrestReason(DemoCampaignData state, BattleLootData loot, LootSourceData? source)
    {
        if (loot.Phase != LootPhases.Open) return BattleLootReasons.LootClosed;
        if (source is not { Kind: "DisabledDrone", Pilot: { Alive: true } pilot }) return PrisonerCustodyReasons.NotCapturable;
        if (state.Prisoners.Any(p => p.SourceBattleId == loot.BattleId && p.SourceDroneId == source.ObjectId)) return PrisonerCustodyReasons.AlreadyCaptured;
        var ship = _objects.FirstOrDefault(s => s.InitialMotion.ObjectId == PlayerShipObjectId && !s.IsDestroyed);
        return ship is null ? DemoCampaignReasons.CarrierDestroyed : GetAvailablePrisonerCabins(ship, state) < 1 ? PrisonerCustodyReasons.NoCabin : null;
    }
    private void PrepareArrestPilot(ArrestPilotCommand command, ref DemoCampaignData candidate, ref string? reason)
    {
        if (candidate.LootSession is not { } loot || loot.BattleId != command.BattleId) { reason = BattleLootReasons.WrongBattle; return; }
        var source = loot.Sources.FirstOrDefault(s => s.ObjectId == command.DroneId);
        reason = ArrestReason(candidate, loot, source);
        if (reason is not null) return;
        var pilot = source!.Pilot!;
        candidate = candidate with { Prisoners = candidate.Prisoners.Add(new("prisoner:" + pilot.SourcePilotId,
            PlayerShipObjectId!, loot.BattleId, source.ObjectId, pilot.SourcePilotId, pilot.DisplayName, pilot.PortraitAssetId,
            _clock.GameTimeMs, pilot.Gender)) };
    }
    private LootPilotSnapshot? ProjectLootPilot(BattleLootData loot, LootSourceData source) => source.Pilot is not { } pilot ? null : pilot with
    {
        Captured = _demoCampaign.Prisoners.Any(p => p.SourceBattleId == loot.BattleId && p.SourceDroneId == source.ObjectId),
        CaptureReason = ArrestReason(_demoCampaign, loot, source)
    };
}
