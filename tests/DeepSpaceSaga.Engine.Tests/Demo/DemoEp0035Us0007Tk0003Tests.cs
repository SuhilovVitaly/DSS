using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0007Tk0003Tests
{
    [Fact]
    public void DemoStart_HasSensorWithoutOverlap_AndScannerRemainsInstalled()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = engine.CaptureSaveState();
        var ship = save.GameState.SpaceObjects.Single(s => s.ObjectId == save.GameState.PlayerShipObjectId);
        Assert.NotNull(ship.Modules);
        Assert.Single(ship.Modules, m => m.ModuleTypeId == "module.sensor.player-start");
        Assert.Contains(ship.Modules, m => m.ModuleTypeId == "module.scanner.mk1");
        var cells = ship.Modules.SelectMany(m => m.OccupiedCells!).ToArray();
        Assert.Equal(cells.Length, cells.Distinct().Count());
    }

    [Fact]
    public void PlayerRadius_Is120PercentOfPirateModule()
    {
        string root = DemoEp0033Us0001Tk0003Tests.Root();
        var categories = EngineContentLoader.LoadModuleCategories(root + "/Data/module-types.json");
        var sensors = EngineContentLoader.LoadModuleImplementations(root + "/Data/Modules/Sensor", categories);
        double pirate = sensors.Single(m => m.TypeId == "module.sensor.pirate-start").SensorRadiusWorldUnits!.Value;
        Assert.Equal(2000, pirate);
        Assert.Equal(1.2 * pirate, sensors.Single(m => m.TypeId == "module.sensor.player-start").SensorRadiusWorldUnits);
    }
}
