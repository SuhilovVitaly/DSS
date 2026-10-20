using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0006TK0002Tests
{
    private const string Engine = "MOD-PLAYER-ENGINE-01", NpcEngine = "MOD-PIRATE-ENGINE-01", NpcLauncher = "MOD-PIRATE-TORPEDO-01";
    private static AuthoritativeSnapshot At(SimulationEngine engine, long time = 0) => engine.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);
    private static ObjectMotionSnapshot Ship(AuthoritativeSnapshot snapshot) => snapshot.Objects.Single(o => o.ObjectId == Player);
    private static PlayerCommand Evade(string threat, EvadeSide? side = EvadeSide.Left, string id = "evade") =>
        new(id, 2, Player, Engine, ManeuverCommandTypes.Evade, ThreatProjectileId: threat, EvadeSide: side);

    private static (SimulationEngine Engine, string Threat) Battle(string engineState = "ready", double endpointX = -600)
    {
        var engine = Create(o => o.ObjectId == Player ? o with
        {
            SpeedMps = 2000,
            DirectionDegrees = 0,
            MovementType = "Linear",
            Modules = o.Modules!.Select(m => m.ModuleId != Engine ? m : m with
            {
                PowerState = engineState == "off" ? "Off" : m.PowerState,
                DisabledUntilMotionTimeMs = engineState == "disabled" ? 1000 : m.DisabledUntilMotionTimeMs
            }).ToArray()
        } : o with
        {
            PositionX = 100,
            PositionY = 0,
            Modules = o.Modules!.Select(m => m.ModuleId == NpcLauncher ? m with { PowerState = "On" } : m).ToArray()
        }, scatter: 0);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, NpcLauncher, endpointX, 0, "slow", 0).Disposition);
        string id = Assert.Single(At(engine).Objects, o => o.ObservedProjectile is not null).ObjectId;
        return (engine, id);
    }

    [Theory]
    [InlineData(EvadeSide.Left, 180, 110)]
    [InlineData(EvadeSide.Right, 0, -130)]
    public void Evade_uses_trace_perpendicular_and_returns_to_base(EvadeSide side, double heading, double endY)
    {
        var (engine, threat) = Battle();
        using (engine)
        {
            var rng = engine.CaptureLaunchScatterState();
            engine.ReceiveCommand(Evade(threat, side));
            var start = At(engine);
            var ship = Ship(start);
            Assert.Equal(CommandResultStatus.Executed, Assert.Single(start.CommandResults).Status);
            Assert.Equal(heading, ship.Direction, 9);
            Assert.Equal(6, ship.SpeedKmS);
            Assert.Equal(2000, ship.Evasion!.EndsAtMotionTimeMs, 8);
            Assert.Equal(2, ship.Evasion.BaseMotion.SpeedKmS);
            Assert.Equal(0, ship.Evasion.BaseMotion.Direction);
            Assert.Equal(0, ship.Evasion.SampleMotionTimeMs);
            Assert.Equal(JsonSerializer.Serialize(ship), JsonSerializer.Serialize(Ship(At(engine))));
            var forecast = new DeepSpaceSaga.Motion.LinearMotionPredictor().Predict(ship, 2500);
            var actual = Ship(At(engine, 2500));
            Assert.Null(actual.Evasion);
            Assert.Equal(2, actual.SpeedKmS);
            Assert.Equal(0, actual.Direction);
            Assert.Equal(0, actual.X, 8);
            Assert.Equal(endY, actual.Y, 8);
            Assert.Equal(actual.X, forecast.X, 8);
            Assert.Equal(actual.Y, forecast.Y, 8);
            Assert.Equal(rng, engine.CaptureLaunchScatterState());
            Assert.Equal(1, engine.CaptureEvasionEpisodeCount());
        }
    }

    [Theory]
    [InlineData("off", CommandReasonCodes.EngineDisabled)]
    [InlineData("disabled", CommandReasonCodes.EngineDisabled)]
    [InlineData("missing", CommandReasonCodes.UnknownThreat)]
    [InlineData("own", CommandReasonCodes.UnknownThreat)]
    [InlineData("side", CommandReasonCodes.InvalidEvadeSide)]
    public void Evade_rejects_missing_foreign_threat_or_disabled_engine(string kind, string expected)
    {
        var (engine, threat) = Battle(kind);
        using (engine)
        {
            if (kind == "own") { engine.ReceiveCommand(Launch()); threat = Flight(At(engine)).ObjectId; }
            var before = Ship(At(engine));
            var random = engine.CaptureLaunchScatterState();
            engine.ReceiveCommand(Evade(kind == "missing" ? "absent" : threat, kind == "side" ? (EvadeSide)999 : EvadeSide.Left));
            var after = At(engine);
            Assert.Equal(expected, Assert.Single(after.CommandResults).ReasonCode);
            Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(Ship(after)));
            Assert.Equal(0, engine.CaptureEvasionEpisodeCount());
            Assert.Equal(random, engine.CaptureLaunchScatterState());
        }
    }

    [Fact]
    public void Evade_preserves_base_course_and_counts_once()
    {
        var (engine, threat) = Battle();
        using (engine)
        {
            var command = Evade(threat);
            engine.ReceiveCommand(command);
            var first = Ship(At(engine)).Evasion!;
            At(engine, 500);
            engine.ReceiveCommand(command);
            Assert.Equal(first.EndsAtMotionTimeMs, Ship(At(engine, 500)).Evasion!.EndsAtMotionTimeMs);
            engine.ReceiveCommand(Evade(threat, EvadeSide.Right, "change-side"));
            var changed = Ship(At(engine, 500));
            Assert.Equal(first.BaseMotion, changed.Evasion!.BaseMotion);
            Assert.Equal(1, engine.CaptureEvasionEpisodeCount());
            var ended = Ship(At(engine, 2000));
            Assert.Null(ended.Evasion);
            Assert.Equal(2, ended.SpeedKmS);
            Assert.Equal(0, ended.Direction);
            engine.ReceiveCommand(Evade(threat, id: "new-episode")); At(engine, 2200);
            Assert.Equal(2, engine.CaptureEvasionEpisodeCount());
        }
    }

    [Fact]
    public void Evasion_schedule_ignores_concealed_projectile_endpoint_and_deadline()
    {
        var (first, threat1) = Battle(endpointX: 0);
        var (second, threat2) = Battle(endpointX: -600);
        using (first) using (second)
        {
            first.ReceiveCommand(Evade(threat1)); second.ReceiveCommand(Evade(threat2));
            var a = Ship(At(first)); var b = Ship(At(second));
            Assert.Equal(a.Evasion, b.Evasion);
            Assert.Equal(a.Direction, b.Direction);
            Assert.Equal(a.SpeedKmS, b.SpeedKmS);
        }
    }

    [Fact]
    public void Rejected_engine_input_preserves_evasion_but_valid_input_takes_control()
    {
        var (engine, threat) = Battle();
        using (engine)
        {
            engine.ReceiveCommand(Evade(threat)); At(engine);
            var before = Ship(At(engine, 500));
            engine.ReceiveCommand(new("invalid-orbit", 3, Player, Engine, ShipEngineCommandTypes.Orbit));
            var rejected = At(engine, 500);
            Assert.Equal(CommandReasonCodes.InvalidTargetCoordinates, Assert.Single(rejected.CommandResults).ReasonCode);
            Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(Ship(rejected)));
            engine.ReceiveCommand(new("take-control", 4, Player, Engine, ShipEngineCommandTypes.TurnLeftStep));
            Assert.Null(Ship(At(engine, 500)).Evasion);
            var later = Ship(At(engine, 2500));
            Assert.Equal(6, later.SpeedKmS);
            Assert.Equal(179, later.Direction, 8);
        }
    }

    [Fact]
    public void Npc_uses_same_maneuver_without_exposing_its_return_control()
    {
        using var engine = Create(o => o.ObjectId != Target ? o : o with
        {
            PositionX = 100,
            PositionY = 0,
            Modules = o.Modules!.Select(m => m.ModuleId == NpcEngine ? m with { PowerState = "On" } : m).ToArray()
        }, scatter: 0);
        engine.ReceiveCommand(Launch());
        string id = Flight(At(engine)).ObjectId;
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartNpcEvade(Target, NpcEngine, id, EvadeSide.Left, 0).Disposition);
        var snapshot = At(engine);
        var npc = snapshot.Objects.Single(o => o.ObjectId == Target);
        Assert.Null(npc.Evasion);
        Assert.Null(npc.ActiveEngineCommandType);
        Assert.Equal(6, npc.SpeedKmS);
        Assert.DoesNotContain("BaseMotion", JsonSerializer.Serialize(npc));
        Assert.DoesNotContain("EndsAtMotionTimeMs", JsonSerializer.Serialize(npc));
        Assert.Empty(snapshot.CommandResults);
        Assert.Equal(0, engine.CaptureEvasionEpisodeCount());
        var moving = At(engine, 500).Objects.Single(o => o.ObjectId == Target);
        Assert.Equal(6, moving.SpeedKmS);
        Assert.Equal(-30, moving.Y, 8);
        Assert.Null(moving.Evasion);
    }

    [Fact]
    public void Evade_resumes_the_captured_orbit_command_and_matches_shared_prediction()
    {
        var (engine, threat) = Battle();
        using (engine)
        {
            engine.ReceiveCommand(new("orbit", 2, Player, Engine, ShipEngineCommandTypes.Orbit, TargetWorldX: 1000, TargetWorldY: 1000));
            var baseline = Ship(At(engine));
            Assert.Equal(ShipEngineCommandTypes.Orbit, baseline.ActiveEngineCommandType);
            engine.ReceiveCommand(Evade(threat));
            var start = Ship(At(engine));
            Assert.Equal(ShipEngineCommandTypes.Orbit, start.Evasion!.BaseMotion.ActiveEngineCommandType);
            var expected = new DeepSpaceSaga.Motion.LinearMotionPredictor().Predict(start, 3000);
            var actual = Ship(At(engine, 3000));
            Assert.Null(actual.Evasion);
            Assert.Equal(ShipEngineCommandTypes.Orbit, actual.ActiveEngineCommandType);
            Assert.Equal(1000, actual.NavigationTargetX);
            Assert.Equal(1000, actual.NavigationTargetY);
            Assert.Equal(expected.X, actual.X, 7);
            Assert.Equal(expected.Y, actual.Y, 7);
            Assert.Equal(expected.Direction, actual.Direction, 7);
        }
    }

    [Fact]
    public void Evasion_and_orbit_return_are_independent_of_snapshot_partitions()
    {
        var (whole, firstThreat) = Battle(); var (split, secondThreat) = Battle();
        using (whole) using (split)
        {
            foreach (var pair in new[] { (Engine: whole, Threat: firstThreat), (Engine: split, Threat: secondThreat) })
            {
                pair.Engine.ReceiveCommand(new("orbit", 2, Player, Engine, ShipEngineCommandTypes.Orbit, TargetWorldX: 1000, TargetWorldY: 1000));
                At(pair.Engine);
                pair.Engine.ReceiveCommand(Evade(pair.Threat)); At(pair.Engine);
            }
            var expected = Ship(At(whole, 3500));
            foreach (long t in new long[] { 17, 250, 1001, 1999, 2000, 2107, 2400, 3000, 3499 }) At(split, t);
            Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(Ship(At(split, 3500))));
            Assert.Equal(whole.CaptureEvasionEpisodeCount(), split.CaptureEvasionEpisodeCount());
        }
    }

    [Fact]
    public void Engine_damage_interrupts_the_return_and_keeps_inertial_motion()
    {
        var source = Registry(scatter: 0);
        static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m => m with
        {
            MassKg = m.CategoryTypeId == "module.engine" ? 1_000_000 : 1,
            TorpedoDamage = m.TorpedoDamage is null ? null : 1,
            Profiles = m.CategoryTypeId == "module.torpedo.launcher" ? [new("slow", 6, 99)] : m.Profiles
        }), All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), stationMarketProfiles: All(source.StationMarketProfiles),
            shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules);
        var scenario = TorpedoImpactTests.Scenario(o => o.ObjectId == Player ? o with
        { Ai = null, SpeedMps = 2000, DirectionDegrees = 0, MovementType = "Linear" } : o with
        {
            Ai = null,
            PositionX = 100,
            PositionY = 0,
            Modules = o.Modules!.Select(m => m.ModuleId == NpcLauncher ? m with { PowerState = "On" } : m).ToArray()
        });
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, NpcLauncher, 0, 0, "slow", 0).Disposition);
        string threat = Assert.Single(At(engine).Objects, o => o.ObservedProjectile is not null).ObjectId;
        engine.ReceiveCommand(Evade(threat)); At(engine);
        var snapshot = At(engine, 2500);
        var hit = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.ModuleHit && e.TargetObjectId == Player);
        Assert.Equal(Engine, hit.HitModuleId);
        var ship = Ship(snapshot);
        Assert.Null(ship.Evasion);
        Assert.Equal(180, ship.Direction, 8);
        Assert.Equal(6, ship.SpeedKmS);
        Assert.Equal(150, ship.Y, 7);
        Assert.True(snapshot.InstalledModules.Single(m => m.ModuleId == Engine).DisabledUntilMotionTimeMs > 2500);
    }
}
