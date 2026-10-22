using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using Point = DeepSpaceSaga.Engine.Tests.PointCounterFixture;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class TorpedoImpactTests
{
    internal const string Player = "SPC-0001", Target = "SPC-0002", Launcher = "MOD-PLAYER-TORPEDO-01";
    internal static readonly string ClientRoot = Point.Root;
    internal static readonly Lazy<GameDataRegistry> Registry = new(() => Point.Registry());
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
                        Ai = null,
                        Modules = o.Modules?.Select(m => m with
                        {
                            // Explicit6km/s,4deg/s Basic engine calibration for generic motion regressions.
                            ModuleTypeId = m.ModuleTypeId.StartsWith("module.engine.", StringComparison.Ordinal) ? "module.engine.basic" : m.ModuleTypeId,
                            AutoDefenseEnabled = false,
                            PowerState = o.ObjectType == SpaceObjectType.NpcShip && (m.ModuleTypeId.StartsWith("module.engine.", StringComparison.Ordinal) || m.ModuleTypeId == "module.torpedo.launcher.basic") ? "Off" : m.PowerState
                        }).ToArray(),
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
    internal static SpaceObjectData FragileForOneHit(SpaceObjectData ship) => ship with
    {
        Modules = ship.Modules!.Select(module => module.ModuleTypeId is "module.container.basic" or "module.scanner.mk1"
            ? module with { StructurePoints = 0, Destroyed = true } : module).ToArray()
    };

    internal static ScenarioFile InRangePlayerAttackScenario(ScenarioFile scenario)
    {
        var player = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == Player);
        return scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId == Target ? o with
                {
                    PositionX = player.PositionX + (o.PositionX - player.PositionX) * 0.1,
                    PositionY = player.PositionY + (o.PositionY - player.PositionY) * 0.1,
                    Modules = o.Modules!.Select(m => m.ModuleTypeId is "module.torpedo.launcher.basic" or "module.engine.basic" ? m with { PowerState = "Off" } : m).ToArray()
                } : o).ToArray()
            }
        };
    }

    internal static SimulationEngine Create(ScenarioFile? scenario = null, GameDataRegistry? registry = null)
    {
        var engine = new SimulationEngine(registry ?? Registry.Value);
        engine.LoadScenario(scenario ?? Scenario());
        return engine;
    }
    internal static AuthoritativeSnapshot At(SimulationEngine engine, long motion, long? calendar = null) =>
        engine.CaptureSnapshotForTests(calendar ?? motion, simulationTimeMs: motion);
    internal static SpaceObjectData Obstacle(string id, double x, double y) =>
        new(id, SpaceObjectType.Asteroid, "Permanent", id, x, y, 0, 0, "Stationary", 1_000_000, "Silicate", null, IsKnown: true);

    internal static PlayerCommand LaunchAt(string id, double x = 0, double y = -100, string profile = "medium") =>
        new(id, 1, Player, Launcher, CombatCommandTypes.Launch, TargetWorldX: x, TargetWorldY: y, ProfileId: profile);

    [Fact]
    public void Controlled_source_fixture_uses_real_point_catalog_and_explicit_basic_engine_calibration()
    {
        using var engine = Create(); var snapshot = At(engine, 0);
        Assert.Equal(2, snapshot.InstalledModules.Count(m => m.Defense is not null));
        Assert.Equal(new[] { "slow", "medium", "fast" }, snapshot.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.Profiles!.Value.Select(p => p.ProfileId));
        Assert.All(snapshot.InstalledModules.Where(m => m.Defense is not null), m => Assert.False(m.Defense!.AutoEnabled));
        Assert.Equal("module.engine.basic", snapshot.InstalledModules.Single(m => m.ModuleId == "MOD-PLAYER-ENGINE-01").ModuleTypeId);
    }

    [Fact]
    public void Chosen_point_flight_has_no_early_hull_contact_and_resolves_exactly_at_fuse()
    {
        using var engine = Create(); engine.ReceiveCommand(LaunchAt("point")); At(engine, 0);
        var middle = At(engine, 500); var flight = Assert.Single(middle.Objects, o => o.OwnProjectile is not null);
        Assert.Equal(0, flight.X, 10); Assert.Equal(-50, flight.Y, 10); Assert.Equal(1000, flight.OwnProjectile!.DetonationMotionTimeMs);
        Assert.DoesNotContain(middle.CombatJournal, e => e.Type == CombatEventType.Hit);
        var end = At(engine, 1000); Assert.DoesNotContain(end.Objects, o => o.OwnProjectile is not null);
        Assert.Equal(1000, Assert.Single(end.CombatJournal, e => e.Type == CombatEventType.TorpedoDetonated).MotionTimeMs);
        Assert.Single(end.CombatJournal, e => e.Type == CombatEventType.Hit && e.TargetObjectId == Target);
        Assert.Equal(10000, end.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ReadyAtMotionTimeMs);
        var saved = engine.CaptureSaveStateForTests(1000, SimulationSpeed.Speed0, 1000);
        Assert.Equal(1ul, saved.GameState.CombatState!.HitLocationRngCounter); Assert.Equal(2ul, saved.GameState.PointCombatState!.LaunchScatter.Counter);
        Assert.All(end.CombatJournal, e => { Assert.Null(e.Roll); Assert.Null(e.ChanceTenths); });
    }

    [Fact]
    public void Intermediate_asteroid_does_not_replace_the_scheduled_area_detonation()
    {
        using var engine = Create(Scenario(null, Obstacle("rock", 0, -50)));
        engine.ReceiveCommand(LaunchAt("point")); At(engine, 0);
        Assert.Single(At(engine, 500).Objects, o => o.OwnProjectile is not null);
        var final = At(engine, 1000); Assert.Contains(final.Objects, o => o.ObjectId == "rock");
        Assert.Single(final.CombatJournal, e => e.Type == CombatEventType.Hit && e.TargetObjectId == Target);
        Assert.DoesNotContain(final.CombatJournal, e => e.Type == CombatEventType.Hit && e.TargetObjectId == "rock");
    }

    [Fact]
    public void Sparse_and_partitioned_calendar_updates_keep_the_same_physical_point_facts()
    {
        using var whole = Create(); using var split = Create();
        foreach (var engine in new[] { whole, split }) { engine.ReceiveCommand(LaunchAt("point")); At(engine, 0); }
        var expected = At(whole, 5000, 2 * GameCalendar.DayMs);
        foreach (long time in new long[] { 7, 123, 999, 1000, 1111, 5000 }) At(split, time, time * (2 * GameCalendar.DayMs) / 5000);
        var actual = At(split, 5000, 2 * GameCalendar.DayMs);
        Assert.Equal(JsonSerializer.Serialize(expected.Objects), JsonSerializer.Serialize(actual.Objects));
        Assert.Equal(JsonSerializer.Serialize(expected.CombatJournal), JsonSerializer.Serialize(actual.CombatJournal));
        Assert.Equal(JsonSerializer.Serialize(whole.CaptureSaveStateForTests(2 * GameCalendar.DayMs, SimulationSpeed.Speed0, 5000)),
            JsonSerializer.Serialize(split.CaptureSaveStateForTests(2 * GameCalendar.DayMs, SimulationSpeed.Speed0, 5000)));
    }

    [Fact]
    public void Coalesced_point_facts_remain_durable_without_replay_or_new_session_duplicates()
    {
        using var engine = Create(); engine.ReceiveCommand(LaunchAt("one")); At(engine, 0);
        var first = At(engine, 1000).CombatJournal; var late = At(engine, 50000).CombatJournal;
        Assert.Equal(first.ToArray(), late.Take(first.Length).ToArray());
        Assert.Equal(first.Length + 1, late.Length);
        Assert.Equal(CombatEventType.TorpedoReloadCompleted, late[^1].Type);
        engine.ReceiveCommand(LaunchAt("one", 300, 0));
        Assert.Equal(late.ToArray(), At(engine, 50000).CombatJournal.ToArray());
        Assert.Equal(2ul, engine.CaptureLaunchScatterState().Counter);
        engine.LoadScenario(Scenario()); Assert.Empty(At(engine, 0).CombatJournal);
        engine.ReceiveCommand(LaunchAt("new-session")); Assert.Equal(1, Assert.Single(At(engine, 0).CombatJournal).EventId);
    }

    [Fact]
    public void Captured_start_and_endpoint_do_not_accumulate_guidance_history()
    {
        using var engine = Create(); engine.ReceiveCommand(LaunchAt("line")); At(engine, 0);
        foreach (long t in Enumerable.Range(1, 50).Select(i => i * 17L))
        {
            var f = Assert.Single(At(engine, t).Objects, o => o.OwnProjectile is not null);
            Assert.Equal(0, f.OwnProjectile!.StartX); Assert.Equal(0, f.OwnProjectile.StartY);
            Assert.Equal(0, f.OwnProjectile.DetonationX); Assert.Equal(-100, f.OwnProjectile.DetonationY);
            Assert.Equal(-t / 10d, f.Y, 10); Assert.Null(f.Torpedo); Assert.Null(f.Countermeasure);
        }
    }

    [Fact]
    public void Ships_without_legacy_class_take_point_damage_without_publishing_private_health()
    {
        using var engine = Create(Scenario(o => o.ObjectId != Target ? o : o with { ShipClassId = null }));
        engine.ReceiveCommand(LaunchAt("point")); At(engine, 0); var hit = At(engine, 1000);
        Assert.Single(hit.CombatJournal, e => e.Type == CombatEventType.Hit && e.TargetObjectId == Target);
        Assert.All(hit.Objects.Where(o => o.ObjectId == Target), o => { Assert.Null(o.HullCombat); Assert.True(o.ModuleHealth.IsDefaultOrEmpty); });
        Assert.Equal(1ul, engine.CaptureSaveStateForTests(1000, SimulationSpeed.Speed0, 1000).GameState.CombatState!.HitLocationRngCounter);
    }
}
