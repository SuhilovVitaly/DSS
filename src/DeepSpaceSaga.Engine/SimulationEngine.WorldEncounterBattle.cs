using System.Collections.Immutable;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle.Content;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    internal BattleScenario BuildPirateEncounterScenario(string groupId, TacticalBattleCatalog catalog)
    {
        var group = _demoCampaign.PirateWorld?.Groups.SingleOrDefault(g => g.GroupId == groupId)
            ?? throw new ScenarioException("Unknown pirate encounter group.");
        if (!group.CompositionMaterialized) throw new ScenarioException("Pirate encounter composition is not materialized.");
        var template = catalog.Scenarios["initial"];
        var own = template.Sides.Single(s => s.Id == "A");
        var enemy = template.Sides.Single(s => s.Id == "B");
        var carriers = ImmutableArray.CreateBuilder<BattleCarrierStart>();
        var drones = ImmutableArray.CreateBuilder<BattleDroneStart>();
        for (int memberIndex = 0; memberIndex < group.Members.Length; memberIndex++)
        {
            var member = group.Members[memberIndex];
            var composition = _demoCampaign.PirateCompositions.Single(c => c.GroupId == groupId && c.CarrierId == member.CarrierId);
            double centerY = group.Members.Length == 2 ? 150 + 300 * memberIndex : 250;
            carriers.Add(new(member.CarrierId, "carrier.standard", 850, centerY, 180));
            for (int i = 0; i < composition.Drones.Length; i++)
            {
                var drone = composition.Drones[i];
                int rows = group.Members.Length == 2 ? 6 : 5;
                double y = centerY - (rows - 1) * 20 + i % rows * 40;
                drones.Add(new(drone.DroneId, enemy.Drones[i % enemy.Drones.Length].Pilot, drone.LayoutId,
                    700 + i / rows * 60, y, 180, enemy.Drones[i % enemy.Drones.Length].Speed, member.CarrierId));
            }
        }
        var ownCarrier = own.Carrier with { Id = PlayerShipObjectId! };
        own = own with { Carrier = ownCarrier, Carriers = [ownCarrier],
            Drones = own.Drones.Select(d => d with { OriginCarrierId = ownCarrier.Id }).ToImmutableArray() };
        enemy = enemy with { Carrier = carriers[0], Carriers = carriers.ToImmutable(), Drones = drones.ToImmutable() };
        return TacticalBattleContentLoader.NormalizeScenario(template with { Name = groupId, Sides = [own, enemy], Goal = null });
    }
}
