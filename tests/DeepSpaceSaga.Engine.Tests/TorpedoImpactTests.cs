using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class TorpedoImpactTests
{
    internal const string Player = "SPC-0001", Target = "SPC-0002", Launcher = "MOD-PLAYER-TORPEDO-01";
    internal static readonly string ClientRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    internal static readonly Lazy<GameDataRegistry> Registry = new(() => EngineContentLoader.LoadRegistryFromSettingsFile(
        Path.Combine(ClientRoot, "Settings.json"), out _, out _));
    internal static ScenarioFile Scenario(Func<SpaceObjectData, SpaceObjectData>? configure = null,
        params SpaceObjectData[] extras)
    {
        var source = ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json"));
        return source with
        {
            GameState = source.GameState with
            {
                MasterSeed = 42,
                SpaceObjects = source.GameState.SpaceObjects.Select(o =>
                {
                    var configured = o with
                    {
                        PositionX = 0,
                        PositionY = o.ObjectId == Player ? 0 : -100,
                        SpeedMps = 0,
                        DirectionDegrees = 0,
                        MovementType = "Stationary"
                    };
                    return configure?.Invoke(configured) ?? configured;
                }).Concat(extras).ToArray()
            }
        };
    }
    internal static SimulationEngine Create(ScenarioFile? scenario = null)
    {
        var engine = new SimulationEngine(Registry.Value);
        engine.LoadScenario(scenario ?? Scenario());
        return engine;
    }
    internal static PlayerCommand Fire(string id, string target = Target) => new(id, 1, Player, Launcher, CombatCommandTypes.Fire, target);
    internal static AuthoritativeSnapshot At(SimulationEngine engine, long motion, long? calendar = null) =>
        engine.CaptureSnapshotForTests(calendar ?? motion, simulationTimeMs: motion);
    internal static SpaceObjectData Obstacle(string id, double x, double y) =>
        new(id, SpaceObjectType.Asteroid, "Permanent", id, x, y, 0, 0, "Stationary", 1_000_000, "Silicate", null, IsKnown: true);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 5)]
    [InlineData(1234, 3)]
    public void Initial_overlap_waits_for_physical_advance_through_repeated_paused_snapshots(long launchTime, double distance)
    {
        using var engine = Create(Scenario(o => o.ObjectId == Target ? o with { PositionY = -distance } : o));
        At(engine, launchTime);
        engine.ReceiveCommand(Fire("overlap"));
        var launched = At(engine, launchTime);
        Assert.Equal(SimulationSpeed.Speed0, launched.CurrentSpeed);
        var projectile = Assert.Single(launched.Objects.Where(o => o.Torpedo is not null));

        for (int i = 0; i < 4; i++)
        {
            var paused = At(engine, launchTime);
            Assert.Equal(launchTime, paused.MotionTimeMs);
            Assert.Equal(JsonSerializer.Serialize(projectile),
                JsonSerializer.Serialize(Assert.Single(paused.Objects.Where(o => o.Torpedo is not null))));
            Assert.Empty(paused.CombatImpacts);
            Assert.Equal(450, paused.Objects.Single(o => o.ObjectId == Target).HullCombat!.CurrentHp);
            Assert.Equal(projectile.ObjectId,
                paused.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoObjectId);
        }

        // Calendar advancement alone must not trigger physical contact either.
        Assert.Empty(At(engine, launchTime, launchTime + 1000).CombatImpacts);
        var advanced = At(engine, launchTime + 1, launchTime + 1001);
        var impact = Assert.Single(advanced.CombatImpacts);
        Assert.Equal((double)launchTime, impact.MotionTimeMs);
        Assert.Equal(Target, impact.HitObjectId);
        Assert.Equal(150, impact.DamageApplied);
        Assert.Empty(impact.FinalTrail);
        Assert.DoesNotContain(advanced.Objects, o => o.Torpedo is not null);
        Assert.Equal(300, advanced.Objects.Single(o => o.ObjectId == Target).HullCombat!.CurrentHp);
        Assert.Null(advanced.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoObjectId);
        Assert.Single(At(engine, launchTime + 1, launchTime + 1001).CombatImpacts);
    }

    [Fact]
    public void Three_contacts_apply_150_without_rng_draws()
    {
        using var engine = Create();
        var before = At(engine, 0);
        for (int n = 1; n <= 3; n++)
        {
            engine.ReceiveCommand(Fire("hit-" + n));
            At(engine, (n - 1) * 4000);
            var after = At(engine, n * 4000);
            Assert.Equal(n, after.CombatImpacts.Length);
            Assert.Equal(150, after.CombatImpacts[^1].DamageApplied);
            Assert.Equal(450 - n * 150, after.Objects.FirstOrDefault(o => o.ObjectId == Target)?.HullCombat?.CurrentHp ?? 0);
            Assert.DoesNotContain(after.Objects, o => o.Torpedo is not null);
            Assert.Null(after.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoObjectId);
            Assert.Empty(after.ShipEvents);
            Assert.Equal(JsonSerializer.Serialize(before.InstalledModules), JsonSerializer.Serialize(after.InstalledModules));
        }
        Assert.Equal(42UL, engine.MasterSeed);
        // Launch/impact takes no module cycle (the RNG-bearing command executor).
        // All module state and event journals above remain unchanged across all hits.
    }

    [Fact]
    public void Invulnerable_obstacle_explodes_torpedo_and_releases_launcher()
    {
        using var engine = Create(Scenario(null, Obstacle("rock", 0, -50)));
        engine.ReceiveCommand(Fire("one"));
        At(engine, 0);
        var hit = At(engine, 4000);
        var fact = Assert.Single(hit.CombatImpacts);
        Assert.Equal("rock", fact.HitObjectId);
        Assert.Equal(Target, fact.TargetObjectId);
        Assert.Equal(0, fact.DamageApplied);
        Assert.Equal(450, hit.Objects.Single(o => o.ObjectId == Target).HullCombat!.CurrentHp);
        Assert.Equal(1500, fact.MotionTimeMs, 4);
        var last = fact.FinalTrail[^1];
        var end = TorpedoGuidanceMath.PredictSegment(last.Segment, last.Segment.DurationMs);
        Assert.Equal(fact.X, end.X, 7);
        Assert.Equal(fact.Y, end.Y, 7);
        Assert.Equal(fact.MotionTimeMs, fact.FinalTrail.Sum(s => s.Segment.DurationMs), 7);
        engine.ReceiveCommand(Fire("two"));
        Assert.Single(At(engine, 4000).Objects.Where(o => o.Torpedo is not null));
    }

    [Fact]
    public void Owner_is_ignored_for_entire_flight()
    {
        using var engine = Create(Scenario(o => o.ObjectId == Player ? o with { SpeedMps = 3000, MovementType = "Linear" } : o));
        engine.ReceiveCommand(Fire("one"));
        At(engine, 0);
        var hit = At(engine, 4000);
        Assert.Equal(Target, Assert.Single(hit.CombatImpacts).HitObjectId);
        Assert.Equal(450, hit.Objects.Single(o => o.ObjectId == Player).HullCombat!.CurrentHp);
    }

    [Fact]
    public void Equal_time_contacts_use_stable_order_and_apply_once()
    {
        foreach (bool reverse in new[] { false, true })
        {
            var a = Obstacle("a", 0, -50);
            var z = Obstacle("z", 0, -50);
            using var engine = Create(Scenario(null, reverse ? [a, z] : [z, a]));
            engine.ReceiveCommand(Fire("one"));
            At(engine, 0);
            var hit = At(engine, 5000);
            Assert.Equal("a", Assert.Single(hit.CombatImpacts).HitObjectId);
            Assert.Single(At(engine, 6000).CombatImpacts);
            engine.ReceiveCommand(Fire("one"));
            Assert.DoesNotContain(At(engine, 6000).Objects, o => o.Torpedo is not null);
        }
    }

    [Fact]
    public void Snapshot_coalescing_does_not_lose_impact_fact()
    {
        using var engine = Create();
        engine.ReceiveCommand(Fire("one"));
        At(engine, 0);
        var first = Assert.Single(At(engine, 5000).CombatImpacts);
        var late = Assert.Single(At(engine, 50000).CombatImpacts);
        Assert.Equal(first, late);
        Assert.NotEmpty(late.FinalTrail);
        engine.LoadScenario(Scenario());
        Assert.Empty(At(engine, 0).CombatImpacts);
        engine.ReceiveCommand(Fire("fresh-session"));
        At(engine, 0);
        Assert.Equal(1, Assert.Single(At(engine, 5000).CombatImpacts).EventId);
    }

    [Fact]
    public void Calendar_boundaries_do_not_change_combat_result()
    {
        using var whole = Create();
        using var split = Create();
        foreach (var engine in new[] { whole, split }) { engine.ReceiveCommand(Fire("one")); At(engine, 0); }
        var expected = At(whole, 5000, 2 * GameCalendar.DayMs);
        foreach (long time in new long[] { 7, 123, 1111, 3166, 3167, 5000 })
            At(split, time, time * (2 * GameCalendar.DayMs) / 5000);
        var actual = At(split, 5000, 2 * GameCalendar.DayMs);
        var a = Assert.Single(actual.CombatImpacts);
        var e = Assert.Single(expected.CombatImpacts);
        Assert.Equal(e.HitObjectId, a.HitObjectId);
        Assert.InRange(Math.Abs(e.MotionTimeMs - a.MotionTimeMs), 0, .0001);
        Assert.InRange(Math.Abs(e.X - a.X) + Math.Abs(e.Y - a.Y), 0, 3e-7);
        Assert.Equal(300, actual.Objects.Single(o => o.ObjectId == Target).HullCombat!.CurrentHp);
        Assert.InRange(Math.Abs(e.FinalTrail.Sum(s => s.Segment.DurationMs) - a.FinalTrail.Sum(s => s.Segment.DurationMs)), 0, .0001);
    }

    [Fact]
    public void Moving_obstacle_is_hit_between_boundary_endpoints()
    {
        // The obstacle crosses the projectile at 1500 ms and leaves before 1600 ms.
        var rock = Obstacle("crossing", -3000, -45) with { SpeedMps = 200000, DirectionDegrees = 90, MovementType = "Linear" };
        using var engine = Create(Scenario(null, rock));
        engine.ReceiveCommand(Fire("one"));
        At(engine, 0);
        var hit = Assert.Single(At(engine, 4000).CombatImpacts);
        Assert.Equal("crossing", hit.HitObjectId);
        Assert.InRange(hit.MotionTimeMs, 1497, 1500);
    }

    [Fact]
    public void Real_pirate_scenario_contact_closes_arc_and_straight_history()
    {
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json"));
        using var engine = Create(scenario);
        engine.ReceiveCommand(Fire("real-flight"));
        var launched = At(engine, 0);
        var torpedo = Assert.Single(launched.Objects.Where(o => o.Torpedo is not null)).Torpedo!;
        Assert.True(torpedo.Route.HasIntercept);
        long end = torpedo.PredictedImpactMotionTimeMs!.Value + 1000;
        var snapshot = At(engine, end);
        var hit = Assert.Single(snapshot.CombatImpacts);
        Assert.Equal(Target, hit.HitObjectId);
        Assert.Equal(150, hit.DamageApplied);
        Assert.Contains(hit.FinalTrail, s => s.Segment.AngularVelocityDegPerSec != 0);
        Assert.Contains(hit.FinalTrail, s => s.Segment.AngularVelocityDegPerSec == 0);
        var target = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == Target);
        double x = target.PositionX + target.SpeedMps / 100000 * hit.MotionTimeMs * Math.Sin(target.DirectionDegrees * Math.PI / 180);
        double y = target.PositionY - target.SpeedMps / 100000 * hit.MotionTimeMs * Math.Cos(target.DirectionDegrees * Math.PI / 180);
        Assert.InRange(Math.Abs(Math.Sqrt(Math.Pow(x - hit.X, 2) + Math.Pow(y - hit.Y, 2)) - 5), 0, 3e-7);
    }

    [Fact]
    public void Tetrarch_sprite_without_class_is_invulnerable_and_unknown_hull_stays_masked()
    {
        using var legacy = Create(Scenario(o => o.ObjectId == Target ? o with { ShipClassId = null } : o));
        legacy.ReceiveCommand(Fire("legacy"));
        At(legacy, 0);
        Assert.Equal(0, Assert.Single(At(legacy, 4000).CombatImpacts).DamageApplied);
        using var unknown = Create(Scenario(o => o.ObjectId == Target ? o with { IsKnown = false } : o));
        unknown.ReceiveCommand(Fire("unknown"));
        At(unknown, 0);
        var snapshot = At(unknown, 4000);
        Assert.Equal(150, Assert.Single(snapshot.CombatImpacts).DamageApplied);
        Assert.Null(snapshot.Objects.Single(o => o.ObjectId == Target).HullCombat);
    }
}
