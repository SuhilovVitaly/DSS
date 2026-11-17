using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;

namespace DeepSpaceSaga.Engine.Tests.Demo;

[Collection("Demo process debug")]
public sealed class Ep0035Us13Tk3Tests
{
    [Fact]
    public void ActualSavedSquadAssemblesThreeCarriersAndTwelveIndependentDronesWithoutReroll()
    {
        int original = SimulationEngine.ConfigurePirateSquadChance().PirateSquadChancePercent;
        try
        {
            SimulationEngine.ConfigurePirateSquadChance(100);
            using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
            engine.LoadScenario(DemoEp0035Us0010Tk0002Tests.ContactScenario(engine), true);
            var save = engine.CaptureSaveState();
            var group = save.GameState.DemoCampaign!.PirateWorld!.Groups[0];
            var catalog = TacticalBattleContentLoader.Load(DemoEp0033Us0001Tk0003Tests.Root() + "/Data/TacticalBattle");
            var scenario = engine.BuildPirateEncounterScenario(group.GroupId, catalog);
            Assert.Equal(2, scenario.Sides[1].Carriers.Length);
            Assert.Equal(12, scenario.Sides[1].Drones.Length);
            foreach (var composition in save.GameState.DemoCampaign.PirateCompositions.Where(c => c.GroupId == group.GroupId))
                foreach (var drone in composition.Drones)
                {
                    var deployed = scenario.Sides[1].Drones.Single(d => d.Id == drone.DroneId);
                    Assert.Equal(drone.LayoutId, deployed.Layout);
                    Assert.Equal(composition.CarrierId, deployed.OriginCarrierId);
                }
            var ship = save.GameState.SpaceObjects.Single(s => s.ObjectId == save.GameState.PlayerShipObjectId);
            var crew = ship.Crew!.Select(c => new CrewMemberRuntime(c.CrewId, c.DisplayName, c.AccuracySkill, c.ManeuverabilitySkill,
                c.Gender, c.PortraitAssetId, c.Role, c.Initiative, c.SensorRangeBonusPercent)).ToImmutableArray();
            var wing = ship.CarrierState!.OwnedDrones!.ToImmutableArray();
            var battle = OwnedBattleAssembly.Create(catalog, ship.ObjectId, 350, 35, wing, crew, scenario, group.GroupId, 123);
            Assert.Equal(3, battle.Carriers.Length);
            Assert.Equal(12, battle.Drones.Count(d => d.Side == "B"));
            Assert.All(battle.Drones.Where(d => d.Side == "A"), d => Assert.Equal(ship.ObjectId, d.OriginCarrierId));
            Assert.All(battle.Drones.Where(d => d.Side == "B").GroupBy(d => d.OriginCarrierId), g => Assert.Equal(6, g.Count()));
            var snapshot = TacticalBattleSnapshotBuilder.Units(battle);
            Assert.Equal(snapshot.Length, snapshot.Select(u => u.Id).Distinct().Count());
            Assert.All(snapshot.Where(u => u.Kind == DeepSpaceSaga.Contracts.TacticalUnitKind.Drone), u => Assert.NotNull(u.OriginCarrierId));
            var repeated = OwnedBattleAssembly.Create(catalog, ship.ObjectId, 350, 35, wing, crew, scenario, group.GroupId, 123);
            Assert.Equal(JsonSerializer.Serialize(snapshot), JsonSerializer.Serialize(TacticalBattleSnapshotBuilder.Units(repeated)));
            foreach (var drone in battle.Drones.Where(d => d.Side == "A"))
                Assert.Equal(wing.Single(w => w.DroneId == drone.Id).AmmoCurrent, drone.AmmoCurrent);
        }
        finally { SimulationEngine.ConfigurePirateSquadChance(original); }
    }
}
