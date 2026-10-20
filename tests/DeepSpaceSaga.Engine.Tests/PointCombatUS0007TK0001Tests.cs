using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0007TK0001Tests
{
    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    public void Maximum_speed_player_enters_launch_range_within_sixty_seconds_from_combat_start_range(double playerDirection)
    {
        var source = PointCombatUS0003TK0002Tests.Registry(scatter: 0);
        static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m =>
            m.CategoryTypeId == "module.engine" ? m with
            { MaxSpeedMps = 8000, LinearInertiaMps2 = 1000, AngularInertiaDegPerSec = 24 } : m),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), stationMarketProfiles: All(source.StationMarketProfiles),
            shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules);
        var original = ScenarioLoader.LoadFromFile(Path.Combine(TorpedoImpactTests.ClientRoot, "Scenarios", "CombatMukhaSingle", "scenario.json"));
        string playerId = original.GameState.PlayerShipObjectId!;
        var scenario = original with
        {
            GameState = original.GameState with
            {
                SpaceObjects = original.GameState.SpaceObjects.Select(o => o.ObjectId == playerId
                    ? o with { SpeedMps = 5000, DirectionDegrees = playerDirection, MovementType = "Linear" }
                    : o with
                    {
                        // Existing scenario starts at 300 km. This bounded controller check starts at A01 combat admission (100 km).
                        PositionX = o.PositionX / 3,
                        PositionY = o.PositionY / 3,
                        Ai = new(25, 8, 35),
                        Modules = o.Modules!.Select(m => m with
                        { PowerState = m.ModuleTypeId.Contains("torpedo", StringComparison.Ordinal) ? "Off" : m.PowerState }).ToArray()
                    }).ToArray()
            }
        };
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario);
        bool reached = false;
        for (long time = 100; time <= 60000; time += 100)
        {
            var snapshot = engine.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);
            var player = snapshot.Objects.Single(o => o.ObjectId == playerId);
            var enemy = snapshot.Objects.Single(o => o.ObjectId != playerId && o.EnemyStatus is not null);
            double dx = (enemy.X - player.X) / 10, dy = (enemy.Y - player.Y) / 10;
            if (dx * dx + dy * dy <= 70 * 70) { reached = true; break; }
        }
        Assert.True(reached, "Controller at combat admission failed to enter the 70 km point-launch range within 60 physical seconds.");
    }
}
