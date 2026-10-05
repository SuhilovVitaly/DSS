using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

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
    public void Ratings_and_motion_match_agreed_values(bool output)
    {
        var registry = Registry(output);
        var module = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.countermeasure.launcher.basic"));
        Assert.Equal(30m, module.CountermeasureBaseRating);
        Assert.Equal(12, module.CountermeasureSpeedKmS);
        Assert.Equal(90, module.CountermeasureTurnRateDegPerSec);
        Assert.Equal(100, module.CountermeasureRangeKm);
        Assert.Equal(10000, module.CountermeasureReloadMs);
        Assert.Equal(30m, registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.torpedo.launcher.basic")).TorpedoBaseRating);
    }
}
