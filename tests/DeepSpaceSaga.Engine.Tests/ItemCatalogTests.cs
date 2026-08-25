using System.Linq;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Covers U1 (item-catalog TradeCategory/CatalogCode content + loader parsing) and U2 (Module
/// BasePriceCredits data shape) of story-20260825-084409 Batch 1 — requirements §59,
/// Docs\FirstRelease\TechnicalTasks\StationEconomyProductionAndSizing.md "Номенклатура" /
/// "Формула цены" / "Схема данных".
/// </summary>
public class ItemCatalogTests
{
    // --- EngineContentLoader.LoadItemTypes parsing (isolated JSON, not the real catalog) -----

    [Fact]
    public void LoadItemTypes_parses_tradeCategory_and_catalogCode()
    {
        string json = """
        {
          "itemTypes": [
            { "typeId": "item.test-resource", "displayName": "Test Resource", "unitMassKg": 1,
              "basePriceCredits": 10, "tradeCategory": "Resource", "catalogCode": "RES-9001" },
            { "typeId": "item.test-good", "displayName": "Test Good", "unitMassKg": 1,
              "basePriceCredits": 20, "tradeCategory": "Good", "catalogCode": "ITM-9001" }
          ]
        }
        """;

        var path = WriteTempFile(json);
        try
        {
            var items = EngineContentLoader.LoadItemTypes(path);

            var resource = items.Single(i => i.TypeId == "item.test-resource");
            Assert.Equal(TradeCategory.Resource, resource.Category);
            Assert.Equal("RES-9001", resource.CatalogCode);
            Assert.Equal(10, resource.BasePriceCredits);
            Assert.Equal(1, resource.UnitMassKg);

            var good = items.Single(i => i.TypeId == "item.test-good");
            Assert.Equal(TradeCategory.Good, good.Category);
            Assert.Equal("ITM-9001", good.CatalogCode);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadItemTypes_missing_tradeCategory_defaults_to_good()
    {
        string json = """
        { "itemTypes": [ { "typeId": "item.legacy", "displayName": "Legacy", "unitMassKg": 10 } ] }
        """;

        var path = WriteTempFile(json);
        try
        {
            var item = Assert.Single(EngineContentLoader.LoadItemTypes(path));
            Assert.Equal(TradeCategory.Good, item.Category);
            Assert.Null(item.CatalogCode);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadItemTypes_unknown_tradeCategory_throws()
    {
        string json = """
        { "itemTypes": [ { "typeId": "item.bad", "displayName": "Bad", "unitMassKg": 1,
          "tradeCategory": "Module" } ] }
        """;

        var path = WriteTempFile(json);
        try
        {
            Assert.Throws<ContentException>(() => EngineContentLoader.LoadItemTypes(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteTempFile(string json)
    {
        string path = Path.Combine(Path.GetTempPath(), $"dss-itemtypes-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }

    // --- EngineContentLoader.LoadItemTypes directory mode (mirrors LoadModuleImplementations'
    // directory dual-mode: Data/Items/<category>/items-<category>.json) --------------------------

    [Fact]
    public void LoadItemTypes_merges_all_json_files_in_directory()
    {
        string directory = CreateTempDirectory();
        try
        {
            WriteFile(directory, "resource.json", """
            {
              "itemTypes": [
                { "typeId": "item.test-resource", "displayName": "Test Resource", "unitMassKg": 1,
                  "basePriceCredits": 10, "tradeCategory": "Resource", "catalogCode": "RES-9001" }
              ]
            }
            """);
            WriteFile(directory, "good.json", """
            {
              "itemTypes": [
                { "typeId": "item.test-good", "displayName": "Test Good", "unitMassKg": 1,
                  "basePriceCredits": 20, "tradeCategory": "Good", "catalogCode": "ITM-9001" }
              ]
            }
            """);

            var items = EngineContentLoader.LoadItemTypes(directory);

            Assert.Equal(2, items.Count);
            Assert.Contains(items, i => i.TypeId == "item.test-resource" && i.Category == TradeCategory.Resource);
            Assert.Contains(items, i => i.TypeId == "item.test-good" && i.Category == TradeCategory.Good);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadItemTypes_rejects_empty_directory_with_no_json_files()
    {
        string directory = CreateTempDirectory();
        try
        {
            var ex = Assert.Throws<ContentException>(() => EngineContentLoader.LoadItemTypes(directory));
            Assert.Contains(directory, ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadItemTypes_rejects_path_that_is_neither_file_nor_directory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"dss-itemtypes-missing-{Guid.NewGuid():N}");

        var ex = Assert.Throws<ContentException>(() => EngineContentLoader.LoadItemTypes(path));
        Assert.Contains(path, ex.Message, StringComparison.Ordinal);
    }

    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dss-itemtypes-dir-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteFile(string directory, string fileName, string json) =>
        File.WriteAllText(Path.Combine(directory, fileName), json);

    // --- Real Data/Items catalog content (§59 acceptance criteria) ---------------------------

    private static GameDataRegistry LoadRealRegistry()
    {
        string settingsPath = ResolveRealSettingsPath();
        return EngineContentLoader.LoadRegistryFromSettingsFile(settingsPath, out _, out _);
    }

    private static string ResolveRealSettingsPath()
    {
        string settingsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DeepSpaceSaga.Client", "Settings.json"));

        if (!File.Exists(settingsPath))
        {
            settingsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Settings.json"));
        }

        return settingsPath;
    }

    [Fact]
    public void Real_catalog_has_exactly_ten_tradeable_items()
    {
        var registry = LoadRealRegistry();
        Assert.Equal(10, registry.ItemTypes.Count);
    }

    // isResource (bool) rather than TradeCategory (internal enum) — xUnit requires [Theory]
    // methods to be public, and a public method cannot expose an internal-visibility parameter
    // type (CS0051).
    [Theory]
    [InlineData("item.ice", "Ice", true, "RES-2001", 10)]
    [InlineData("item.iron-ore", "Iron Ore", true, "RES-2002", 5)]
    [InlineData("item.silicon", "Silicon", true, "RES-2003", 40)]
    [InlineData("item.magnesium-ore", "Magnesium Ore", true, "RES-2004", 30)]
    [InlineData("item.water", "Water", false, "ITM-3001", 14)]
    [InlineData("item.steel", "Steel", false, "ITM-3002", 40)]
    [InlineData("item.energy-cells", "Energy Cells", false, "ITM-3003", 50)]
    [InlineData("item.fuel", "Fuel", false, "ITM-3004", 10)]
    [InlineData("item.protein-mass", "Protein mass", false, "ITM-3005", 110)]
    public void Real_catalog_item_matches_documented_price_category_and_catalog_code(
        string typeId, string displayName, bool isResource, string catalogCode, long basePrice)
    {
        var registry = LoadRealRegistry();
        var item = registry.ItemTypes.GetDefinition(registry.ItemTypes.GetIndex(typeId));

        Assert.Equal(displayName, item.DisplayName);
        Assert.Equal(isResource ? TradeCategory.Resource : TradeCategory.Good, item.Category);
        Assert.Equal(catalogCode, item.CatalogCode);
        Assert.Equal(basePrice, item.BasePriceCredits);
        // CP-1 (story-20260825-084409): UnitMassKg = 1 for every tradeable Resource/Good —
        // Quantity in a trade command is literally kg.
        Assert.Equal(1, item.UnitMassKg);
    }

    [Fact]
    public void Real_catalog_food_rations_has_no_catalog_code_but_documented_price()
    {
        // §59 leaves Food Rations' spec id unassigned; story-20260825-084409 decision 1 fixes
        // its BasePriceCredits at 20 without inventing a spec id.
        var registry = LoadRealRegistry();
        var item = registry.ItemTypes.GetDefinition(registry.ItemTypes.GetIndex("item.food-rations"));

        Assert.Equal("Food Rations", item.DisplayName);
        Assert.Equal(TradeCategory.Good, item.Category);
        Assert.Null(item.CatalogCode);
        Assert.Equal(20, item.BasePriceCredits);
        Assert.Equal(1, item.UnitMassKg);
    }

    // --- ModuleTypeDefinition.BasePriceCredits data shape (U2) --------------------------------

    [Fact]
    public void ModuleTypeDefinition_BasePriceCredits_defaults_to_null()
    {
        var moduleType = new ModuleTypeDefinition(
            "module.test", "Test Module", SlotSize: 1, MassKg: 100,
            StructurePointsMax: 10, PowerConsumptionW: 0,
            CommandTypeIds: System.Collections.Immutable.ImmutableArray<string>.Empty);

        Assert.Null(moduleType.BasePriceCredits);
    }

    [Fact]
    public void Real_module_catalog_has_no_basePriceCredits_populated_yet()
    {
        // Story-20260825-084409 decision 2: Module buy/sell is out of scope for Batch 1 — the
        // data shape exists (this field) but no module-types content populates it yet.
        var registry = LoadRealRegistry();

        for (int i = 0; i < registry.ModuleTypes.Count; i++)
        {
            Assert.Null(registry.ModuleTypes.GetDefinition(i).BasePriceCredits);
        }
    }
}
