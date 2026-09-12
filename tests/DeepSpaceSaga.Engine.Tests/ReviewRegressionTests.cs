
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Dialogue;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

// Regression coverage for the September 12 code review.
public class ReviewRegressionTests
{
    [Fact]
    public void Duplicate_trade_command_must_not_charge_twice()
    {
        using var engine = TradeCommandTests.CreateEngine();
        var command = new PlayerCommand("same-id", 1, "SPC-0001", "MOD-CARGO-01", TradeCommandTypes.Buy,
            ItemTypeId: "item.energy-cells", Quantity: 1);
        engine.ReceiveCommand(command);
        engine.ReceiveCommand(command);
        var first = engine.CaptureSnapshotForTests();
        var save = engine.CaptureSaveState();
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true));
        engine.ReceiveCommand(command);
        var second = engine.CaptureSnapshotForTests();
        Assert.Equal(first.PlayerCredits, second.PlayerCredits);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(second.CommandResults).Status);
    }

    [Theory]
    [InlineData(TradeCommandTypes.Buy)]
    [InlineData(TradeCommandTypes.Sell)]
    [InlineData(TradeCommandTypes.Refuel)]
    public void Trade_overflow_must_leave_all_balances_and_cargo_unchanged(string commandType)
    {
        using var engine = TradeCommandTests.CreateEngine(stationCredits: long.MaxValue,
            playerCredits: commandType == TradeCommandTypes.Sell ? long.MaxValue : 100_000,
            shipCargo: [("item.energy-cells", 10)]);
        var before = engine.CaptureSaveState();
        bool refuel = commandType == TradeCommandTypes.Refuel;
        engine.ReceiveCommand(new("trade", 1, "SPC-0001", refuel ? "MOD-ENG-01" : "MOD-CARGO-01", commandType,
            ItemTypeId: refuel ? "item.fuel" : "item.energy-cells", Quantity: 1));
        var result = engine.CaptureSnapshotForTests();
        Assert.Equal("value_overflow", Assert.Single(result.CommandResults).ReasonCode);
        Assert.Equal(before.GameState.PlayerTokens, result.PlayerCredits);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(before.GameState.SpaceObjects),
            System.Text.Json.JsonSerializer.Serialize(engine.CaptureSaveState().GameState.SpaceObjects));
        Assert.InRange(engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "STATION-01").Credits, 0, long.MaxValue);
    }

    [Fact]
    public void Saving_after_cancelling_Approach_must_preserve_heading()
    {
        using var engine = ApproachCommandTests.CreateEngine(shipSpeedMps: 3000, shipX: 9000d, shipY: 10500d, shipDirectionDegrees: 270, targetSpeedMps: 1000, targetDirectionDegrees: 90, trailDistanceKm: 1);
        engine.ReceiveCommand(new("approach", 1, "SHIP", "ENGINE-1", NavigationComputerCommandTypes.Approach, TargetObjectId: "TARGET"));
        engine.CaptureSnapshotForTests();
        engine.ReceiveCommand(new("cancel", 2, "SHIP", "ENGINE-1", ShipEngineCommandTypes.CancelAll));
        var before = engine.CaptureSnapshotForTests(137).Objects.Single(o => o.ObjectId == "SHIP");
        var save = engine.CaptureSaveStateForTests(137, SimulationSpeed.Speed0);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), allowNonZeroGameTime: true));
        var after = engine.CaptureSnapshotForTests(137).Objects.Single(o => o.ObjectId == "SHIP");
        Assert.Equal(before.Direction, after.Direction, 8);
    }

    [Fact]
    public void Accepted_player_id_case_must_resolve_to_an_actual_object()
    {
        using var engine = TradeCommandTests.CreateEngine();
        var original = engine.CaptureSaveState();
        var save = original with { GameState = original.GameState with { PlayerShipObjectId = "spc-0001" } };
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save)));
        var snapshot = engine.CaptureSnapshotForTests();
        Assert.Contains(snapshot.Objects, o => o.ObjectId == snapshot.PlayerShipObjectId);
    }

    [Fact]
    public void Security_must_use_the_active_route_when_checking_the_deadline()
    {
        using var engine = ApproachCommandTests.CreateEngine(shipSpeedMps: 2000);
        var route = new ApproachRoute(0, 0, 0, 2, 90, "RSL", 20, 40, 0, 100, 0, 90, 1, 10);
        var ship = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "SHIP");
        ship = ship with { Modules = ship.Modules.SetItem(0, ship.Modules[0] with {
            ActiveCycle = new ActiveCycleData("test", 0, 1000, NavigationComputerCommandTypes.Approach, true, ApproachRoute: route) }) };
        // At 1.1s the true curved route is 19.49 units from the station, inside its 20-unit zone.
        // Straight extrapolation incorrectly places the ship 22 units away, outside it.
        var actual = ApproachLineCaptureMath.PredictPose(route, 1100);
        Assert.True(actual.X * actual.X + actual.Y * actual.Y < 400);
        var objects = new List<SpaceObjectRuntime> { ship,
            new(new ObjectMotionSnapshot("station", 0, 0, 0, 0), SpaceObjectType.Station, 0, [], SecurityZoneRadiusKm: 2) };
        var progress = DialogueProgressState.Empty with {
            SecurityIncidents = [new("test", "station", "piracy", 0, 1100)] };
        StationSecuritySystem.Update(objects, "SHIP", progress, 1100);
        Assert.True(objects[0].IsDestroyed);
    }

}
