using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0002TK0003Tests
{
    // Independent original basic definition: adding Tetrarch must not rebalance other ships.
    private const string Basic = """
        {"typeId":"module.engine.basic","displayName":"Engine","type":"module.engine","massKg":5000,
         "structurePointsMax":150,"combatRole":"engine","powerConsumptionW":0,"maxSpeedMps":6000,
         "turnStepDegrees":1,"linearInertiaMps2":400,"angularInertiaDegPerSec":4,"baseCycleTimeMs":1000,
         "fuelCapacityKg":1000,"fuelEfficiencyKmPerKg":200,"baseSuccessChancePercent":100,"maneuverability":10}
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Tetrarch_has_dedicated_engine_and_basic_is_unchanged(bool deployed)
    {
        string source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        string root = deployed ? AppContext.BaseDirectory : source;
        const string canonical = "Data/Modules/Engine/modules-engine.json", candidate = "Data/CombatV3/Modules/Engine/modules-engine.json";
        bool staged = File.Exists(Path.Combine(source, candidate)); string relative = staged ? candidate : canonical;
        var json = JsonNode.Parse(File.ReadAllText(Path.Combine(root, relative)))!;
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Path.Combine(source, relative))), json));
        var rawBasic = Assert.Single(json["moduleImplementations"]!.AsArray(), m => m!["typeId"]!.GetValue<string>() == "module.engine.basic");
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(Basic), rawBasic));
        var categories = EngineContentLoader.LoadModuleCategories(Path.Combine(root, "Data/module-types.json"));
        var modules = EngineContentLoader.LoadModuleImplementations(Path.Combine(root, relative), categories);
        foreach (var module in modules) GameDataRegistry.ValidateWeaponRatings(module);
        var tetrarch = Assert.Single(modules, m => m.TypeId == "module.engine.tetrarch");
        Assert.Equal("module.engine", tetrarch.CategoryTypeId); Assert.Equal("engine", tetrarch.CombatRole);
        Assert.Equal((5000, 600, 20), (tetrarch.MaxSpeedMps, tetrarch.LinearInertiaMps2, tetrarch.AngularInertiaDegPerSec));
        Assert.Equal(5000, tetrarch.MassKg); Assert.Equal(150, tetrarch.StructurePointsMax); Assert.Equal(1, tetrarch.SlotSize);
        var strizh = Assert.Single(modules, m => m.TypeId == "module.engine.strizh");
        Assert.Equal(5000, strizh.MassKg);
        Assert.Equal((8000, 1000, 24), (strizh.MaxSpeedMps, strizh.LinearInertiaMps2, strizh.AngularInertiaDegPerSec));
        Assert.Equal("engine", strizh.CombatRole);
        var basic = Assert.Single(modules, m => m.TypeId == "module.engine.basic");
        Assert.Equal((6000, 400, 4), (basic.MaxSpeedMps, basic.LinearInertiaMps2, basic.AngularInertiaDegPerSec));
        var active = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        Assert.Equal(!staged, active.ModuleTypes.Contains("module.engine.tetrarch"));
        Assert.Equal(staged ? 12 : 24, active.ModuleTypes.GetDefinition(active.ModuleTypes.GetIndex("module.engine.strizh")).AngularInertiaDegPerSec);
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(Path.Combine(root, "Settings.json")); Assert.NotNull(engine.PlayerShipObjectId);
    }
}
