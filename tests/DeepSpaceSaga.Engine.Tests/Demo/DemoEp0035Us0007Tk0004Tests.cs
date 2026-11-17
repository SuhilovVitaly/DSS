using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0007Tk0004Tests
{
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
