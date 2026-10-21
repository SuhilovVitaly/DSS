using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class HostileAttackAiTests
{
    private static ScenarioFile Configured(double flank = 35, double distanceUnits = 10000)
    {
        var source = HostileOrbitRuntimeTests.Scene();
        return source with
        {
            GameState = source.GameState with
            {
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o.ObjectId == Target
                    ? o with { PositionY = -distanceUnits, Ai = new AttackAiData(50, 10, flank) }
                    : o).ToArray()
            }
        };
    }

    [Fact]
    public void Configured_standoff_and_flank_reach_live_controller()
    {
        using var engine = Create(Configured());
        var hostile = At(engine, 1).Objects.Single(o => o.ObjectId == Target);
        Assert.Equal(50, hostile.HostileCombat!.DesiredRadiusKm);
        Assert.Equal(HostileCombatPhase.FlankApproach, hostile.HostileCombat.Phase);
    }

    [Fact]
    public void Configured_ship_enters_50km_orbit_band()
    {
        using var engine = Create(Configured(distanceUnits: 1400));
        var hostile = At(engine, 500000).Objects.Single(o => o.ObjectId == Target);
        Assert.Equal(50, hostile.HostileCombat!.DesiredRadiusKm);
        Assert.InRange(Math.Sqrt(hostile.X * hostile.X + hostile.Y * hostile.Y) / 10, 40, 60);
        Assert.Equal(HostileCombatPhase.OrbitHold, hostile.HostileCombat.Phase);
    }

    [Fact]
    public void Opposite_flank_offsets_change_attack_steering()
    {
        using var left = Create(Configured(-60));
        using var right = Create(Configured(60));
        var a = At(left, 1).Objects.Single(o => o.ObjectId == Target).HostileCombat!;
        var b = At(right, 1).Objects.Single(o => o.ObjectId == Target).HostileCombat!;
        Assert.NotEqual(a.Segment.TurnRateDegPerSec, b.Segment.TurnRateDegPerSec);
    }

    [Fact]
    public void Missing_ai_block_uses_half_current_launcher_range_and_pursuit()
    {
        using var engine = Create(HostileOrbitRuntimeTests.Scene());
        var state = At(engine, 1).Objects.Single(o => o.ObjectId == Target).HostileCombat!;
        Assert.Equal(35, state.DesiredRadiusKm);
        Assert.Equal(HostileCombatPhase.Pursuit, state.Phase);
    }

    [Fact]
    public void Configured_ai_is_partition_invariant_and_survives_save_load()
    {
        var scene = Configured();
        using var whole = Create(scene);
        using var split = Create(scene);
        foreach (long time in new long[] { 1, 31, 100, 101, 155, 713 }) At(split, time);
        Assert.Equal(At(whole, 1013).Objects.Single(o => o.ObjectId == Target),
            At(split, 1013).Objects.Single(o => o.ObjectId == Target));

        var saved = whole.CaptureSaveStateForTests(1013, SimulationSpeed.Speed0);
        var json = ScenarioLoader.Serialize(saved);
        Assert.Contains("\"standoffKm\": 50", json);
        using var resumed = new SimulationEngine(Registry.Value);
        resumed.LoadScenario(ScenarioLoader.LoadFromJson(json, true), isSave: true);
        Assert.Equal(new AttackAiData(50, 10, 35),
            resumed.CaptureSaveStateForTests(1013, SimulationSpeed.Speed0).GameState.SpaceObjects.Single(o => o.ObjectId == Target).Ai);
        Assert.Equal(At(whole, 1113).Objects.Single(o => o.ObjectId == Target),
            At(resumed, 1113).Objects.Single(o => o.ObjectId == Target));
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(50, 50, 0)]
    [InlineData(50, -1, 0)]
    [InlineData(50, 10, 181)]
    public void Invalid_attack_parameters_reject_before_load(double standoff, double band, double flank)
    {
        var source = Configured();
        var bad = source with
        {
            GameState = source.GameState with
            {
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o.ObjectId == Target
                    ? o with { Ai = new AttackAiData(standoff, band, flank) } : o).ToArray()
            }
        };
        using var engine = Create();
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad));
    }
}
