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
    internal static readonly Lazy<GameDataRegistry> Registry = new(() => LegacyWeaponRegistry(100, 0));

    // EP-0008 flight/interception regressions retain their calibrated weapon parameters.
    // The shipped EP-0009 catalog is asserted separately by Client content tests.
    internal static GameDataRegistry LegacyWeaponRegistry(decimal accuracy, decimal chanceBase)
    {
        var source = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(ClientRoot, "Settings.json"), out _, out _);
        static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition =>
            Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
        return GameDataRegistry.Create(
            All(source.ModuleCategories),
            All(source.ModuleTypes).Select(module => module.TypeId switch
            {
                "module.torpedo.launcher.basic" => module with
                {
                    TorpedoManeuverability = 5,
                    Accuracy = accuracy,
                    TorpedoRangeKm = 200,
                    TorpedoSpeedKmS = 3
                },
                "module.countermeasure.launcher.basic" => module with
                {
                    CountermeasureAccuracy = 55,
                    CountermeasureManualRangeKm = 200,
                    CountermeasureMaxFlightTimeMs = 60000,
                    CountermeasureSpeedKmS = 12,
                    CountermeasureRangeKm = 100,
                    CountermeasureReloadMs = 10000,
                    Barrels = 1
                },
                _ => module
            }).Select(module => module with { CombatRole = null }),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), catalogVersion: source.CatalogVersion,
            legacyCatalogFingerprint: source.LegacyCatalogFingerprint,
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses),
            stationMarketEvents: All(source.StationMarketEvents),
            combatRules: source.CombatRules! with { Chance = Assert.IsType<CombatChanceRules>(source.CombatRules.Chance) with { Base = chanceBase } },
            fireModes: source.FireModes);
    }
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
                        Modules = o.Modules?.Select(m => m with
                        {
                            AutoDefenseEnabled = false,
                            PowerState = o.ObjectType == SpaceObjectType.NpcShip && m.ModuleTypeId is "module.torpedo.launcher.basic" or "module.engine.basic" ? "Off" : m.PowerState
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
    internal static ScenarioFile WithoutDefense(ScenarioFile scenario) => scenario with
    {
        GameState = scenario.GameState with
        {
            SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with
            {
                Modules = o.Modules?.Select(m => m with { AutoDefenseEnabled = false }).ToArray()
            }).ToArray()
        }
    };

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

    [Fact]
    public void Legacy_fixtures_explicitly_disable_defense()
    {
        var scenario = Scenario();
        foreach (var ship in scenario.GameState.SpaceObjects)
        {
            var defenses = ship.Modules!.Where(m => m.ModuleTypeId == "module.countermeasure.launcher.basic").ToArray();
            Assert.Equal(ship.ObjectType == SpaceObjectType.PlayerShip ? 2 : 1, defenses.Length);
            Assert.All(defenses, defense =>
            {
                Assert.False(defense.AutoDefenseEnabled);
                Assert.Contains(ship.Crew!, c => c.CrewId == defense.OperatorCrewId);
            });
        }
    }

    internal static SimulationEngine Create(ScenarioFile? scenario = null, GameDataRegistry? registry = null)
    {
        var engine = new SimulationEngine(registry ?? Registry.Value);
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
            Assert.Null(paused.Objects.Single(o => o.ObjectId == Target).HullCombat);
            Assert.Equal(projectile.ObjectId,
                Assert.Single(paused.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoIds));
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
        Assert.Single(advanced.CombatJournal, e => e.Type == CombatEventType.ModuleDestroyed);
        Assert.Empty(advanced.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoIds);
        Assert.Single(At(engine, launchTime + 1, launchTime + 1001).CombatImpacts);
    }

    [Fact]
    public void Three_contacts_destroy_modules_with_isolated_rng_draws()
    {
        using var engine = Create();
        var before = At(engine, 0);
        for (int n = 1; n <= 3; n++)
        {
            long launchTime = (n - 1) * 15000;
            engine.ReceiveCommand(Fire("hit-" + n));
            At(engine, launchTime);
            var after = At(engine, launchTime + 4000);
            Assert.Equal(n, after.CombatImpacts.Length);
            Assert.Equal(150, after.CombatImpacts[^1].DamageApplied);
            Assert.Equal(n, after.CombatJournal.Count(e => e.Type == CombatEventType.ModuleDestroyed));
            Assert.Equal((ulong)n, engine.CaptureSaveStateForTests(launchTime + 4000, SimulationSpeed.Speed0).GameState.CombatState!.HitLocationRngCounter);
            Assert.DoesNotContain(after.Objects, o => o.Torpedo is not null);
            Assert.Empty(after.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoIds);
            Assert.Empty(after.ShipEvents);
            Assert.Equal(launchTime + 15000,
                after.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ReadyAtMotionTimeMs);
        }
        Assert.Equal(42UL, engine.MasterSeed);
        // Hit location uses its own stream; the player's launcher state remains unchanged.
    }

    [Fact]
    public void Intermediate_obstacle_is_ignored_and_target_contact_releases_launcher()
    {
        using var engine = Create(Scenario(null, Obstacle("rock", 0, -50)));
        engine.ReceiveCommand(Fire("one"));
        At(engine, 0);
        var hit = At(engine, 4000);
        var fact = Assert.Single(hit.CombatImpacts);
        Assert.Equal(Target, fact.HitObjectId);
        Assert.Equal(Target, fact.TargetObjectId);
        Assert.Equal(150, fact.DamageApplied);
        Assert.Null(hit.Objects.Single(o => o.ObjectId == Target).HullCombat);
        Assert.Equal(3166.6666666666665, fact.MotionTimeMs, 4);
        var last = fact.FinalTrail[^1];
        var end = TorpedoGuidanceMath.PredictSegment(last.Segment, last.Segment.DurationMs);
        Assert.Equal(fact.X, end.X, 7);
        Assert.Equal(fact.Y, end.Y, 7);
        Assert.Equal(fact.MotionTimeMs, fact.FinalTrail.Sum(s => s.Segment.DurationMs), 7);
        engine.ReceiveCommand(Fire("two"));
        Assert.Equal("launcher_reloading", At(engine, 4000).CommandResults.Single(r => r.CommandId == "two").ReasonCode);
        engine.ReceiveCommand(Fire("ready"));
        Assert.Single(At(engine, 15000).Objects.Where(o => o.Torpedo is not null));
    }

    [Fact]
    public void Owner_is_ignored_for_entire_flight()
    {
        using var engine = Create(Scenario(o => o.ObjectId == Player ? o with { SpeedMps = 3000, MovementType = "Linear" } : o));
        engine.ReceiveCommand(Fire("one"));
        At(engine, 0);
        var hit = At(engine, 4000);
        Assert.Equal(Target, Assert.Single(hit.CombatImpacts).HitObjectId);
        Assert.Null(hit.Objects.Single(o => o.ObjectId == Player).HullCombat);
    }

    [Fact]
    public void Equal_time_non_targets_are_ignored_and_target_is_hit_once()
    {
        foreach (bool reverse in new[] { false, true })
        {
            var a = Obstacle("a", 0, -50);
            var z = Obstacle("z", 0, -50);
            using var engine = Create(Scenario(null, reverse ? [a, z] : [z, a]));
            engine.ReceiveCommand(Fire("one"));
            At(engine, 0);
            var hit = At(engine, 5000);
            Assert.Equal(Target, Assert.Single(hit.CombatImpacts).HitObjectId);
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
        Assert.Single(actual.CombatJournal, ev => ev.Type == CombatEventType.ModuleDestroyed);
        Assert.InRange(Math.Abs(e.FinalTrail.Sum(s => s.Segment.DurationMs) - a.FinalTrail.Sum(s => s.Segment.DurationMs)), 0, .0001);
    }

    [Fact]
    public void Moving_non_target_does_not_intercept_torpedo_between_boundary_endpoints()
    {
        // The obstacle crosses the projectile at 1500 ms and leaves before 1600 ms.
        var rock = Obstacle("crossing", -3000, -45) with { SpeedMps = 200000, DirectionDegrees = 90, MovementType = "Linear" };
        using var engine = Create(Scenario(null, rock));
        engine.ReceiveCommand(Fire("one"));
        At(engine, 0);
        var hit = Assert.Single(At(engine, 4000).CombatImpacts);
        Assert.Equal(Target, hit.HitObjectId);
        Assert.InRange(hit.MotionTimeMs, 3166, 3167);
    }

    [Fact]
    public void Non_target_ship_is_ignored_on_way_to_selected_asteroid()
    {
        var scene = Scenario(o => o.ObjectId == Target ? o with { PositionY = -50 } : o,
            Obstacle("selected-rock", 0, -100));
        using var engine = Create(scene);
        engine.ReceiveCommand(Fire("one", "selected-rock"));
        At(engine, 0);
        var hit = Assert.Single(At(engine, 4000).CombatImpacts);
        Assert.Equal("selected-rock", hit.HitObjectId);
        Assert.Equal("selected-rock", hit.TargetObjectId);
        Assert.Null(At(engine, 4000).Objects.Single(o => o.ObjectId == Target).HullCombat);
    }

    [Fact]
    public void Real_pirate_scenario_contact_closes_arc_and_straight_history()
    {
        var scenario = InRangePlayerAttackScenario(ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json")));
        using var engine = Create(WithoutDefense(scenario));
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
