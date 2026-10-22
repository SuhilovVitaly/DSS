using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class SpeedHoldEngineTests
{
    private const string EngineId = "MOD-PLAYER-ENGINE-01", PlayerId = "SPC-0001";
    private static AuthoritativeSnapshot At(SimulationEngine engine, long time = 0) => PointCombatUS0018TK0002Tests.At(engine, time);
    private static ObjectMotionSnapshot Player(AuthoritativeSnapshot snapshot) => snapshot.Objects.Single(o => o.ObjectId == snapshot.PlayerShipObjectId);
    private static PlayerCommand Speed(string id = "speed", SpeedDirection direction = SpeedDirection.Accelerate, bool active = true, bool latched = false) =>
        new(id, 1, PlayerId, EngineId, ShipEngineCommandTypes.SpeedHold, Active: active, SpeedDirection: direction, Latched: latched);
    private static PlayerCommand Turn(string id = "turn", TurnDirection direction = TurnDirection.Right, bool active = true) =>
        new(id, 1, PlayerId, EngineId, ShipEngineCommandTypes.TurnHold, Active: active, TurnDirection: direction);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Independent_channels_keep_each_other_and_match_analytical_two_second_arc(bool speedFirst)
    {
        using var engine = DebugTuningShipTests.Create(3000);
        var initial = Player(At(engine));
        engine.ReceiveCommand(speedFirst ? Speed() : Turn()); At(engine);
        engine.ReceiveCommand(speedFirst ? Turn() : Speed());
        var accepted = Player(At(engine));
        Assert.Equal(.6, accepted.SpeedHoldAccelerationKmS2); Assert.Equal(20, accepted.TurnHoldRateDegPerSec);
        var expected = HostileOrbitMath.Sample(new(0, long.MaxValue, 0, initial.X, initial.Y, 3, 0, .6, 20), 2000);
        var after = Player(At(engine, 2000));
        Equal(after, expected.X, expected.Y, 4.2, 40);
        engine.ReceiveCommand(Turn("other-release", TurnDirection.Left, false));
        engine.ReceiveCommand(Speed("other-speed-release", SpeedDirection.Brake, false));
        var unchanged = Player(At(engine, 2000)); Assert.Equal(.6, unchanged.SpeedHoldAccelerationKmS2); Assert.Equal(20, unchanged.TurnHoldRateDegPerSec);
        engine.ReceiveCommand(Turn("release-turn", active: false));
        Assert.Equal(.6, Player(At(engine, 2000)).SpeedHoldAccelerationKmS2);
        engine.ReceiveCommand(Speed("release-speed", active: false));
        var released = Player(At(engine, 2000)); Assert.Null(released.SpeedHoldAccelerationKmS2); Assert.Null(released.TurnHoldRateDegPerSec);
        var coast = Player(At(engine, 3000)); Assert.Equal(released.Direction, coast.Direction); Assert.Equal(released.SpeedKmS, coast.SpeedKmS);
    }

    [Theory]
    [InlineData(SpeedDirection.Accelerate, false)]
    [InlineData(SpeedDirection.Accelerate, true)]
    [InlineData(SpeedDirection.Brake, false)]
    public void Engine_and_forecast_agree_across_speed_boundary_and_step_partitions(SpeedDirection direction, bool latched)
    {
        using var direct = DebugTuningShipTests.Create(3000); using var partitioned = DebugTuningShipTests.Create(3000);
        foreach (var engine in new[] { direct, partitioned }) { engine.ReceiveCommand(Speed(direction: direction, latched: latched)); engine.ReceiveCommand(Turn()); At(engine); }
        var start = Player(At(direct)); var forecast = CurrentManeuverForecast.Sample(start, 20000);
        var whole = Player(At(direct, 20000));
        for (long time = 73; time < 20000; time += 73) At(partitioned, time);
        var pieces = Player(At(partitioned, 20000));
        Equal(whole, forecast.X, forecast.Y, forecast.SpeedKmS, forecast.Direction);
        Equal(pieces, whole.X, whole.Y, whole.SpeedKmS, whole.Direction);
        Assert.Equal(direction == SpeedDirection.Accelerate ? 5 : 0, whole.SpeedKmS);
        Assert.Equal(latched ? null : start.SpeedHoldAccelerationKmS2, whole.SpeedHoldAccelerationKmS2);
        Assert.False(whole.SpeedHoldLatched); Assert.Equal(20, whole.TurnHoldRateDegPerSec);
    }

    [Fact]
    public void Shift_latch_survives_turn_and_is_replaced_by_braking()
    {
        using var engine = DebugTuningShipTests.Create(3000);
        engine.ReceiveCommand(Speed(latched: true)); At(engine);
        engine.ReceiveCommand(Turn()); var moving = Player(At(engine, 1000)); Assert.True(moving.SpeedHoldLatched);
        engine.ReceiveCommand(Speed("brake", SpeedDirection.Brake));
        var braking = Player(At(engine, 1000)); Assert.False(braking.SpeedHoldLatched); Assert.Equal(-.6, braking.SpeedHoldAccelerationKmS2); Assert.Equal(20, braking.TurnHoldRateDegPerSec);
        engine.ReceiveCommand(Speed("late-accelerate-release", active: false));
        Assert.Equal(-.6, Player(At(engine, 1000)).SpeedHoldAccelerationKmS2);
    }

    [Theory]
    [InlineData(ShipEngineCommandTypes.Accelerate)]
    [InlineData(ShipEngineCommandTypes.Brake)]
    [InlineData(ShipEngineCommandTypes.TurnRightUntilCancel)]
    [InlineData(ShipEngineCommandTypes.Orbit)]
    [InlineData(NavigationComputerCommandTypes.Approach)]
    public void Hold_cancels_existing_engine_cycle_with_receipt(string command)
    {
        using var engine = DebugTuningShipTests.Create(3000);
        engine.ReceiveCommand(new("old", 1, PlayerId, EngineId, command, TargetWorldX: 100, TargetWorldY: 100, TargetObjectId: "SPC-0002"));
        var state = At(engine); Assert.Equal(command, state.InstalledModules.Single(m => m.ModuleId == EngineId).ActiveCommandType);
        engine.ReceiveCommand(Speed()); state = At(engine);
        Assert.Contains(state.CommandResults, r => r.CommandId == "old" && r.Status == CommandResultStatus.Cancelled);
        Assert.Equal(.6, Player(state).SpeedHoldAccelerationKmS2);
        engine.ReceiveCommand(new("override", 1, PlayerId, EngineId, ShipEngineCommandTypes.MaintainSpeed));
        Assert.Null(Player(At(engine)).SpeedHoldAccelerationKmS2);
    }

    [Fact]
    public void Invalid_patch_is_atomic_and_debug_patch_reanchors_both_controls_without_rng()
    {
        using var engine = DebugTuningShipTests.Create(3000);
        engine.ReceiveCommand(Speed()); engine.ReceiveCommand(Turn()); At(engine);
        var before = Player(At(engine, 1000)); var rng = engine.CaptureLaunchScatterState();
        engine.ReceiveCommand(Speed("bad") with { SpeedDirection = (SpeedDirection)999 });
        var state = At(engine, 1000); Assert.Equal(CommandReasonCodes.InvalidSpeedPayload, Assert.Single(state.CommandResults).ReasonCode);
        Equal(Player(state), before.X, before.Y, before.SpeedKmS, before.Direction);
        engine.ReceiveCommand(new("tune", 1, "", "", DebugTuningCommandTypes.Apply,
            DebugTuning: new([new("module.engine.tetrarch", "accelerationMps2", 1200), new("module.engine.tetrarch", "turnRateDegPerSec", 40)])));
        var after = Player(At(engine, 1000)); Equal(after, before.X, before.Y, before.SpeedKmS, before.Direction);
        Assert.Equal(1.2, after.SpeedHoldAccelerationKmS2); Assert.Equal(40, after.TurnHoldRateDegPerSec);
        var expected = CurrentManeuverForecast.Sample(after, 1000); var later = Player(At(engine, 2000));
        Equal(later, expected.X, expected.Y, expected.SpeedKmS, expected.Direction); Assert.Equal(rng, engine.CaptureLaunchScatterState());
    }

    [Fact]
    public void Evasion_releases_both_axes_and_new_hold_cancels_evasion_at_continuous_pose()
    {
        using var engine = DebugTuningShipTests.Create(3000, enemyWeapon: true);
        var initial = At(engine);
        var npc = engine.CaptureSaveState().GameState.SpaceObjects.First(o => o.ObjectType == SpaceObjectType.NpcShip);
        string launcher = npc.Modules!.First(m => m.ModuleTypeId.Contains("torpedo", StringComparison.Ordinal)).ModuleId;
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(npc.ObjectId, launcher, npc.PositionX + 600, npc.PositionY, "slow", 0).Disposition);
        string threat = At(engine).Objects.Single(o => o.ObservedProjectile is not null).ObjectId;
        engine.ReceiveCommand(Speed()); engine.ReceiveCommand(Turn()); At(engine);
        engine.ReceiveCommand(new("evade", 1, PlayerId, EngineId, ManeuverCommandTypes.Evade, ThreatProjectileId: threat, EvadeSide: EvadeSide.Right));
        var evading = Player(At(engine)); Assert.NotNull(evading.Evasion); Assert.Null(evading.SpeedHoldAccelerationKmS2); Assert.Null(evading.TurnHoldRateDegPerSec);
        var before = Player(At(engine, 100)); engine.ReceiveCommand(Speed("interrupt"));
        var state = At(engine, 100); var after = Player(state);
        Assert.Null(after.Evasion); Assert.Equal(.6, after.SpeedHoldAccelerationKmS2);
        Equal(after, before.X, before.Y, before.SpeedKmS, before.Direction);
        Assert.Contains(state.CommandResults, r => r.CommandId == "evade" && r.Status == CommandResultStatus.Cancelled);
    }

    [Fact]
    public void Save_records_sampled_pose_and_load_releases_both_controls()
    {
        using var engine = DebugTuningShipTests.Create(3000);
        engine.ReceiveCommand(Speed(latched: true)); engine.ReceiveCommand(Turn()); At(engine);
        var before = Player(At(engine, 1234));
        var save = engine.CaptureSaveStateForTests(1234 * 300, SimulationSpeed.Speed0, 1234);
        string json = ScenarioLoader.Serialize(save); Assert.DoesNotContain("SpeedHold", json); Assert.DoesNotContain("TurnHold", json);
        using var restored = DebugTuningShipTests.Create(); restored.LoadScenario(save, true);
        var loaded = Player(At(restored, 1234)); Equal(loaded, before.X, before.Y, before.SpeedKmS, before.Direction);
        Assert.Null(loaded.SpeedHoldAccelerationKmS2); Assert.Null(loaded.TurnHoldRateDegPerSec); Assert.False(loaded.SpeedHoldLatched);
        var coast = Player(At(restored, 2234)); Assert.Equal(before.Direction, coast.Direction); Assert.Equal(before.SpeedKmS, coast.SpeedKmS);
        Assert.All(At(engine, 1234).Objects.Where(o => o.ObjectId != PlayerId), o => { Assert.Null(o.SpeedHoldAccelerationKmS2); Assert.False(o.SpeedHoldLatched); });
    }

    [Fact]
    public void Paused_session_accepts_controls_and_disabled_engine_rejects_without_motion_change()
    {
        using var engine = DebugTuningShipTests.Create(3000);
        engine.SetSpeed(SimulationSpeed.Speed0); engine.ReceiveCommand(Speed()); engine.ReceiveCommand(Turn());
        var first = Player(At(engine)); var again = Player(At(engine)); Equal(again, first.X, first.Y, first.SpeedKmS, first.Direction);
        Assert.Equal(.6, again.SpeedHoldAccelerationKmS2);
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0);
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == PlayerId ? o with
                { Modules = o.Modules!.Select(m => m.ModuleId == EngineId ? m with { PowerState = "Off" } : m).ToArray() } : o).ToArray()
            }
        }, true);
        engine.ReceiveCommand(Speed("disabled")); var state = At(engine);
        Assert.Equal(CommandReasonCodes.EngineDisabled, Assert.Single(state.CommandResults).ReasonCode); Assert.Null(Player(state).SpeedHoldAccelerationKmS2);
    }

    private static void Equal(ObjectMotionSnapshot actual, double x, double y, double speed, double direction)
    {
        Assert.InRange(Math.Abs(actual.X - x), 0, 1e-6); Assert.InRange(Math.Abs(actual.Y - y), 0, 1e-6);
        Assert.InRange(Math.Abs(actual.SpeedKmS - speed), 0, 1e-6); Assert.InRange(Math.Abs(actual.Direction - direction), 0, 1e-6);
    }
}
