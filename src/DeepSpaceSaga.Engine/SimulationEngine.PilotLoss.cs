using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private (SpaceObjectRuntime Ship, DemoCampaignData Campaign) PreparePilotLosses(
        string shipId, string battleId, IEnumerable<OwnedDroneData> wing, IReadOnlyCollection<string> lostIds)
    {
        var ship = _objects.Single(o => o.InitialMotion.ObjectId == shipId);
        var lostPilots = wing.Where(d => lostIds.Contains(d.DroneId) && !string.IsNullOrEmpty(d.CrewId))
            .Select(d => d.CrewId).ToHashSet(StringComparer.Ordinal);
        var memorial = _demoCampaign.PilotMemorial.ToBuilder();
        foreach (var pilot in BuildCrewProjection(ship).Where(p => lostPilots.Contains(p.Id)))
            if (!memorial.Any(m => m.Pilot.Id == pilot.Id))
                memorial.Add(new(pilot, battleId, _clock.GameTimeMs));
        return (ship with
        {
            Crew = ship.Crew.Where(c => !lostPilots.Contains(c.Id)).ToImmutableArray(),
            Modules = ship.Modules.Select(m => m.OperatorCrewId is { } crewId && lostPilots.Contains(crewId)
                ? m with { OperatorCrewId = null } : m).ToImmutableArray()
        },
            _demoCampaign with { PilotMemorial = memorial.ToImmutable() });
    }

    private PilotLossSnapshot BuildPilotLossProjection()
    {
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        return new(_demoCampaign.PilotMemorial, ship is null || ship.Crew.IsDefaultOrEmpty
            ? [] : ship.Crew.Select(c => c.Id).ToImmutableArray());
    }

    private static void ValidatePilotLossForLoad(DemoCampaignData data, GameStateData state)
    {
        var dead = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in data.PilotMemorial)
        {
            if (entry?.Pilot is not { } pilot || string.IsNullOrWhiteSpace(pilot.Id) ||
                string.IsNullOrWhiteSpace(pilot.DisplayName) || !dead.Add(pilot.Id) ||
                string.IsNullOrWhiteSpace(entry.BattleId) || entry.GameTimeMs < 0 || entry.GameTimeMs > state.GameTimeMs ||
                pilot.Accuracy < 0 || pilot.Maneuver < 0 || pilot.Initiative < 0)
                throw new ScenarioException("Invalid pilot memorial.");
        }
        foreach (var obj in state.SpaceObjects)
        {
            foreach (var pilot in obj.Crew ?? [])
                if (dead.Contains(pilot.CrewId)) throw new ScenarioException("Pilot is both alive and memorialized.");
            foreach (var drone in obj.CarrierState?.OwnedDrones ?? [])
                if (dead.Contains(drone.CrewId)) throw new ScenarioException("Memorialized pilot remains assigned to a drone.");
        }
        foreach (string id in dead.Concat(state.SpaceObjects.SelectMany(o => o.Crew ?? []).Select(c => c.CrewId)))
            if (id.StartsWith("CHR-DEMO-", StringComparison.Ordinal) && long.TryParse(id.AsSpan(9), out long sequence) &&
                sequence >= data.NextCrewSequence)
                throw new ScenarioException("Crew identity counter would reuse an existing identity.");
    }
}
