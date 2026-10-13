using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class NpcTorpedoLifetimeTests
{
    private static GameDataRegistry ProductionRegistry() => EngineContentLoader.LoadRegistryFromSettingsFile(
        Path.Combine(ClientRoot, "Settings.json"), out _, out _);

    [Fact]
    public void Reachable_hostile_launches_again_after_fifteen_seconds()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario(), ProductionRegistry());
        var first = At(engine, 1);
        Assert.Equal(120001, Assert.Single(first.Objects, o => o.Torpedo is not null)
            .Torpedo!.ExpiresAtMotionTimeMs);
        var later = At(engine, 15001);
        var launches = later.CombatJournal.Where(e => e.Type == CombatEventType.Launch && e.Result == "torpedo" &&
            e.ActorObjectId == Target).Select(e => e.MotionTimeMs).ToArray();
        Assert.Equal(new double[] { 1, 15001 }, launches);
    }

    [Fact]
    public void Hostile_does_not_fire_at_unreachable_intercept_even_inside_range()
    {
        var scenario = HostileTorpedoFireTests.HostileScenario();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId == Player ? o with
                { SpeedMps = 20000, DirectionDegrees = 180, MovementType = "Linear" } : o).ToArray()
            }
        };
        using var engine = Create(scenario, ProductionRegistry());
        var snapshot = At(engine, 1);
        Assert.DoesNotContain(snapshot.Objects, o => o.Torpedo is not null);
        Assert.DoesNotContain(snapshot.CombatJournal, e => e.Type == CombatEventType.Launch && e.ActorObjectId == Target);
    }

    [Fact]
    public void Torpedo_expires_at_captured_120_seconds_and_resume_matches()
    {
        var scenario = TorpedoImpactTests.Scenario(o => o.ObjectId == Target ? o with
        { RelationToPlayer = PlayerRelation.Neutral, SpeedMps = 20000, DirectionDegrees = 0, MovementType = "Linear" } : o);
        var registry = ProductionRegistry();
        using var engine = Create(scenario, registry);
        engine.ReceiveCommand(Fire("lifetime"));
        var launched = At(engine, 0);
        Assert.Equal(120000, Assert.Single(launched.Objects, o => o.Torpedo is not null).Torpedo!.ExpiresAtMotionTimeMs);
        var before = At(engine, 119999);
        Assert.Single(before.Objects, o => o.Torpedo is not null);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(
            engine.CaptureSaveStateForTests(119999, SimulationSpeed.Speed0, 119999)), true);
        using var restored = new SimulationEngine(registry);
        restored.LoadScenario(save, isSave: true);
        var expired = At(engine, 120000);
        var resumed = At(restored, 120000);
        Assert.DoesNotContain(expired.Objects, o => o.Torpedo is not null);
        Assert.Single(expired.CombatImpacts, impact => impact.TerminationKind == TorpedoTerminationKind.Expired);
        Assert.Equal(expired.CombatJournal.Select(e => (e.Type, e.MotionTimeMs)),
            resumed.CombatJournal.Select(e => (e.Type, e.MotionTimeMs)));
    }
}
