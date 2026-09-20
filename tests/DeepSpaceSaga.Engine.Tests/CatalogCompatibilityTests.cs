using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CatalogCompatibilityTests
{
    private static string SettingsPath => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client", "Settings.json"));

    private static GameDataRegistry RealRegistry() => EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);

    private static GameDataRegistry ChangeCatalog(GameDataRegistry source, Func<ItemTypeDefinition, ItemTypeDefinition> change) =>
        GameDataRegistry.Create(
            Enumerable.Range(0, source.ModuleCategories.Count).Select(source.ModuleCategories.GetDefinition),
            Enumerable.Range(0, source.ModuleTypes.Count).Select(source.ModuleTypes.GetDefinition),
            Enumerable.Range(0, source.ItemTypes.Count).Select(i => change(source.ItemTypes.GetDefinition(i))),
            Enumerable.Range(0, source.CommandDefinitions.Count).Select(source.CommandDefinitions.GetDefinition),
            Enumerable.Range(0, source.FactoryTypes.Count).Select(source.FactoryTypes.GetDefinition),
            Enumerable.Range(0, source.Recipes.Count).Select(source.Recipes.GetDefinition),
            legacyCatalogFingerprint: source.LegacyCatalogFingerprint);

    [Theory]
    [InlineData("Default")]
    [InlineData("Default_500")]
    [InlineData("Docked")]
    [InlineData("Undocked")]
    public void Real_scenarios_and_new_saves_use_compatible_catalog(string scenarioName)
    {
        string path = Path.Combine(Path.GetDirectoryName(SettingsPath)!, "Scenarios", scenarioName, "scenario.json");
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, path);
        var save = engine.CaptureSaveState();
        Assert.Equal(7, save.SaveFormatVersion);
        var registry = RealRegistry();
        Assert.Equal(registry.CatalogCompatibility, save.GameState.CatalogCompatibility);
        Assert.Equal(registry.CatalogCompatibility.Fingerprint, registry.LegacyCatalogFingerprint);
        using var loaded = new SimulationEngine(registry);
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), allowNonZeroGameTime: true));
        Assert.Equal(engine.PlayerCredits, loaded.PlayerCredits);
        Assert.Equal(save.GameState.CatalogCompatibility, loaded.CaptureSaveState().GameState.CatalogCompatibility);
    }

    [Fact]
    public void Rename_is_compatible_but_price_and_mass_changes_are_not()
    {
        var registry = RealRegistry();
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(SettingsPath);
        var save = engine.CaptureSaveState();
        using var renamed = new SimulationEngine(ChangeCatalog(registry, item => item with { DisplayName = "Renamed" }));
        renamed.LoadScenario(save);
        Assert.Equal(engine.PlayerCredits, renamed.PlayerCredits);
        using var repriced = new SimulationEngine(ChangeCatalog(registry, item => item with { BasePriceCredits = item.BasePriceCredits + 1 }));
        Assert.Throws<ScenarioException>(() => repriced.LoadScenario(save));
        using var massChanged = new SimulationEngine(ChangeCatalog(registry, item => item.TypeId == "item.food-rations"
            ? item with { UnitMassKg = 2 } : item));
        Assert.Throws<ScenarioException>(() => massChanged.LoadScenario(save));
    }

    [Fact]
    public void Legacy_save_requires_exact_approved_baseline()
    {
        var registry = RealRegistry();
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(SettingsPath);
        var captured = engine.CaptureSaveState();
        var legacy = captured with { SaveFormatVersion = 6,
            GameState = captured.GameState with { CatalogCompatibility = null } };
        using var compatible = new SimulationEngine(registry);
        compatible.LoadScenario(legacy);
        using var changed = new SimulationEngine(ChangeCatalog(registry, item => item with { BasePriceCredits = item.BasePriceCredits + 1 }));
        Assert.Contains("legacyCatalogFingerprint", Assert.Throws<ScenarioException>(() => changed.LoadScenario(legacy)).Message);
        Assert.Throws<ScenarioException>(() => changed.LoadScenario(legacy with { SaveFormatVersion = 0 }, isSave: true));
    }

    [Fact]
    public void Missing_or_mismatched_identity_does_not_replace_running_world()
    {
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(SettingsPath);
        var save = engine.CaptureSaveState();
        long balance = engine.PlayerCredits;
        foreach (var stamp in new CatalogCompatibilityData?[] { null,
            save.GameState.CatalogCompatibility! with { CatalogVersion = 2 },
            save.GameState.CatalogCompatibility! with { RulesVersion = 2 },
            save.GameState.CatalogCompatibility! with { Fingerprint = "changed" } })
        {
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with {
                GameState = save.GameState with { PlayerTokens = 0, CatalogCompatibility = stamp } }));
            Assert.Equal(balance, engine.PlayerCredits);
            Assert.Equal(save.GameState.PlayerShipObjectId, engine.PlayerShipObjectId);
        }
    }

    [Theory]
    [InlineData("item.unknown", false)]
    [InlineData("item.ice", true)]
    public void Station_inventory_rejects_unknown_or_nontradeable_content(string itemId, bool removePrice)
    {
        var registry = RealRegistry();
        using var original = EngineContentLoader.CreateEngineFromSettingsFile(SettingsPath);
        var save = original.CaptureSaveState();
        if (removePrice) registry = ChangeCatalog(registry, item => item.TypeId == itemId ? item with { BasePriceCredits = null } : item);
        var objects = save.GameState.SpaceObjects.Select(obj => obj.ObjectId == "SPC-0002"
            ? obj with { Inventory = [new StationInventoryItemData(itemId, 1)] } : obj).ToArray();
        using var engine = new SimulationEngine(registry);
        var ex = Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with {
            GameState = save.GameState with { SpaceObjects = objects, CatalogCompatibility = registry.CatalogCompatibility } }));
        Assert.Contains("SPC-0002", ex.Message);
        Assert.Contains(itemId, ex.Message);
    }

    [Theory]
    [InlineData("catalogVersion")]
    [InlineData("rulesVersion")]
    public void Settings_version_mismatch_is_rejected(string field)
    {
        var settings = JsonNode.Parse(File.ReadAllText(SettingsPath))!;
        string root = Path.GetDirectoryName(SettingsPath)!;
        foreach (var pair in settings["typeData"]!.AsObject().ToArray())
            settings["typeData"]![pair.Key] = Path.Combine(root, pair.Value!.GetValue<string>());
        settings["economy"]![field] = 999;
        string temporary = Path.Combine(Path.GetTempPath(), $"dss-settings-version-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(temporary, settings.ToJsonString());
            Assert.Contains(temporary, Assert.Throws<ContentException>(() =>
                EngineContentLoader.LoadRegistryFromSettingsFile(temporary, out _, out _)).Message);
        }
        finally { File.Delete(temporary); }
    }
}
