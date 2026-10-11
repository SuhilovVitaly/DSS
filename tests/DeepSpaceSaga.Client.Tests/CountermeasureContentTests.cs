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
    public void Catalog_has_automatic_defense_only(bool output)
    {
        var registry = Registry(output);
        var module = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.countermeasure.launcher.basic"));
        Assert.Equal(new[] { DefenseCommandTypes.Enable, DefenseCommandTypes.Disable }, module.CommandTypeIds);
        Assert.Equal(1, module.SlotSize);
        Assert.Equal(1, module.BaseCycleTimeMs); // Required content metadata; defense reload has its own physical deadline.
        foreach (var id in module.CommandTypeIds)
        {
            var command = registry.CommandDefinitions.GetDefinition(registry.CommandDefinitions.GetIndex(id));
            Assert.Equal("none", command.Target);
            Assert.Equal(0, command.ActivationEnergyCellsCost);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Basic_weapon_values_load_from_real_catalog(bool output)
    {
        var registry = Registry(output);
        var module = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.countermeasure.launcher.basic"));
        Assert.Equal(55m, module.CountermeasureAccuracy);
        Assert.Equal(12, module.CountermeasureSpeedKmS);
        Assert.Equal(90, module.CountermeasureTurnRateDegPerSec);
        Assert.Equal(100, module.CountermeasureRangeKm);
        Assert.Equal(200, module.CountermeasureManualRangeKm);
        Assert.Equal(60000, module.CountermeasureMaxFlightTimeMs);
        Assert.Equal(10000, module.CountermeasureReloadMs);
        var torpedo = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.torpedo.launcher.basic"));
        Assert.Equal(5m, torpedo.TorpedoManeuverability);
        Assert.Equal(200, torpedo.TorpedoRangeKm);
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
            Assert.Equal(55m, defenseType.CountermeasureAccuracy);
            Assert.Equal(5m, launcherType.TorpedoManeuverability);
            Assert.Equal(200, launcherType.TorpedoRangeKm);
            Assert.Equal(100, defenseType.CountermeasureRangeKm);
            Assert.Equal(200, defenseType.CountermeasureManualRangeKm);
        }
    }
}
