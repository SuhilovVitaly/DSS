using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0019TK0002Tests
{
    [Theory]
    [InlineData("CombatMukha")]
    [InlineData("CombatMukhaSingle")]
    [InlineData("PlayerShipOnly")]
    public void Scenario_engine_fixtures_do_not_reference_removed_shmel_file_and_preserve_translated_combat(string name)
    {
        string root = SeededWorldBootstrapTests.ClientRoot;
        var files = Directory.GetFiles(Path.Combine(root, "Scenarios"), "scenario.json", SearchOption.AllDirectories);
        Assert.Equal(8, files.Length);
        Assert.DoesNotContain(files, path => Path.GetFileName(Path.GetDirectoryName(path)) == "CombatShmel");
        var source = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios", name, "scenario.json"));
        source = source with { GameState = source.GameState with { MasterSeed = 3001 } };
        var player = source.GameState.SpaceObjects.Single(o => o.ObjectId == source.GameState.PlayerShipObjectId);
        double dx = 720000 - player.PositionX, dy = 123000 - player.PositionY;
        var translated = ScenarioGroupPlacement.Translate(source, 720000, 123000);
        Assert.Equal(source.GameState.SpaceObjects.Count, translated.GameState.SpaceObjects.Count);
        Assert.All(source.GameState.SpaceObjects, original =>
        {
            var moved = translated.GameState.SpaceObjects.Single(o => o.ObjectId == original.ObjectId);
            Assert.Equal(original.PositionX + dx, moved.PositionX); Assert.Equal(original.PositionY + dy, moved.PositionY);
            Assert.Equal(original, moved with { PositionX = original.PositionX, PositionY = original.PositionY });
        });
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        using var baseline = new SimulationEngine(registry); baseline.LoadScenario(source);
        using var shifted = new SimulationEngine(registry); shifted.LoadScenario(translated);
        foreach (long time in new long[] { 0, 100, 1000, 5000, 10000, 30000, 60000 })
        {
            var a = baseline.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);
            var b = shifted.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);
            Assert.Equal(a.EffectiveOutcome.State, b.EffectiveOutcome.State);
            Assert.Equal(a.Encounter!.State, b.Encounter!.State); Assert.Equal(a.Encounter.Stats, b.Encounter.Stats);
            Assert.Equal(a.Objects.Select(o => o.ObjectId), b.Objects.Select(o => o.ObjectId));
            foreach (var original in a.Objects)
            {
                var moved = b.Objects.Single(o => o.ObjectId == original.ObjectId);
                Assert.InRange(Math.Abs(original.X + dx - moved.X), 0, 1e-6);
                Assert.InRange(Math.Abs(original.Y + dy - moved.Y), 0, 1e-6);
                Assert.Equal(original.Direction, moved.Direction, 6); Assert.Equal(original.SpeedKmS, moved.SpeedKmS, 8);
            }
            Assert.Equal(baseline.CaptureLaunchScatterState(), shifted.CaptureLaunchScatterState());
            Assert.Equal(baseline.CaptureNpcDodgeRngState(), shifted.CaptureNpcDodgeRngState());
            Assert.DoesNotContain(a.CombatJournal, e => e.ChanceTenths is not null || e.Roll is not null || e.Type.ToString() == "TorpedoHitRoll");
        }
    }
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
        Assert.Equal(CombatEncounterState.Idle, actual.Encounter!.State);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2), $"Peaceful gap took {watch.Elapsed.TotalMilliseconds:0.0}ms.");
    }
}
