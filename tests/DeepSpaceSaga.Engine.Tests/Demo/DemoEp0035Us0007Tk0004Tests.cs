using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0007Tk0004Tests
{
    [Fact]
    public void AuthoritativeContact_UsesMotionTime_AndSurvivesPausedLoad()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = engine.CaptureSaveState();
        var target = save.GameState.SpaceObjects.Single(s => s.ObjectId == "SPC-0003");
        string player = save.GameState.PlayerShipObjectId;
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId != player ? s : s with
                {
                    PositionX = target.PositionX - 2401,
                    PositionY = target.PositionY,
                    SpeedMps = 100,
                    DirectionDegrees = 90,
                    IsDocked = false,
                    DockedStationObjectId = null,
                    Orbit = null
                }).ToArray()
            }
        }, isSave: true);
        Assert.False(engine.ObserveSensorContact(player, target.ObjectId).IsDetected);
        engine.CaptureSnapshotForTests(300000, DeepSpaceSaga.Contracts.SimulationSpeed.Speed0, 1000);
        var observed = engine.ObserveSensorContact(player, target.ObjectId);
        Assert.True(observed.IsDetected);
        Assert.Equal(1000, observed.ObservedMotionTimeMs);
        Assert.False(engine.ObserveSensorContact(target.ObjectId, player).IsDetected);
        var checkpoint = engine.CaptureSaveStateForTests(300000, DeepSpaceSaga.Contracts.SimulationSpeed.Speed0, 1000);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(checkpoint), true, true), isSave: true);
        Assert.Equal(observed, engine.ObserveSensorContact(player, target.ObjectId));
        engine.CaptureSnapshot();
        Assert.Equal(observed, engine.ObserveSensorContact(player, target.ObjectId));
    }

    [Theory]
    [InlineData(2400, true)]
    [InlineData(2400.001, false)]
    [InlineData(2399.999, true)]
    public void Boundary_InsideOutside(double distance, bool expected) =>
        Assert.Equal(expected, SimulationEngine.SensorCovers(2400, 0, 0, distance, 0));

    [Fact]
    public void PlayerOnlyBand_PirateDoesNotDetect()
    {
        Assert.True(SimulationEngine.SensorCovers(2400, 0, 0, 2200, 0));
        Assert.False(SimulationEngine.SensorCovers(2000, 2200, 0, 0, 0));
        Assert.False(SimulationEngine.SensorCovers(0, 0, 0, 0, 0));
    }

    [Theory]
    [InlineData("Off", "Ready", 150, false)]
    [InlineData("On", "Disabled", 150, false)]
    [InlineData("On", "Ready", 0, false)]
    [InlineData("On", "Ready", 150, true)]
    public void DisabledSensor_DetectsNothing(string power, string state, int structure, bool destroyed)
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = engine.CaptureSaveState();
        var objects = save.GameState.SpaceObjects.Select(s => s.ObjectId != save.GameState.PlayerShipObjectId ? s : s with
        {
            Modules = s.Modules!.Select(m => m.ModuleId != "MOD-PLAYER-SENSOR-01" ? m : m with
            { PowerState = power, OperationalState = state, StructurePoints = structure, Destroyed = destroyed }).ToArray()
        }).ToArray();
        engine.LoadScenario(save with { GameState = save.GameState with { SpaceObjects = objects } }, isSave: true);
        var sensor = engine.CaptureSnapshot().DemoCampaign!.Sensor!;
        Assert.False(sensor.IsOperational);
        Assert.Equal(0, sensor.RadiusWorldUnits);
    }

    [Fact]
    public void SaveLoad_PreservesSensor_AndLegacyDoesNotReceiveModule()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var before = engine.CaptureSnapshot().DemoCampaign!.Sensor;
        Assert.Equal(2400, before!.RadiusWorldUnits);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true, true);
        engine.LoadScenario(save, isSave: true);
        Assert.Equal(before, engine.CaptureSnapshot().DemoCampaign!.Sensor);
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId != save.GameState.PlayerShipObjectId ? s : s with
                { Modules = s.Modules!.Where(m => m.ModuleId != "MOD-PLAYER-SENSOR-01").ToArray() }).ToArray()
            }
        }, isSave: true);
        Assert.Null(engine.CaptureSnapshot().DemoCampaign!.Sensor);
    }
}
