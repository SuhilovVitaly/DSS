using System.Text.Json;
using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0007TK0003Tests
{
    private const string NpcEngine = "MOD-PIRATE-ENGINE-01";
    private static AuthoritativeSnapshot At(SimulationEngine engine, long time = 0) => engine.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);
    private static SimulationEngine Battle(int skill = 20, ulong seed = 1, string enginePower = "On", string profile = "slow")
    {
        var engine = Create(o => o.ObjectId != Target ? o : o with
        {
            PositionX = 200,
            PositionY = 0,
            DirectionDegrees = 270,
            Ai = new(25, 8, 35),
            Crew = o.Crew!.Append(o.Crew![0] with { CrewId = "npc-pilot", ManeuverabilitySkill = skill }).ToArray(),
            Modules = o.Modules!.Select(m => m.ModuleId == NpcEngine ? m with
            { PowerState = enginePower, OperatorCrewId = "npc-pilot" } : m with { PowerState = "Off" }).ToArray()
        }, scatter: 0, seed: seed);
        engine.ReceiveCommand(Launch(profile: profile));
        At(engine);
        return engine;
    }

    [Theory]
    [InlineData(0, 1400)]
    [InlineData(20, 800)]
    [InlineData(30, 500)]
    public void Npc_reaction_is_skill_based_and_rolls_once_per_new_threat(int skill, long due)
    {
        using var engine = Battle(skill);
        var episode = Assert.Single(engine.CaptureNpcDodgeThreats());
        Assert.Equal(0, episode.DetectedAt);
        Assert.Equal(due, episode.DueAt);
        At(engine, due - 1);
        Assert.Equal(0UL, engine.CaptureNpcDodgeRngState().Counter);
        At(engine, due);
        episode = Assert.Single(engine.CaptureNpcDodgeThreats());
        Assert.True(episode.Evaluated);
        Assert.True(episode.RollSucceeded);
        Assert.True(episode.Evaded);
        Assert.Equal(1UL, engine.CaptureNpcDodgeRngState().Counter);
        At(engine, due + 100);
        At(engine, due + 100);
        Assert.Equal(1UL, engine.CaptureNpcDodgeRngState().Counter);
        Assert.Equal(0, engine.CaptureEvasionEpisodeCount());
    }

    [Theory]
    [InlineData(1UL, 16114971481387581067UL, 9068942227001227936UL, true)]
    [InlineData(2UL, 8921703351816078528UL, 1875674097429725397UL, false)]
    public void Npc_dodge_uses_independent_named_stream_known_vectors(ulong seed, ulong initial, ulong next, bool success)
    {
        using var engine = Battle(seed: seed);
        Assert.Equal((initial, 0UL), engine.CaptureNpcDodgeRngState());
        var scatter = engine.CaptureLaunchScatterState();
        At(engine, 800);
        Assert.Equal((next, 1UL), engine.CaptureNpcDodgeRngState());
        var episode = Assert.Single(engine.CaptureNpcDodgeThreats());
        Assert.Equal(success, episode.RollSucceeded);
        Assert.Equal(success, episode.Evaded);
        At(engine, 1500);
        Assert.Equal((next, 1UL), engine.CaptureNpcDodgeRngState());
        Assert.Equal(scatter, engine.CaptureLaunchScatterState());
    }

    [Fact]
    public void Npc_evade_uses_shared_executor_without_exposing_return_control()
    {
        using var engine = Battle();
        var snapshot = At(engine, 800);
        var npc = snapshot.Objects.Single(o => o.ObjectId == Target);
        var episode = Assert.Single(engine.CaptureNpcDodgeThreats());
        Assert.True(episode.Evaded);
        Assert.Equal(6, npc.SpeedKmS);
        Assert.True(npc.Y > 0); // The NPC has already moved south of the eastbound trace.
        Assert.Equal(180, npc.Direction, 8); // Continue away from the trace.
        Assert.Null(npc.Evasion);
        Assert.Null(npc.ActiveEngineCommandType);
        Assert.Null(npc.HostileCombat);
        Assert.DoesNotContain("BaseMotion", JsonSerializer.Serialize(npc));
        var moved = At(engine, 900).Objects.Single(o => o.ObjectId == Target);
        Assert.Equal(npc.X, moved.X, 8);
        Assert.Equal(npc.Y + 6, moved.Y, 8);
        Assert.Empty(snapshot.CommandResults);
    }

    [Fact]
    public void Pause_stops_reaction_deadline_and_resume_does_not_reroll()
    {
        using var engine = Battle();
        At(engine, 799);
        engine.SetSpeed(SimulationSpeed.Speed0);
        for (int i = 0; i < 4; i++) engine.CaptureSnapshotForTests(10000 + i * 1000, simulationTimeMs: 799);
        Assert.Equal(0UL, engine.CaptureNpcDodgeRngState().Counter);
        Assert.False(Assert.Single(engine.CaptureNpcDodgeThreats()).Evaluated);
        engine.SetSpeed(SimulationSpeed.Speed1);
        At(engine, 800);
        Assert.True(Assert.Single(engine.CaptureNpcDodgeThreats()).Evaded);
        Assert.Equal(1UL, engine.CaptureNpcDodgeRngState().Counter);
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    public void Reaction_and_outcome_are_independent_of_snapshot_partitioning(ulong seed)
    {
        using var whole = Battle(seed: seed); using var split = Battle(seed: seed);
        foreach (long t in new long[] { 17, 100, 313, 799, 800, 801, 900, 999 }) At(split, t);
        var a = At(whole, 1000); var b = At(split, 1000);
        Assert.Equal(JsonSerializer.Serialize(a.Objects), JsonSerializer.Serialize(b.Objects));
        Assert.Equal(whole.CaptureNpcDodgeRngState(), split.CaptureNpcDodgeRngState());
        Assert.Equal(whole.CaptureNpcDodgeThreats(), split.CaptureNpcDodgeThreats());
        Assert.Equal(a.CombatJournal.ToArray(), b.CombatJournal.ToArray());
    }

    [Fact]
    public void Stale_threat_resolves_without_rng_or_late_maneuver()
    {
        using var engine = Battle(skill: 0, profile: "fast");
        string projectile = Assert.Single(engine.CaptureNpcDodgeThreats()).ProjectileId;
        At(engine, 800);
        engine.ReceiveCommand(new("detonate", 2, Player, Launcher, CombatCommandTypes.Detonate, TargetObjectId: projectile));
        var detonation = At(engine, 800);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(detonation.CommandResults).Status);
        At(engine, 1400);
        var episode = Assert.Single(engine.CaptureNpcDodgeThreats());
        Assert.True(episode.Evaluated);
        Assert.Null(episode.RollSucceeded);
        Assert.False(episode.Evaded);
        Assert.Equal(0UL, engine.CaptureNpcDodgeRngState().Counter);
    }

    [Fact]
    public void Disabled_engine_does_not_draw_or_retry_the_episode()
    {
        using var engine = Battle(enginePower: "Off");
        At(engine, 800); At(engine, 1000);
        var episode = Assert.Single(engine.CaptureNpcDodgeThreats());
        Assert.True(episode.Evaluated);
        Assert.Null(episode.RollSucceeded);
        Assert.Equal(0UL, engine.CaptureNpcDodgeRngState().Counter);
    }

    [Fact]
    public void Session_reset_clears_seen_threats_and_rng_cursor()
    {
        using var engine = Battle(); At(engine, 800);
        var scenario = TorpedoImpactTests.Scenario(o => o);
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { MasterSeed = 1 } });
        Assert.Empty(engine.CaptureNpcDodgeThreats());
        Assert.Equal((16114971481387581067UL, 0UL), engine.CaptureNpcDodgeRngState());
    }
}
