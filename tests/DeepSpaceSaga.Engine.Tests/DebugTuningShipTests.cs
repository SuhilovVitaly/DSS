using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class DebugTuningShipTests
{
    internal static SimulationEngine Create(double speed = 4500, bool hostile = false, bool enemyWeapon = false)
    {
        var source = ScenarioLoader.LoadFromFile(Path.Combine(TorpedoImpactTests.ClientRoot,
            "Scenarios", hostile ? "CombatMukhaSingle" : "PlayerShipOnly", "scenario.json"));
        var engine = new SimulationEngine(PointCombatUS0018TK0002Tests.Registry(), clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(source with
        {
            GameState = source.GameState with
            {
                CurrentSpeed = "Speed0",
                MasterSeed = 42,
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o with
                {
                    SpeedMps = o.ObjectId == source.GameState.PlayerShipObjectId ? speed : o.SpeedMps,
                    Modules = o.Modules!.Select(m => m with
                    {
                        PowerState = hostile && m.ModuleTypeId.Contains("engine", StringComparison.Ordinal) ||
                        enemyWeapon && m.ModuleTypeId.Contains("torpedo", StringComparison.Ordinal) || o.ObjectId == source.GameState.PlayerShipObjectId ? "On" : "Off"
                    }).ToArray()
                }).ToArray()
            }
        });
        return engine;
    }

    internal static void Apply(SimulationEngine engine, string field, double value, string type = "module.engine.tetrarch", string id = "tune") =>
        engine.ReceiveCommand(new(id, 1, "", "", DebugTuningCommandTypes.Apply, DebugTuning: new([new(type, field, value)])));
    private static ObjectMotionSnapshot Player(AuthoritativeSnapshot state) => state.Objects.Single(o => o.ObjectId == state.PlayerShipObjectId);

    [Fact]
    public void Lower_maximum_keeps_position_clamps_speed_and_moves_two_km_in_one_second()
    {
        using var engine = Create();
        var before = Player(PointCombatUS0018TK0002Tests.At(engine, 1000));
        Apply(engine, "maxSpeedKmS", 2);
        var state = PointCombatUS0018TK0002Tests.At(engine, 1000);
        var after = Player(state);
        Assert.Equal(before.X, after.X, 9); Assert.Equal(before.Y, after.Y, 9);
        Assert.Equal(2, after.SpeedKmS); Assert.Equal(2, after.MaxSpeedKmS);
        Assert.Equal(2, state.InstalledModules.Single(m => m.ModuleTypeId == "module.engine.tetrarch").EngineMotion!.MaxSpeedKmS);
        var later = Player(PointCombatUS0018TK0002Tests.At(engine, 2000));
        Assert.Equal(2, double.Hypot(later.X - after.X, later.Y - after.Y) / 10, 6);
    }

    [Fact]
    public void Held_turn_is_reanchored_at_current_pose_with_new_rate_without_rng_draws()
    {
        using var engine = Create(3000);
        var start = PointCombatUS0018TK0002Tests.At(engine);
        string module = start.InstalledModules.Single(m => m.ModuleTypeId == "module.engine.tetrarch").ModuleId;
        engine.ReceiveCommand(new("hold", 1, start.PlayerShipObjectId!, module, ShipEngineCommandTypes.TurnHold,
            TurnDirection: TurnDirection.Right, Active: true));
        PointCombatUS0018TK0002Tests.At(engine);
        var before = Player(PointCombatUS0018TK0002Tests.At(engine, 1000));
        var rng = engine.CaptureLaunchScatterState();
        Apply(engine, "turnRateDegPerSec", 40);
        var after = Player(PointCombatUS0018TK0002Tests.At(engine, 1000));
        Assert.Equal(before.X, after.X, 9); Assert.Equal(before.Y, after.Y, 9);
        Assert.Equal(before.Direction, after.Direction, 9); Assert.Equal(40, after.TurnHoldRateDegPerSec);
        var later = Player(PointCombatUS0018TK0002Tests.At(engine, 2000));
        Assert.Equal((after.Direction + 40) % 360, later.Direction, 6);
        Assert.Equal(rng, engine.CaptureLaunchScatterState());
    }

    [Fact]
    public void Npc_segment_uses_new_limits_at_command_epoch_and_next_decision()
    {
        using var engine = Create(3000, hostile: true);
        PointCombatUS0018TK0002Tests.At(engine, 1000);
        Apply(engine, "turnRateDegPerSec", 48, "module.engine.strizh");
        PointCombatUS0018TK0002Tests.At(engine, 1000);
        var actors = engine.CaptureSaveState().GameState.HostileCombatState!.Actors;
        Assert.NotEmpty(actors);
        Assert.All(actors, a => Assert.InRange(Math.Abs(a.State.Segment.TurnRateDegPerSec), 0, 48));
        PointCombatUS0018TK0002Tests.At(engine, 1100);
        Assert.All(engine.CaptureSaveState().GameState.HostileCombatState!.Actors,
            a => Assert.InRange(Math.Abs(a.State.Segment.TurnRateDegPerSec), 0, 48));
    }

    [Fact]
    public void Evasion_keeps_escape_end_and_replans_return_with_new_acceleration()
    {
        using var engine = Create(3000, enemyWeapon: true);
        var initial = PointCombatUS0018TK0002Tests.At(engine);
        var npc = engine.CaptureSaveState().GameState.SpaceObjects.First(o => o.ObjectType == SpaceObjectType.NpcShip);
        string launcher = npc.Modules!.First(m => m.ModuleTypeId.Contains("torpedo", StringComparison.Ordinal)).ModuleId;
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(npc.ObjectId, launcher,
            npc.PositionX + 600, npc.PositionY, "slow", 0).Disposition);
        var threat = PointCombatUS0018TK0002Tests.At(engine).Objects.Single(o => o.ObservedProjectile is not null);
        string module = initial.InstalledModules.Single(m => m.ModuleTypeId == "module.engine.tetrarch").ModuleId;
        engine.ReceiveCommand(new("evade", 1, initial.PlayerShipObjectId!, module, ManeuverCommandTypes.Evade,
            ThreatProjectileId: threat.ObjectId, EvadeSide: EvadeSide.Right));
        var started = Player(PointCombatUS0018TK0002Tests.At(engine));
        Assert.NotNull(started.Evasion);
        var before = Player(PointCombatUS0018TK0002Tests.At(engine, 100));
        Apply(engine, "accelerationMps2", 1200);
        var after = Player(PointCombatUS0018TK0002Tests.At(engine, 100));
        Assert.Equal(before.X, after.X, 9); Assert.Equal(before.Y, after.Y, 9);
        Assert.Equal(started.Evasion.EndsAtMotionTimeMs, after.Evasion!.EndsAtMotionTimeMs);
        Assert.Equal(1.2, after.Evasion.Steering!.AccelerationKmS2);
        Assert.Equal(100, after.Evasion.SampleMotionTimeMs);
        Assert.True(after.Evasion.Steering.ReturnsAtMotionTimeMs >= after.Evasion.EndsAtMotionTimeMs);
    }

    [Fact]
    public void Reanchored_turn_and_clamp_do_not_depend_on_intermediate_snapshot_partitions()
    {
        static ObjectMotionSnapshot Run(bool split)
        {
            using var engine = Create(4500);
            var initial = PointCombatUS0018TK0002Tests.At(engine);
            string module = initial.InstalledModules.Single(m => m.ModuleTypeId == "module.engine.tetrarch").ModuleId;
            engine.ReceiveCommand(new("hold", 1, initial.PlayerShipObjectId!, module, ShipEngineCommandTypes.TurnHold,
                TurnDirection: TurnDirection.Right, Active: true));
            PointCombatUS0018TK0002Tests.At(engine);
            if (split) for (long t = 100; t < 1000; t += 100) PointCombatUS0018TK0002Tests.At(engine, t);
            PointCombatUS0018TK0002Tests.At(engine, 1000);
            Apply(engine, "maxSpeedKmS", 2);
            PointCombatUS0018TK0002Tests.At(engine, 1000);
            if (split) for (long t = 1100; t < 3000; t += 100) PointCombatUS0018TK0002Tests.At(engine, t);
            return Player(PointCombatUS0018TK0002Tests.At(engine, 3000));
        }
        var first = Run(false); var second = Run(true);
        Assert.Equal(first.X, second.X, 9); Assert.Equal(first.Y, second.Y, 9);
        Assert.Equal(first.Direction, second.Direction, 9); Assert.Equal(first.SpeedKmS, second.SpeedKmS);
    }
}
