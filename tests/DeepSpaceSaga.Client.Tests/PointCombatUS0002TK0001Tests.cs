using DeepSpaceSaga.Engine.Content;
using System.Text.Json.Nodes;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0002TK0001Tests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Torpedo_catalog_matches_v3(bool deployed)
    {
        string source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        string root = deployed ? AppContext.BaseDirectory : source;
        const string canonical = "Data/Modules/TorpedoLauncher/modules-torpedolauncher.json";
        const string candidate = "Data/CombatV3/Modules/TorpedoLauncher/modules-torpedolauncher.json";
        bool staged = File.Exists(Path.Combine(source, candidate));
        string relative = staged ? candidate : canonical;
        var categories = EngineContentLoader.LoadModuleCategories(Path.Combine(root, "Data/module-types.json"));
        var modules = EngineContentLoader.LoadModuleImplementations(Path.Combine(root, relative), categories);
        Assert.Equal(2, modules.Count);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Path.Combine(source, relative))),
            JsonNode.Parse(File.ReadAllText(Path.Combine(root, relative)))));
        foreach (var (id, reload) in new[] { ("module.torpedo.launcher.basic", 10000L), ("module.torpedo.launcher.light", 7250L) })
        {
            var module = Assert.Single(modules, m => m.TypeId == id);
            GameDataRegistry.ValidateWeaponRatings(module);
            Assert.Equal(70d, module.RangeKm);
            Assert.Equal(reload, module.ReloadMs);
            Assert.Equal(200, module.TorpedoDamage);
            Assert.Equal(20m, module.ArmorPiercingPercent);
            Assert.Equal(100m, module.ArmorDamagePercent);
            Assert.Equal(0.02, module.ScatterPercent);
            Assert.Equal(new[] { "slow", "medium", "fast" }, module.Profiles.Select(p => p.ProfileId));
            Assert.Equal(new[] { 6d, 10d, 16d }, module.Profiles.Select(p => p.SpeedKmS));
            Assert.Equal(new[] { 9d, 5d, 2.5 }, module.Profiles.Select(p => p.BlastRadiusKm));
            Assert.Null(module.TorpedoRangeKm);
            Assert.Null(module.TorpedoSpeedKmS);
            Assert.Null(module.TorpedoTurnRateDegPerSec);
            Assert.Null(module.TorpedoManeuverability);
            Assert.Null(module.LauncherReloadMs);
            Assert.Null(module.Accuracy);
            Assert.Null(module.TorpedoMaxFlightTimeMs);
        }
        var active = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        var current = active.ModuleTypes.GetDefinition(active.ModuleTypes.GetIndex("module.torpedo.launcher.basic"));
        Assert.Equal(!staged, current.IsPointLauncher); // Default activation is deliberately owned by US2-TK5.
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(Path.Combine(root, "Settings.json"));
        Assert.NotNull(engine.PlayerShipObjectId);
    }
}
