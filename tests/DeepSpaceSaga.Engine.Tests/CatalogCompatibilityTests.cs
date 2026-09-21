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
            legacyCatalogFingerprint: source.LegacyCatalogFingerprint,
            stationMarketProfiles: Enumerable.Range(0, source.StationMarketProfiles.Count)
                .Select(source.StationMarketProfiles.GetDefinition));

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
        Assert.Equal(8, SaveFormat.CurrentSaveFormatVersion);
        Assert.Equal(SaveFormat.CurrentSaveFormatVersion, save.SaveFormatVersion);
        var registry = RealRegistry();
        Assert.Equal(registry.CatalogCompatibility, save.GameState.CatalogCompatibility);
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
        var baselineItems = new[] { new ItemTypeDefinition("item.legacy", "Legacy", 1, 10) };
        var initial = SyntheticRegistry(baselineItems);
        var registry = SyntheticRegistry(baselineItems, initial.CatalogCompatibility.Fingerprint);
        var legacy = AnonymousLegacySave();
        using var compatible = new SimulationEngine(registry);
        compatible.LoadScenario(legacy);
        using var changed = new SimulationEngine(SyntheticRegistry(
            [baselineItems[0] with { BasePriceCredits = 11 }], registry.LegacyCatalogFingerprint));
        Assert.Contains("legacyCatalogFingerprint", Assert.Throws<ScenarioException>(() => changed.LoadScenario(legacy)).Message);
        Assert.Throws<ScenarioException>(() => changed.LoadScenario(legacy with { SaveFormatVersion = 0 }, isSave: true));
    }

    [Fact]
    public void Added_electronics_rejects_old_catalog_identity_without_rewriting_legacy_baseline()
    {
        var baselineItems = new[] { new ItemTypeDefinition("item.legacy", "Legacy", 1, 10) };
        var initial = SyntheticRegistry(baselineItems);
        string approvedBaseline = initial.CatalogCompatibility.Fingerprint;
        var baseline = SyntheticRegistry(baselineItems, approvedBaseline);
        using var compatible = new SimulationEngine(baseline);
        compatible.LoadScenario(AnonymousLegacySave(), isSave: true);

        var expanded = SyntheticRegistry(
            [.. baselineItems, new ItemTypeDefinition("item.electronics", "Electronics", 1, 150,
                CatalogCode: "ITM-3006", TradeUnit: TradeUnit.Piece)],
            approvedBaseline);
        Assert.Equal(approvedBaseline, expanded.LegacyCatalogFingerprint);
        Assert.NotEqual(approvedBaseline, expanded.CatalogCompatibility.Fingerprint);
        using var incompatible = new SimulationEngine(expanded);
        Assert.Contains("legacyCatalogFingerprint",
            Assert.Throws<ScenarioException>(() => incompatible.LoadScenario(AnonymousLegacySave(), isSave: true)).Message);
    }

    private static GameDataRegistry SyntheticRegistry(
        IEnumerable<ItemTypeDefinition> items,
        string? legacyCatalogFingerprint = null) =>
        GameDataRegistry.Create([], [], items, [], legacyCatalogFingerprint: legacyCatalogFingerprint);

    private static ScenarioFile AnonymousLegacySave() => new(
        new ScenarioMetadata("legacy", "Legacy"),
        new GameStateData(
            GameTimeMs: 0,
            CurrentSpeed: "Speed0",
            PlayerShipObjectId: "SHIP",
            Focus: null,
            SpaceObjects:
            [
                new SpaceObjectData("SHIP", "PlayerShip", "Permanent", null, 0, 0, 0, 0, "Stationary", null, null, null),
                new SpaceObjectData("STATION", "Station", "Permanent", null, 1, 0, 0, 0, "Stationary", null, null, null,
                    Credits: 100, Inventory: [new StationInventoryItemData("item.legacy", 10)]),
            ],
            MasterSeed: 1,
            PlayerTokens: 0,
            EconomyTime: new EconomyTimeData(),
            SimulationTimeMs: 0,
            CatalogCompatibility: null),
        SaveFormatVersion: 6);

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
    [InlineData("item.uranium-ore", true)]
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
