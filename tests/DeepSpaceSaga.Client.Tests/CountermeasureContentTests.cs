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
    public void Retired_single_launcher_is_absent(bool output)
    {
        var registry = Registry(output);
        var basic = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.countermeasure.launcher.basic"));
        Assert.True(basic.IsPointLauncher);
        Assert.False(registry.ModuleTypes.Contains("module.countermeasure.launcher.single"));
    }
}
