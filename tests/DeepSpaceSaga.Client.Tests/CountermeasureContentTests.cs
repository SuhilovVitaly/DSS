using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public class CountermeasureContentTests
{
    private static GameDataRegistry Registry(bool output) => EngineContentLoader.LoadRegistryFromSettingsFile(
        Path.Combine(output ? AppContext.BaseDirectory : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client")), "Settings.json"), out _, out _);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Catalog_registers_manual_fire_and_preserves_automatic_toggles(bool output)
    {
        var registry = Registry(output);
        var module = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.countermeasure.launcher.basic"));
        Assert.Equal(new[] { DefenseCommandTypes.Fire, DefenseCommandTypes.Enable, DefenseCommandTypes.Disable }, module.CommandTypeIds);
        Assert.Equal(1, module.SlotSize);
        Assert.Equal(1, module.BaseCycleTimeMs); // Required content metadata; defense reload has its own physical deadline.
        foreach (var id in module.CommandTypeIds)
        {
            var command = registry.CommandDefinitions.GetDefinition(registry.CommandDefinitions.GetIndex(id));
            Assert.Equal(id == DefenseCommandTypes.Fire ? "object" : "none", command.Target);
            Assert.Equal("module.countermeasure.launcher", command.Type);
            Assert.Equal(0, command.ActivationEnergyCellsCost);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Pd_catalog_matches_ep9(bool output)
    {
        var registry = Registry(output);
        var module = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.countermeasure.launcher.basic"));
        Assert.Equal(58m, module.CountermeasureAccuracy);
        Assert.Equal(20, module.CountermeasureSpeedKmS);
        Assert.Equal(90, module.CountermeasureTurnRateDegPerSec);
        Assert.Equal(150, module.CountermeasureRangeKm);
        Assert.Equal(150, module.CountermeasureManualRangeKm);
        Assert.Equal(30000, module.CountermeasureMaxFlightTimeMs);
        Assert.Equal(14500, module.CountermeasureReloadMs);
        Assert.Equal(2, module.Barrels);
        Assert.Equal(2000, module.BurstIntervalMs);
        var torpedo = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.torpedo.launcher.basic"));
        Assert.Equal(50m, torpedo.TorpedoManeuverability);
        Assert.Equal(150, torpedo.TorpedoRangeKm);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Shared_module_has_same_values_for_both_sides(bool output)
    {
        string root = output ? AppContext.BaseDirectory : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios", "PlayerShipOnly", "scenario.json"));
        var ships = scenario.GameState.SpaceObjects.Where(o => o.ObjectType is "PlayerShip" or "NpcShip").ToArray();
        Assert.Contains(ships, o => o.ObjectId == scenario.GameState.PlayerShipObjectId);
        Assert.Contains(ships, o => o.ObjectType == "NpcShip");
        var registry = Registry(output);
        foreach (var ship in ships)
        {
            var defenses = ship.Modules!.Where(m => m.ModuleTypeId == "module.countermeasure.launcher.basic").ToArray();
            Assert.Equal(ship.ObjectType == "PlayerShip" ? 2 : 1, defenses.Length);
            var launcher = Assert.Single(ship.Modules!, m => m.ModuleTypeId == "module.torpedo.launcher.basic");
            var defenseType = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(defenses[0].ModuleTypeId));
            Assert.All(defenses, m => Assert.Equal(defenses[0].ModuleTypeId, m.ModuleTypeId));
            var launcherType = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(launcher.ModuleTypeId));
            Assert.Equal(58m, defenseType.CountermeasureAccuracy);
            Assert.Equal(50m, launcherType.TorpedoManeuverability);
            Assert.Equal(150, launcherType.TorpedoRangeKm);
            Assert.Equal(150, defenseType.CountermeasureRangeKm);
            Assert.Equal(150, defenseType.CountermeasureManualRangeKm);
            Assert.Contains(DefenseCommandTypes.Fire, defenseType.CommandTypeIds);
        }
        using var engine = new DeepSpaceSaga.Engine.SimulationEngine(registry);
        engine.LoadScenario(scenario);
        var modules = engine.CaptureSnapshotForTests().InstalledModules.Where(m => m.Defense is not null).ToArray();
        Assert.Equal(2, modules.Length);
        Assert.All(modules, m =>
        {
            Assert.Contains(m.Commands, c => c.CommandTypeId == DefenseCommandTypes.Fire && c.Target == "object");
            Assert.Equal(DefenseState.Ready, m.Defense!.State);
            Assert.Null(m.ActiveCommandType);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Single_barrel_pd_exists(bool output)
    {
        var registry = Registry(output);
        var basic = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.countermeasure.launcher.basic"));
        var single = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.countermeasure.launcher.single"));
        Assert.Equal("module.countermeasure.launcher", single.CategoryTypeId);
        Assert.Equal(1, single.Barrels);
        Assert.Equal(basic.BurstIntervalMs, single.BurstIntervalMs);
        Assert.Equal(basic.CountermeasureAccuracy, single.CountermeasureAccuracy);
        Assert.Equal(basic.CountermeasureReloadMs, single.CountermeasureReloadMs);
        Assert.Equal(basic.CommandTypeIds, single.CommandTypeIds);
    }
}
