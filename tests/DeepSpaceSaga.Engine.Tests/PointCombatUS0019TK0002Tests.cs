using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0019TK0002Tests
{
    [Fact]
    public void Peaceful_high_speed_clock_consumes_large_real_gap_without_millisecond_walk()
    {
        string root = SeededWorldBootstrapTests.ClientRoot;
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        var source = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios/MarketProfiles/scenario.json"));
        var player = source.GameState.SpaceObjects.Single(o => o.ObjectId == source.GameState.PlayerShipObjectId);
        source = source with
        {
            GameState = source.GameState with
            {
                MasterSeed = 42,
                CurrentSpeed = "Speed0",
                SpaceObjects = [player with { IsDocked = false, DockedStationObjectId = null, Crew = [] }]
            }
        };
        long real = 0;
        using var engine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => real));
        engine.LoadScenario(source); engine.SetSpeed(SimulationSpeed.Speed4);
        real = 20_000_000;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var actual = engine.CaptureSnapshot(advanceClock: true); watch.Stop();
        Assert.Equal(500_000_000L, actual.MotionTimeMs);
        Assert.Equal(150_000_000_000L, actual.GameTimeMs);
        Assert.Equal(SimulationSpeed.Speed4, actual.CurrentSpeed);
        Assert.False(actual.IsTacticalBattleActive);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2), $"Peaceful gap took {watch.Elapsed.TotalMilliseconds:0.0}ms.");
    }
}
