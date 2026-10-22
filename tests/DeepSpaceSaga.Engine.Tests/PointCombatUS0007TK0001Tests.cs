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
    public void Maximum_speed_player_enters_launch_range_within_sixty_seconds_from_shipped_single_mukha_spawn(double playerDirection)
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(TorpedoImpactTests.ClientRoot, "Settings.json"), out _, out _);
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
