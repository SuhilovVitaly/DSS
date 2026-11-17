using DeepSpaceSaga.Contracts;
using System.Collections.Immutable;

namespace DeepSpaceSaga.Engine.Scenario;

public sealed record WorldEncounterData(WorldBattleBinding Binding, long Seed, string Phase);
public sealed record WorldEncounterResultData(WorldBattleBinding Binding, bool PlayerWon,
    TacticalOutcomeSnapshot Outcome, ImmutableArray<TacticalUnitSnapshot> Units);

public sealed partial record DemoCampaignData
{
    private static void ValidateEncounterHistory(DemoCampaignData data)
    {
        if (data.EncounterResults.IsDefault || data.EncounterResults.Length > 4 || data.EncounterResults.Any(r => r is null || r.Binding is null ||
            r.Outcome is null || !Enum.IsDefined(r.Outcome.Kind) || r.PlayerWon != (r.Outcome.Kind == TacticalBattleOutcomeCode.A) ||
            r.Binding.BattleId != "battle:" + r.Binding.GroupId || r.Binding.EncounterId != "encounter:" + r.Binding.GroupId ||
            r.Units.IsDefaultOrEmpty || r.Units.Any(u => u is null || string.IsNullOrWhiteSpace(u.Id) || u.Hull < 0 || u.Hull > u.MaxHull ||
                !Enum.IsDefined(u.Kind) || !Enum.IsDefined(u.State) || u.Side is not ("A" or "B")) ||
            r.Units.Select(u => u.Id).Distinct(StringComparer.Ordinal).Count() != r.Units.Length) ||
            data.EncounterResults.Select(r => r.Binding.GroupId).Distinct(StringComparer.Ordinal).Count() != data.EncounterResults.Length)
            throw new ScenarioException("Invalid encounter result history.");
        if (data.PirateWorld is not { } world)
        {
            if (!data.EncounterResults.IsEmpty || data.WorldEncounter is not null) throw new ScenarioException("Encounter history requires pirate world.");
            return;
        }
        if (world.Groups.IsDefault || world.Groups.Any(g => g is null) || data.PirateCompositions.IsDefault ||
            data.PirateCompositions.Any(c => c is null || c.Drones.IsDefault || c.Drones.Any(d => d is null)))
            throw new ScenarioException("Invalid encounter world or composition.");
        foreach (var group in world.Groups)
        {
            var result = data.EncounterResults.SingleOrDefault(r => r.Binding.GroupId == group.GroupId);
            if (group.Retired != (result is not null)) throw new ScenarioException("Retirement must match encounter result.");
            if (result is null) continue;
            var enemies = result.Units.Where(u => u.Side == "B").ToArray();
            if (!enemies.Where(u => u.Kind == TacticalUnitKind.Carrier).Select(u => u.Id).Order(StringComparer.Ordinal)
                .SequenceEqual(group.Members.Select(m => m.CarrierId).Order(StringComparer.Ordinal)) ||
                result.Units.Count(u => u.Side == "A" && u.Kind == TacticalUnitKind.Carrier) != 1)
                throw new ScenarioException("Encounter carrier result identity mismatch.");
            var compositions = data.PirateCompositions.Where(c => c.GroupId == group.GroupId).ToArray();
            if (!enemies.Where(u => u.Kind == TacticalUnitKind.Drone).Select(u => u.Id).Order(StringComparer.Ordinal)
                .SequenceEqual(compositions.SelectMany(c => c.Drones).Select(d => d.DroneId).Order(StringComparer.Ordinal)) ||
                enemies.Where(u => u.Kind == TacticalUnitKind.Drone).Any(u => !compositions.Any(c => c.CarrierId == u.OriginCarrierId && c.Drones.Any(d => d.DroneId == u.Id))) ||
                group.Members.Any(m => m.IsDestroyed != (enemies.Single(u => u.Id == m.CarrierId).Hull <= 0)))
                throw new ScenarioException("Encounter drone result identity mismatch.");
        }
        if (data.EncounterResults.Any(r => !world.Groups.Any(g => g.GroupId == r.Binding.GroupId)) ||
            data.WorldEncounter is { Phase: WorldEncounterPhases.Finished } completed &&
            !data.EncounterResults.Any(r => r.Binding == completed.Binding))
            throw new ScenarioException("Encounter result has no matching group or completion.");
    }
}
