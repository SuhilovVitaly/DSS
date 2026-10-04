using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
namespace DeepSpaceSaga.Engine.Tests;

public class CountermeasureEndToEndTests
{
    [Theory]
    [InlineData(0UL, CombatEventType.Intercept, 242)]
    [InlineData(42UL, CombatEventType.Miss, 801)]
    public void Real_scenario_success_and_miss(ulong seed, CombatEventType expected, int roll)
    {
        var source = CountermeasureBootstrapTests.RealScenario();
        using var engine = Create(source with { GameState = source.GameState with { MasterSeed = seed } });
        engine.ReceiveCommand(Fire("real-fire")); At(engine, 0);
        AuthoritativeSnapshot snapshot = At(engine, 1);
        for (long time = 1000; time <= 1_000_000 && snapshot.CombatJournal.All(e => e.Roll is null); time += 1000)
            snapshot = At(engine, time);
        var result = Assert.Single(snapshot.CombatJournal, e => e.Roll is not null);
        Assert.Equal(expected, result.Type);
        Assert.Equal(roll, result.Roll);
        Assert.Equal(500, result.ChanceTenths);
        Assert.Contains(snapshot.CombatJournal, e => e.Type == CombatEventType.Launch && e.Result == "countermeasure");
    }
    [Fact]
    public void Three_actual_hits_destroy_defending_pirate_and_save_load_replays()
    {
        var a = Run(false); var b = Run(true);
        Assert.Equal(a.Journal.Select(e => (e.Type, e.ActorObjectId, e.TargetObjectId, e.ProjectileObjectId, e.Roll, e.ChanceTenths, e.Damage)),
            b.Journal.Select(e => (e.Type, e.ActorObjectId, e.TargetObjectId, e.ProjectileObjectId, e.Roll, e.ChanceTenths, e.Damage)));
        Assert.Equal(a.RngState, b.RngState); Assert.Equal(a.RngCounter, b.RngCounter);
        Assert.Equal(3, a.Journal.Count(e => e.Type == CombatEventType.Hit && e.Damage == 150));
        Assert.Single(a.Journal, e => e.Type == CombatEventType.Destroyed);
        Assert.Equal(3UL, a.RngCounter); // Seed42: first three real rolls801,820,781 all miss at50%.
    }
    private static CountermeasureStateData Run(bool reload)
    {
        var source = CountermeasureBootstrapTests.RealScenario();
        var engine = Create(source with { GameState = source.GameState with { MasterSeed = 42 } });
        try
        {
            long time = 0;
            for (int shot = 0; shot < 3; shot++)
            {
                engine.ReceiveCommand(Fire("shot-" + shot));
                var snapshot = At(engine, time);
                bool restored = false;
                while (snapshot.Objects.Any(o => o.Torpedo is not null) && time < 2_000_000)
                {
                    time += 1000;
                    snapshot = engine.CaptureSnapshotForTests(time * 10, SimulationSpeed.Speed4, time);
                    if (reload && !restored && snapshot.Objects.Any(o => o.Countermeasure is not null))
                    {
                        var saved = engine.CaptureSaveStateForTests(time * 10, SimulationSpeed.Speed0, time);
                        var loaded = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true);
                        engine.Dispose(); engine = new SimulationEngine(Registry.Value); engine.LoadScenario(loaded, true);
                        restored = true;
                    }
                }
                Assert.True(time < 2_000_000);
                if (reload) Assert.True(restored);
            }
            var final = At(engine, time, time * 10);
            Assert.DoesNotContain(final.Objects, o => o.ObjectId == Target);
            Assert.Single(final.Objects, o => o.RenderObjectType == SpaceObjectType.Wreck);
            var save = engine.CaptureSaveStateForTests(time * 10, SimulationSpeed.Speed0, time);
            ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true);
            return save.GameState.DefenseState!;
        }
        finally { engine.Dispose(); }
    }
}
