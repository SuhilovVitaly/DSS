using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using DeepSpaceSaga.Engine.Content;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0006TK0003Tests
{
    private const string Engine = "MOD-PLAYER-ENGINE-01";
    private static AuthoritativeSnapshot At(SimulationEngine engine, long time = 0) => engine.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);
    private static ObjectMotionSnapshot Ship(AuthoritativeSnapshot snapshot) => snapshot.Objects.Single(o => o.ObjectId == Player);
    private static SimulationEngine Battle() => Create(TorpedoImpactTests.Scenario(o => o.ObjectId == Player ? o with
    { SpeedMps = 2000, DirectionDegrees = 0, MovementType = "Linear" } : o));
    private static PlayerCommand Hold(string id = "hold", TurnDirection direction = TurnDirection.Right, bool? active = true) =>
        new(id, 1, Player, Engine, ShipEngineCommandTypes.TurnHold, TurnDirection: direction, Active: active);

    [Theory]
    [InlineData(TurnDirection.Right, 4)]
    [InlineData(TurnDirection.Left, -4)]
    public void Turn_hold_integrates_until_release(TurnDirection direction, double rate)
    {
        using var engine = Battle();
        engine.ReceiveCommand(Hold(direction: direction));
        var start = Ship(At(engine));
        Assert.Equal(rate, start.TurnHoldRateDegPerSec);
        var expected = new LinearMotionPredictor().Predict(start, 1500);
        var actual = Ship(At(engine, 1500));
        Assert.Equal((rate * 1.5 + 360) % 360, actual.Direction, 10);
        Assert.Equal(expected.X, actual.X, 10);
        Assert.Equal(expected.Y, actual.Y, 10);
        engine.ReceiveCommand(Hold("release", direction, false));
        var released = Ship(At(engine, 1500));
        Assert.Null(released.TurnHoldRateDegPerSec);
        var coast = new LinearMotionPredictor().Predict(released, 1000);
        actual = Ship(At(engine, 2500));
        Assert.Equal(released.Direction, actual.Direction, 10);
        Assert.Equal(coast.X, actual.X, 10);
        Assert.Equal(coast.Y, actual.Y, 10);
    }

    [Fact]
    public void Wrong_key_release_and_replay_do_not_cancel_or_restart_the_turn()
    {
        using var engine = Battle();
        engine.ReceiveCommand(Hold()); At(engine);
        At(engine, 250);
        engine.ReceiveCommand(Hold());
        engine.ReceiveCommand(Hold("left-release", TurnDirection.Left, false));
        Assert.Equal(4, Ship(At(engine, 500)).TurnHoldRateDegPerSec);
        Assert.Equal(2, Ship(At(engine, 500)).Direction, 10);
        engine.ReceiveCommand(Hold("left", TurnDirection.Left)); At(engine, 500);
        Assert.Equal(0, Ship(At(engine, 1000)).Direction, 10);
    }

    [Fact]
    public void Turn_hold_survives_pause_and_stops_on_session_reset()
    {
        using var engine = Battle();
        engine.ReceiveCommand(Hold()); At(engine); At(engine, 500);
        engine.SetSpeed(SimulationSpeed.Speed0);
        Assert.Equal(4, Ship(At(engine, 500)).TurnHoldRateDegPerSec);
        engine.SetSpeed(SimulationSpeed.Speed1);
        Assert.Equal(4, Ship(At(engine, 1000)).Direction, 10);
        engine.ReceiveCommand(Hold("again")); At(engine, 1000);
        engine.LoadScenario(TorpedoImpactTests.Scenario(o => o));
        Assert.Null(Ship(At(engine)).TurnHoldRateDegPerSec);
    }

    [Fact]
    public void Invalid_payload_or_engine_override_preserves_the_held_control()
    {
        using var engine = Battle();
        engine.ReceiveCommand(Hold()); At(engine);
        var expected = JsonSerializer.Serialize(Ship(At(engine, 500)));
        foreach (var command in new[] { Hold("bad", (TurnDirection)999), Hold("null", active: null),
            new PlayerCommand("bad-orbit", 1, Player, Engine, ShipEngineCommandTypes.Orbit) })
        {
            engine.ReceiveCommand(command);
            var result = At(engine, 500);
            Assert.Equal(CommandResultStatus.Rejected, Assert.Single(result.CommandResults).Status);
            Assert.Equal(expected, JsonSerializer.Serialize(Ship(result)));
        }
        engine.ReceiveCommand(new("cancel", 1, Player, Engine, ShipEngineCommandTypes.MaintainCourse));
        Assert.Null(Ship(At(engine, 500)).TurnHoldRateDegPerSec);
        Assert.Equal(2, Ship(At(engine, 1000)).Direction, 10);
    }

    [Fact]
    public void Outside_combat_existing_step_still_turns_once()
    {
        using var engine = Battle();
        engine.ReceiveCommand(new("step", 1, Player, Engine, ShipEngineCommandTypes.TurnRightStep));
        At(engine);
        var actual = Ship(At(engine, 2000));
        Assert.Equal(1, actual.Direction, 10);
        Assert.Null(actual.TurnHoldRateDegPerSec);
    }
}
