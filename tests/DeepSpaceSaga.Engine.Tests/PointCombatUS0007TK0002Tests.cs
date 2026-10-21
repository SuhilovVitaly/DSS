using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0007TK0002Tests
{
    private const string NpcLauncher = "MOD-PIRATE-TORPEDO-01";
    private static AuthoritativeSnapshot At(SimulationEngine engine, long time = 0) => engine.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);
    private static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);

    private static SimulationEngine Battle(double distance = 60, double scatter = .02, double threshold = .3,
        double assumedDodge = .25, ImmutableArray<LaunchProfileSnapshot>? profiles = null,
        double playerSpeed = 0, double playerDirection = 0, bool friendlyAtTarget = false, bool ai = true, bool futureFriend = false)
    {
        var source = Registry(scatter);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m =>
            m.CategoryTypeId == "module.torpedo.launcher" && profiles.HasValue ? m with { Profiles = profiles.Value } : m),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), stationMarketProfiles: All(source.StationMarketProfiles),
            shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules);
        var original = TorpedoImpactTests.Scenario(o => o.ObjectId == Player ? o with
        { PositionX = 0, PositionY = 0, SpeedMps = playerSpeed * 1000, DirectionDegrees = playerDirection, MovementType = "Linear", Ai = null } : o with
        {
            PositionX = 0,
            PositionY = -distance * 10,
            Ai = ai ? new(25, 8, 35, threshold, assumedDodge) : null,
            Crew = o.Crew!.Select(c => c with { AccuracySkill = 15 }).ToArray(),
            Modules = o.Modules!.Select(m => m.ModuleId == NpcLauncher ? m with { PowerState = "On" } : m).ToArray()
        });
        if (friendlyAtTarget)
        {
            var npc = original.GameState.SpaceObjects.Single(o => o.ObjectId == Target);
            var friend = npc with
            {
                ObjectId = "friend",
                PositionX = futureFriend ? -150 : 0,
                PositionY = 0,
                Ai = null,
                SpeedMps = futureFriend ? 5000 : 0,
                DirectionDegrees = 90,
                MovementType = "Linear",
                Crew = npc.Crew!.Select(c => c with { CrewId = "friend-" + c.CrewId }).ToArray(),
                Modules = npc.Modules!.Select(m => m with
                { ModuleId = "friend-" + m.ModuleId, OperatorCrewId = m.OperatorCrewId is null ? null : "friend-" + m.OperatorCrewId, PowerState = "Off" }).ToArray()
            };
            original = original with { GameState = original.GameState with { SpaceObjects = original.GameState.SpaceObjects.Append(friend).ToArray() } };
        }
        var engine = new SimulationEngine(registry);
        try { engine.LoadScenario(original); return engine; }
        catch { engine.Dispose(); throw; }
    }

    [Fact]
    public void Npc_selects_max_coverage_profile_and_uses_accuracy_scatter_without_rng()
    {
        using var engine = Battle();
        var before = engine.CaptureLaunchScatterState();
        var aim = Assert.IsType<SimulationEngine.NpcPointAim>(engine.CaptureNpcPointAim(Target, NpcLauncher, 0));
        Assert.Equal("medium", aim.ProfileId);
        Assert.Equal(6, aim.FlightSeconds, 10);
        Assert.Equal(2.45, aim.ExpectedMissKm, 10);
        Assert.Equal((5 - 2.45) / 3.5, aim.Coverage, 10);
        Assert.Equal(0, aim.X, 10); Assert.Equal(0, aim.Y, 10);
        Assert.Equal(aim, engine.CaptureNpcPointAim(Target, NpcLauncher, 0));
        Assert.Equal(before, engine.CaptureLaunchScatterState());
    }

    [Fact]
    public void Npc_ties_preserve_catalog_profile_order()
    {
        using var engine = Battle(distance: 30, scatter: 0, assumedDodge: 0,
            profiles: [new("z", 10, 5), new("a", 10, 5)]);
        Assert.Equal("z", engine.CaptureNpcPointAim(Target, NpcLauncher, 0)!.ProfileId);
    }

    [Theory]
    [InlineData(17)]
    [InlineData(25)]
    [InlineData(33)]
    public void Npc_equal_core_coverage_prefers_slow_profile(double distance)
    {
        using var engine = Battle(distance: distance, scatter: 0, assumedDodge: 0);
        var aim = engine.CaptureNpcPointAim(Target, NpcLauncher, 0)!;
        Assert.Equal(1, aim.Coverage);
        Assert.Equal("slow", aim.ProfileId);
    }

    [Theory]
    [InlineData(.3, true)]
    [InlineData(.300001, false)]
    public void Npc_obeys_exact_coverage_threshold(double threshold, bool accepted)
    {
        using var engine = Battle(distance: 10, scatter: .079, assumedDodge: 0, threshold: threshold,
            profiles: [new("one", 10, 1)]);
        var aim = engine.CaptureNpcPointAim(Target, NpcLauncher, 0);
        Assert.Equal(accepted, aim is not null);
        if (aim is not null) Assert.Equal(.3, aim.Coverage, 10);
        Assert.Equal(accepted, At(engine, 1).Objects.Any(o => o.ObservedProjectile is not null));
    }

    [Fact]
    public void Npc_rejects_friendly_blast_and_self_blast_without_launch_rng()
    {
        using var engine = Battle(friendlyAtTarget: true);
        var rng = engine.CaptureLaunchScatterState();
        Assert.Null(engine.CaptureNpcPointAim(Target, NpcLauncher, 0));
        Assert.DoesNotContain(At(engine, 1000).Objects, o => o.ObservedProjectile is not null);
        Assert.Equal(rng, engine.CaptureLaunchScatterState());
        using var close = Battle(distance: 1, scatter: 0, assumedDodge: 0);
        Assert.Null(close.CaptureNpcPointAim(Target, NpcLauncher, 0));
    }

    [Fact]
    public void Npc_linear_lead_intersects_the_moving_player()
    {
        using var engine = Battle(distance: 30, scatter: 0, assumedDodge: 0, playerSpeed: 5, playerDirection: 90,
            profiles: [new("one", 10, 5)]);
        var aim = Assert.IsType<SimulationEngine.NpcPointAim>(engine.CaptureNpcPointAim(Target, NpcLauncher, 0));
        double expected = 30 / Math.Sqrt(100 - 25);
        Assert.Equal(expected, aim.FlightSeconds, 10);
        Assert.Equal(expected * 50, aim.X, 9);
        Assert.Equal(0, aim.Y, 9);
        Assert.Equal(aim.FlightSeconds * 10, Math.Sqrt(aim.X * aim.X + 300 * 300) / 10, 9);
    }

    [Fact]
    public void Friendly_safety_uses_future_position_at_detonation()
    {
        // A friendly ship 15 km west reaches the intended blast centre after the 3 second flight.
        using var engine = Battle(distance: 30, scatter: 0, assumedDodge: 0, friendlyAtTarget: true,
            profiles: [new("one", 10, 5)], futureFriend: true);
        Assert.Null(engine.CaptureNpcPointAim(Target, NpcLauncher, 0));
        Assert.DoesNotContain(At(engine, 1).Objects, o => o.ObservedProjectile is not null);
    }

    [Fact]
    public void Auto_launch_and_rng_are_independent_of_snapshot_partitioning()
    {
        using var whole = Battle(); using var split = Battle();
        At(whole); At(split);
        foreach (long t in new long[] { 1, 17, 100, 313, 750, 999 }) At(split, t);
        var a = At(whole, 1000); var b = At(split, 1000);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(a.Objects), System.Text.Json.JsonSerializer.Serialize(b.Objects));
        Assert.Equal(whole.CaptureLaunchScatterState(), split.CaptureLaunchScatterState());
        Assert.Equal(a.CombatJournal.ToArray(), b.CombatJournal.ToArray());
    }

    [Fact]
    public void Unreachable_out_of_range_or_non_ai_npc_does_not_fire()
    {
        using var unreachable = Battle(distance: 30, playerSpeed: 5, playerDirection: 180,
            profiles: [new("slow", 4, 9)]);
        Assert.Null(unreachable.CaptureNpcPointAim(Target, NpcLauncher, 0));
        using var distant = Battle(distance: 69, playerSpeed: 5, playerDirection: 180);
        Assert.Null(distant.CaptureNpcPointAim(Target, NpcLauncher, 0));
        using var noAi = Battle(ai: false);
        Assert.Null(noAi.CaptureNpcPointAim(Target, NpcLauncher, 0));
        Assert.DoesNotContain(At(noAi, 1000).Objects, o => o.ObservedProjectile is not null);
    }

    [Fact]
    public void Numerically_unrepresentable_flight_is_not_scheduled()
    {
        using var engine = Battle(scatter: 0, assumedDodge: 0, profiles: [new("tiny", 1e-30, 9)]);
        var rng = engine.CaptureLaunchScatterState();
        Assert.Null(engine.CaptureNpcPointAim(Target, NpcLauncher, 0));
        Assert.DoesNotContain(At(engine, 1000).Objects, o => o.ObservedProjectile is not null);
        Assert.Equal(rng, engine.CaptureLaunchScatterState());
    }

    [Fact]
    public void Npc_auto_launch_uses_common_executor_and_no_player_receipt_or_legacy_reaction()
    {
        using var engine = Battle(scatter: 0);
        Assert.DoesNotContain(At(engine).Objects, o => o.ObservedProjectile is not null);
        var before = engine.CaptureLaunchScatterState();
        var snapshot = At(engine, 1);
        var shot = Assert.Single(snapshot.Objects, o => o.ObservedProjectile is not null);
        Assert.Equal("medium", shot.ObservedProjectile!.ProfileId); // medium and fast both cover the core here
        Assert.Null(shot.OwnProjectile);
        Assert.Null(shot.Torpedo);
        Assert.Empty(snapshot.CommandResults);
        Assert.Equal(before.Counter + 2, engine.CaptureLaunchScatterState().Counter);
        Assert.Single(At(engine, 9999).CombatJournal, e => e.Type == CombatEventType.Launch);
    }

    [Theory]
    [InlineData(-.01, .25)]
    [InlineData(1.01, .25)]
    [InlineData(.3, -.01)]
    [InlineData(.3, 1.01)]
    [InlineData(double.NaN, .25)]
    public void Invalid_scenario_ai_threshold_or_dodge_is_rejected(double threshold, double dodge)
    {
        Assert.Throws<ScenarioException>(() => Battle(threshold: threshold, assumedDodge: dodge));
    }
}
