using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class StationMarketCatalogContentTests
{
    private const string LegacyCatalogFingerprint =
        "0E0C8DCBDBB06051AC1D7DBA5321DA8DFD0A2CA29C7980AAA4F56486FA300555";

    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static readonly string ClientRoot = Path.Combine(RepoRoot, "src", "DeepSpaceSaga.Client");
    private static readonly string SettingsPath = Path.Combine(ClientRoot, "Settings.json");

    private static readonly string[] MvpItemIds =
    [
        "item.ice",
        "item.iron-ore",
        "item.silicon",
        "item.magnesium-ore",
        "item.carbon-ore",
        "item.water",
        "item.steel",
        "item.energy-cells",
        "item.fuel",
        "item.protein-mass",
        "item.food-rations",
        "item.electronics",
    ];

    private static readonly IReadOnlyDictionary<string, string> EnglishNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Trade.ItemIce"] = "Cryo-Ice",
            ["Trade.ItemIronOre"] = "Asteroid Iron Ore",
            ["Trade.ItemSilicon"] = "Electronic Silicon",
            ["Trade.ItemMagnesiumOre"] = "Magnesium Concentrate",
            ["Trade.ItemCarbonOre"] = "Carbon Regolith",
            ["Trade.ItemWater"] = "Purified Water",
            ["Trade.ItemSteel"] = "Hull Alloy",
            ["Trade.ItemEnergyCells"] = "Energy Cells",
            ["Trade.ItemFuel"] = "Cryogenic Fuel",
            ["Trade.ItemProteinMass"] = "Synthetic Protein Mass",
            ["Trade.ItemFoodRations"] = "Sealed Rations",
            ["Trade.ItemElectronics"] = "Control Electronics",
        };

    private static readonly IReadOnlyDictionary<string, string> CatalogNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["item.ice"] = "Cryo-Ice",
            ["item.iron-ore"] = "Asteroid Iron Ore",
            ["item.silicon"] = "Electronic Silicon",
            ["item.magnesium-ore"] = "Magnesium Concentrate",
            ["item.carbon-ore"] = "Carbon Regolith",
            ["item.water"] = "Purified Water",
            ["item.steel"] = "Hull Alloy",
            ["item.energy-cells"] = "Energy Cells",
            ["item.fuel"] = "Cryogenic Fuel",
            ["item.protein-mass"] = "Synthetic Protein Mass",
            ["item.food-rations"] = "Sealed Rations",
            ["item.electronics"] = "Control Electronics",
        };

    private static readonly IReadOnlyDictionary<string, string> RussianNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Trade.ItemIce"] = "Криолёд",
            ["Trade.ItemIronOre"] = "Астероидная железная руда",
            ["Trade.ItemSilicon"] = "Электронный кремний",
            ["Trade.ItemMagnesiumOre"] = "Магниевый концентрат",
            ["Trade.ItemCarbonOre"] = "Углеродный реголит",
            ["Trade.ItemWater"] = "Очищенная вода",
            ["Trade.ItemSteel"] = "Корпусной сплав",
            ["Trade.ItemEnergyCells"] = "Энергетические ячейки",
            ["Trade.ItemFuel"] = "Криогенное топливо",
            ["Trade.ItemProteinMass"] = "Синтетическая белковая масса",
            ["Trade.ItemFoodRations"] = "Герметичные пайки",
            ["Trade.ItemElectronics"] = "Управляющая электроника",
        };

    [Fact]
    public void Mvp_catalog_has_twelve_required_items_plus_legacy_uranium()
    {
        var registry = LoadRegistry();
        var items = AllItems(registry);
        string[] expectedIds = [.. MvpItemIds, "item.uranium-ore"];

        Assert.Equal(expectedIds.Order(StringComparer.Ordinal),
            items.Select(item => item.TypeId).Order(StringComparer.Ordinal));
        Assert.Equal(13, items.Length);
        Assert.Equal(5, items.Count(item => item.TypeId != "item.uranium-ore" &&
            item.Category == TradeCategory.Resource));
        Assert.Equal(7, items.Count(item => item.Category == TradeCategory.Good));
        Assert.Equal(1, registry.CatalogVersion);
        foreach (var entry in CatalogNames) Assert.Equal(entry.Value, GetItem(registry, entry.Key).DisplayName);

        string goodsPath = Path.Combine(ClientRoot, "Data", "Items", "Good", "items-good.json");
        using (var goodsDocument = JsonDocument.Parse(File.ReadAllText(goodsPath)))
        {
            AssertNoDuplicateProperties(goodsDocument.RootElement, goodsPath);
            Assert.Equal(2, goodsDocument.RootElement.GetProperty("schemaVersion").GetInt32());
            Assert.Equal(1, goodsDocument.RootElement.GetProperty("catalogVersion").GetInt32());
            Assert.Equal(new[]
            {
                "item.water",
                "item.steel",
                "item.energy-cells",
                "item.fuel",
                "item.protein-mass",
                "item.food-rations",
                "item.electronics",
            }, goodsDocument.RootElement.GetProperty("itemTypes").EnumerateArray()
                .Select(item => item.GetProperty("typeId").GetString()));
        }

        foreach (string scenarioDirectory in Directory.GetDirectories(Path.Combine(ClientRoot, "Scenarios")))
        {
            string scenarioPath = Path.Combine(scenarioDirectory, "scenario.json");
            if (!File.Exists(scenarioPath)) continue;
            using var engine = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, scenarioPath);
            Assert.NotNull(engine.CaptureSaveState().GameState.CatalogCompatibility);
        }
    }

    [Fact]
    public void Electronics_matches_approved_identity_mass_price_and_storage()
    {
        var registry = LoadRegistry();
        var electronics = GetItem(registry, "item.electronics");

        Assert.Equal("Control Electronics", electronics.DisplayName);
        Assert.Equal(1, electronics.UnitMassKg);
        Assert.Equal(150, electronics.BasePriceCredits);
        Assert.Equal(TradeCategory.Good, electronics.Category);
        Assert.Equal("ITM-3006", electronics.CatalogCode);
        Assert.Equal(TradeUnit.Piece, electronics.TradeUnit);
        Assert.Equal(ItemStorageKind.Cargo, electronics.StorageKind);
        Assert.Equal(1, electronics.BuyQuantityStep);
        Assert.Equal(1, electronics.SellQuantityStep);
    }

    [Fact]
    public void Existing_economic_tuples_and_legacy_references_are_unchanged()
    {
        var registry = LoadRegistry();
        ExpectedItem[] expected =
        [
            new("item.ice", 10, TradeCategory.Resource, "RES-2001", TradeUnit.Kilogram, ItemStorageKind.Cargo),
            new("item.iron-ore", 5, TradeCategory.Resource, "RES-2002", TradeUnit.Kilogram, ItemStorageKind.Cargo),
            new("item.silicon", 40, TradeCategory.Resource, "RES-2003", TradeUnit.Kilogram, ItemStorageKind.Cargo),
            new("item.magnesium-ore", 30, TradeCategory.Resource, "RES-2004", TradeUnit.Kilogram, ItemStorageKind.Cargo),
            new("item.uranium-ore", 30, TradeCategory.Resource, "RES-2005", TradeUnit.Kilogram, ItemStorageKind.Cargo),
            new("item.carbon-ore", 30, TradeCategory.Resource, "RES-2006", TradeUnit.Kilogram, ItemStorageKind.Cargo),
            new("item.water", 14, TradeCategory.Good, "ITM-3001", TradeUnit.Kilogram, ItemStorageKind.Cargo),
            new("item.steel", 40, TradeCategory.Good, "ITM-3002", TradeUnit.Kilogram, ItemStorageKind.Cargo),
            new("item.energy-cells", 50, TradeCategory.Good, "ITM-3003", TradeUnit.EnergyCell, ItemStorageKind.Cargo),
            new("item.fuel", 10, TradeCategory.Good, "ITM-3004", TradeUnit.Kilogram, ItemStorageKind.FuelTank),
            new("item.protein-mass", 110, TradeCategory.Good, "ITM-3005", TradeUnit.Kilogram, ItemStorageKind.Cargo),
            new("item.food-rations", 20, TradeCategory.Good, null, TradeUnit.Ration, ItemStorageKind.Cargo),
        ];

        foreach (var baseline in expected)
        {
            var item = GetItem(registry, baseline.TypeId);
            Assert.Equal(1, item.UnitMassKg);
            Assert.Equal(baseline.Price, item.BasePriceCredits);
            Assert.Equal(baseline.Category, item.Category);
            Assert.Equal(baseline.CatalogCode, item.CatalogCode);
            Assert.Equal(baseline.TradeUnit, item.TradeUnit);
            Assert.Equal(baseline.StorageKind, item.StorageKind);
            Assert.Equal(1, item.BuyQuantityStep);
            Assert.Equal(1, item.SellQuantityStep);
        }

        Assert.Equal("Uranium Ore", GetItem(registry, "item.uranium-ore").DisplayName);
        string dockedScenario = File.ReadAllText(Path.Combine(ClientRoot, "Scenarios", "Docked", "scenario.json"));
        Assert.Contains("item.uranium-ore", dockedScenario, StringComparison.Ordinal);
    }

    [Fact]
    public void Both_locales_have_matching_scifi_names_and_unit_format_arguments()
    {
        var english = LoadLocale("English.json");
        var russian = LoadLocale("Russian.json");
        Assert.Equal(english.Keys.Order(StringComparer.Ordinal), russian.Keys.Order(StringComparer.Ordinal));

        AssertEntries(english, EnglishNames);
        AssertEntries(russian, RussianNames);
        Assert.Equal("Control blocks for station systems and ship equipment.",
            english["Trade.DescriptionElectronics"]);
        Assert.Equal("Блоки управления станционными системами и корабельным оборудованием.",
            russian["Trade.DescriptionElectronics"]);

        AssertEntries(english, new Dictionary<string, string>
        {
            ["Trade.UnitKg"] = "kg",
            ["Trade.UnitRation"] = "rations",
            ["Trade.UnitCell"] = "cells",
            ["Trade.UnitBlock"] = "blocks",
            ["Trade.UnitGeneric"] = "units",
            ["Trade.UnitRationSingle"] = "ration",
            ["Trade.UnitCellSingle"] = "cell",
            ["Trade.UnitBlockSingle"] = "block",
            ["Trade.UnitGenericSingle"] = "unit",
            ["Trade.QuantityWithUnit"] = "Quantity, {0}",
            ["Trade.AmountWithUnit"] = "{0} {1}",
            ["Trade.QuantityMass"] = "1 {0} = {1} kg",
            ["Trade.ConfirmBuyWithUnit"] = "Buy {0} {1} for {2}",
            ["Trade.ConfirmSellWithUnit"] = "Sell {0} {1} for {2}",
        });
        AssertEntries(russian, new Dictionary<string, string>
        {
            ["Trade.UnitKg"] = "кг",
            ["Trade.UnitRation"] = "рацион.",
            ["Trade.UnitCell"] = "ячеек",
            ["Trade.UnitBlock"] = "блоков",
            ["Trade.UnitGeneric"] = "ед.",
            ["Trade.UnitRationSingle"] = "рацион",
            ["Trade.UnitCellSingle"] = "ячейка",
            ["Trade.UnitBlockSingle"] = "блок",
            ["Trade.UnitGenericSingle"] = "ед.",
            ["Trade.QuantityWithUnit"] = "Количество, {0}",
            ["Trade.AmountWithUnit"] = "{0} {1}",
            ["Trade.QuantityMass"] = "1 {0} = {1} кг",
            ["Trade.ConfirmBuyWithUnit"] = "Купить {0} {1} за {2}",
            ["Trade.ConfirmSellWithUnit"] = "Продать {0} {1} за {2}",
        });

        foreach (var locale in new[] { english, russian })
        {
            _ = string.Format(CultureInfo.InvariantCulture, locale["Trade.QuantityWithUnit"], locale["Trade.UnitBlock"]);
            _ = string.Format(CultureInfo.InvariantCulture, locale["Trade.AmountWithUnit"], 2, locale["Trade.UnitBlock"]);
            _ = string.Format(CultureInfo.InvariantCulture, locale["Trade.QuantityMass"],
                locale["Trade.UnitBlockSingle"], 1);
            _ = string.Format(CultureInfo.InvariantCulture, locale["Trade.ConfirmBuyWithUnit"],
                2, locale["Trade.UnitBlock"], 300);
            _ = string.Format(CultureInfo.InvariantCulture, locale["Trade.ConfirmSellWithUnit"],
                2, locale["Trade.UnitBlock"], 300);
        }
    }

    [Fact]
    public void Catalog_addition_changes_identity_without_reapproving_legacy()
    {
        var registry = LoadRegistry();
        Assert.Equal(LegacyCatalogFingerprint, registry.LegacyCatalogFingerprint);
        Assert.NotEqual(LegacyCatalogFingerprint, registry.CatalogCompatibility.Fingerprint);

        string scenarioPath = Path.Combine(ClientRoot, "Scenarios", "Default", "scenario.json");
        using var original = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, scenarioPath);
        var save = original.CaptureSaveState();

        using var roundTripped = new SimulationEngine(registry);
        roundTripped.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), allowNonZeroGameTime: true));
        Assert.Equal(save.GameState.CatalogCompatibility,
            roundTripped.CaptureSaveState().GameState.CatalogCompatibility);

        var oldStamp = save with
        {
            GameState = save.GameState with
            {
                CatalogCompatibility = save.GameState.CatalogCompatibility! with
                {
                    Fingerprint = LegacyCatalogFingerprint,
                },
            },
        };
        using var incompatible = new SimulationEngine(registry);
        Assert.Throws<ScenarioException>(() => incompatible.LoadScenario(oldStamp, isSave: true));
    }

    [Fact]
    public void Shipped_catalog_and_locales_are_available_in_output()
    {
        string[] relativePaths =
        [
            Path.Combine("Data", "Items", "Good", "items-good.json"),
            Path.Combine("Data", "Items", "Resource", "items-resource.json"),
            Path.Combine("Data", "Locale", "English.json"),
            Path.Combine("Data", "Locale", "Russian.json"),
        ];

        foreach (string relativePath in relativePaths)
        {
            string sourcePath = Path.Combine(ClientRoot, relativePath);
            string outputPath = Path.Combine(AppContext.BaseDirectory, relativePath);
            Assert.True(File.Exists(outputPath), $"Missing shipped content: {outputPath}");
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(sourcePath)),
                JsonNode.Parse(File.ReadAllText(outputPath))), $"Shipped content differs: {relativePath}");
        }
    }

    private static GameDataRegistry LoadRegistry() =>
        EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);

    private static ItemTypeDefinition[] AllItems(GameDataRegistry registry) =>
        Enumerable.Range(0, registry.ItemTypes.Count).Select(registry.ItemTypes.GetDefinition).ToArray();

    private static ItemTypeDefinition GetItem(GameDataRegistry registry, string typeId) =>
        registry.ItemTypes.GetDefinition(registry.ItemTypes.GetIndex(typeId));

    private static Dictionary<string, string> LoadLocale(string fileName)
    {
        string path = Path.Combine(ClientRoot, "Data", "Locale", fileName);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        AssertNoDuplicateProperties(document.RootElement, path);
        return document.RootElement.EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value.GetString()!,
            StringComparer.Ordinal);
    }

    private static void AssertNoDuplicateProperties(JsonElement element, string path)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                Assert.True(names.Add(property.Name), $"Duplicate JSON key '{property.Name}' in {path}.");
                AssertNoDuplicateProperties(property.Value, path);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray()) AssertNoDuplicateProperties(child, path);
        }
    }

    private static void AssertEntries(IReadOnlyDictionary<string, string> actual,
        IReadOnlyDictionary<string, string> expected)
    {
        foreach (var entry in expected) Assert.Equal(entry.Value, actual[entry.Key]);
    }

    private sealed record ExpectedItem(
        string TypeId,
        long Price,
        TradeCategory Category,
        string? CatalogCode,
        TradeUnit TradeUnit,
        ItemStorageKind StorageKind);
}
