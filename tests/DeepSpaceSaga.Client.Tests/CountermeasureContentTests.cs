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
    public void Catalog_registers_point_launch_without_automatic_toggles(bool output)
    {
        var registry = Registry(output);
        var module = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.countermeasure.launcher.basic"));
        Assert.Equal(new[] { DefenseCommandTypes.Launch }, module.CommandTypeIds);
        Assert.Equal(1, module.SlotSize);
        Assert.Equal(1, module.BaseCycleTimeMs); // Required content metadata; defense reload has its own physical deadline.
        foreach (var id in module.CommandTypeIds)
        {
            var command = registry.CommandDefinitions.GetDefinition(registry.CommandDefinitions.GetIndex(id));
            Assert.Equal("point", command.Target);
            Assert.Equal("module.countermeasure.launcher", command.Type);
            Assert.Equal(0, command.ActivationEnergyCellsCost);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Pd_catalog_matches_ep12(bool output)
    {
        var registry = Registry(output);
        var module = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.countermeasure.launcher.basic"));
        Assert.Null(module.CountermeasureAccuracy);
        Assert.Null(module.CountermeasureSpeedKmS);
        Assert.Null(module.CountermeasureTurnRateDegPerSec);
        Assert.Null(module.CountermeasureRangeKm);
        Assert.Null(module.CountermeasureManualRangeKm);
        Assert.Null(module.CountermeasureMaxFlightTimeMs);
        Assert.Null(module.CountermeasureReloadMs);
        Assert.Null(module.Barrels);
        Assert.Null(module.BurstIntervalMs);
        Assert.Equal(30, module.RangeKm); Assert.Equal(12500, module.ReloadMs); Assert.Equal(.08, module.ScatterPercent);
        Assert.Equal(new[] { 15d, 25d }, module.Profiles.Select(p => p.SpeedKmS));
        Assert.Equal(new[] { 1d, .5 }, module.Profiles.Select(p => p.BlastRadiusKm));
        var torpedo = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.torpedo.launcher.basic"));
        Assert.Null(torpedo.TorpedoManeuverability);
        Assert.Null(torpedo.TorpedoRangeKm);
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
            Assert.Null(defenseType.CountermeasureAccuracy);
            Assert.Null(launcherType.TorpedoManeuverability);
            Assert.Null(launcherType.TorpedoRangeKm);
            Assert.Null(defenseType.CountermeasureRangeKm);
            Assert.Null(defenseType.CountermeasureManualRangeKm);
            Assert.Equal(30, defenseType.RangeKm); Assert.Equal(70, launcherType.RangeKm);
            Assert.Contains(DefenseCommandTypes.Launch, defenseType.CommandTypeIds);
        }
        using var engine = new DeepSpaceSaga.Engine.SimulationEngine(registry);
        engine.LoadScenario(scenario);
        var modules = engine.CaptureSnapshotForTests().InstalledModules.Where(m => m.Defense is not null).ToArray();
        Assert.Equal(2, modules.Length);
        Assert.All(modules, m =>
        {
            Assert.Contains(m.Commands, c => c.CommandTypeId == DefenseCommandTypes.Launch && c.Target == "point");
            Assert.Equal(DefenseState.Ready, m.Defense!.State);
            Assert.Null(m.ActiveCommandType);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Retired_single_launcher_is_absent(bool output)
    {
        var registry = Registry(output);
        var basic = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.countermeasure.launcher.basic"));
        Assert.True(basic.IsPointLauncher);
        Assert.False(registry.ModuleTypes.Contains("module.countermeasure.launcher.single"));
    }
}
