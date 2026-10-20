using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0002TK0002Tests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Countermissile_catalog_matches_v3_in_source_and_deployed_component(bool deployed)
    {
        string source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        string root = deployed ? AppContext.BaseDirectory : source;
        const string canonical = "Data/Modules/CountermeasureLauncher/modules-countermeasurelauncher.json";
        const string candidate = "Data/CombatV3/Modules/CountermeasureLauncher/modules-countermeasurelauncher.json";
        bool staged = File.Exists(Path.Combine(source, candidate)); string relative = staged ? candidate : canonical;
        var categories = EngineContentLoader.LoadModuleCategories(Path.Combine(root, "Data/module-types.json"));
        var modules = EngineContentLoader.LoadModuleImplementations(Path.Combine(root, relative), categories);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Path.Combine(source, relative))),
            JsonNode.Parse(File.ReadAllText(Path.Combine(root, relative)))));
        var module = Assert.Single(modules, m => m.TypeId == "module.countermeasure.launcher.basic");
        GameDataRegistry.ValidateWeaponRatings(module);
        Assert.Equal(30, module.RangeKm); Assert.Equal(12500, module.ReloadMs); Assert.Equal(.08, module.ScatterPercent);
        Assert.Equal(new[] { "wide", "fast" }, module.Profiles.Select(p => p.ProfileId));
        Assert.Equal(new[] { 15d, 25d }, module.Profiles.Select(p => p.SpeedKmS));
        Assert.Equal(new[] { 1d, .5 }, module.Profiles.Select(p => p.BlastRadiusKm));
        Assert.Null(module.TorpedoDamage); Assert.Null(module.ArmorPiercingPercent);
        Assert.Null(module.CountermeasureAccuracy); Assert.Null(module.CountermeasureRangeKm); Assert.Null(module.CountermeasureManualRangeKm);
        Assert.Null(module.CountermeasureSpeedKmS); Assert.Null(module.CountermeasureTurnRateDegPerSec); Assert.Null(module.CountermeasureMaxFlightTimeMs);
        Assert.Null(module.CountermeasureReloadMs); Assert.Null(module.Barrels); Assert.Null(module.BurstIntervalMs);
        Assert.Equal(2000, module.MassKg); Assert.Equal(150, module.StructurePointsMax); Assert.Equal(0, module.PowerConsumptionW);
        Assert.Equal(1, module.BaseCycleTimeMs); Assert.Equal(100, module.BaseSuccessChancePercent);
        var active = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        Assert.Equal(!staged, active.ModuleTypes.GetDefinition(active.ModuleTypes.GetIndex(module.TypeId)).IsPointLauncher);
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(Path.Combine(root, "Settings.json"));
        Assert.NotNull(engine.PlayerShipObjectId);
    }
}
