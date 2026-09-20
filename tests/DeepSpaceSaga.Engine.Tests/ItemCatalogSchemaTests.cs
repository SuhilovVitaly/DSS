using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ItemCatalogSchemaTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-catalog-schema-{Guid.NewGuid():N}");

    public ItemCatalogSchemaTests() => Directory.CreateDirectory(_directory);
    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private static JsonObject Document() => JsonNode.Parse("""
        { "schemaVersion": 2, "catalogVersion": 1, "itemTypes": [
          { "typeId": "item.test", "displayName": "Test", "unitMassKg": 5,
            "basePriceCredits": 20, "tradeCategory": "Good", "catalogCode": "ITM-9001",
            "tradeUnit": "Piece", "storageKind": "Cargo", "buyQuantityStep": 1, "sellQuantityStep": 1 }
        ] }
        """)!.AsObject();

    private string Write(JsonObject document, string name = "items.json")
    {
        string path = Path.Combine(_directory, name);
        File.WriteAllText(path, document.ToJsonString());
        return path;
    }

    private static JsonObject Item(JsonObject document) => document["itemTypes"]![0]!.AsObject();

    [Fact]
    public void Version_two_round_trips_explicit_metadata_and_keeps_name_out_of_identity()
    {
        var document = Document();
        var first = Assert.Single(EngineContentLoader.LoadItemTypes(Write(document)));
        Assert.Equal(TradeUnit.Piece, first.TradeUnit);
        Assert.Equal(ItemStorageKind.Cargo, first.StorageKind);
        Assert.Equal(5, first.UnitMassKg);
        Assert.Equal(1, first.BuyQuantityStep);
        Assert.Equal(1, first.SellQuantityStep);
        Item(document)["displayName"] = "Localized name";
        var second = Assert.Single(EngineContentLoader.LoadItemTypes(Write(document)));
        Assert.Equal(first.TypeId, second.TypeId);
        Assert.Equal(first.CatalogCode, second.CatalogCode);
        Assert.Equal(Registry(first).CatalogCompatibility, Registry(second).CatalogCompatibility);
    }

    [Theory]
    [InlineData("tradeCategory")]
    [InlineData("tradeUnit")]
    [InlineData("storageKind")]
    [InlineData("buyQuantityStep")]
    [InlineData("sellQuantityStep")]
    public void Version_two_requires_explicit_metadata(string field)
    {
        var document = Document();
        Item(document).Remove(field);
        var path = Write(document);
        var ex = Assert.Throws<ContentException>(() => EngineContentLoader.LoadItemTypes(path));
        Assert.Contains(path, ex.Message);
        Assert.Contains("item.test", ex.Message);
        Assert.Contains(field, ex.Message);
    }

    [Theory]
    [InlineData("typeId", "\" \"")]
    [InlineData("catalogCode", "\"\"")]
    [InlineData("displayName", "null")]
    [InlineData("tradeCategory", "\"Module\"")]
    [InlineData("tradeCategory", "\"0\"")]
    [InlineData("tradeUnit", "\"Unknown\"")]
    [InlineData("tradeUnit", "\"1\"")]
    [InlineData("storageKind", "\"0\"")]
    [InlineData("storageKind", "\"FuelTank\"")]
    [InlineData("unitMassKg", "0")]
    [InlineData("unitMassKg", "-1")]
    [InlineData("unitMassKg", "0.5")]
    [InlineData("unitMassKg", "9223372036854775808")]
    [InlineData("basePriceCredits", "0")]
    [InlineData("basePriceCredits", "-1")]
    [InlineData("buyQuantityStep", "0")]
    [InlineData("sellQuantityStep", "-1")]
    [InlineData("sellQuantityStep", "10")]
    public void Invalid_item_fields_have_file_and_field_diagnostics(string field, string value)
    {
        var document = Document();
        Item(document)[field] = JsonNode.Parse(value);
        var path = Write(document);
        var ex = Assert.Throws<ContentException>(() => EngineContentLoader.LoadItemTypes(path));
        Assert.Contains(path, ex.Message);
        Assert.Contains(field, ex.Message);
    }

    [Fact]
    public void Nontradeable_content_may_have_no_price()
    {
        var document = Document();
        Item(document).Remove("basePriceCredits");
        Assert.Null(Assert.Single(EngineContentLoader.LoadItemTypes(Write(document))).BasePriceCredits);
    }

    [Fact]
    public void Version_two_requires_catalog_version()
    {
        var document = Document(); document.Remove("catalogVersion");
        Assert.Contains("catalogVersion", Assert.Throws<ContentException>(() => EngineContentLoader.LoadItemTypes(Write(document))).Message);
    }

    [Theory]
    [InlineData("item.food-rations")]
    [InlineData("item.energy-cells")]
    public void Consumable_unit_cannot_be_reinterpreted(string typeId)
    {
        var document = Document(); Item(document)["typeId"] = typeId;
        Assert.Contains("tradeUnit", Assert.Throws<ContentException>(() => EngineContentLoader.LoadItemTypes(Write(document))).Message);
    }

    [Theory]
    [InlineData("schemaVersion", 0)]
    [InlineData("schemaVersion", 3)]
    [InlineData("catalogVersion", 0)]
    [InlineData("catalogVersion", 2)]
    public void Unsupported_versions_fail_without_fallback(string field, int version)
    {
        var document = Document(); document[field] = version;
        var path = Write(document);
        var ex = Assert.Throws<ContentException>(() => EngineContentLoader.LoadItemTypes(path));
        Assert.Contains(path, ex.Message);
        Assert.Contains(field, ex.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Duplicate_ids_and_codes_across_files_name_both_sources(bool duplicateCode)
    {
        var firstPath = Write(Document(), "a.json");
        var second = Document();
        if (duplicateCode) Item(second)["typeId"] = "item.other";
        var secondPath = Write(second, "b.json");
        var ex = Assert.Throws<ContentException>(() => EngineContentLoader.LoadItemTypes(_directory));
        Assert.Contains(firstPath, ex.Message);
        Assert.Contains(secondPath, ex.Message);
        Assert.Contains(duplicateCode ? "catalogCode" : "typeId", ex.Message);
    }

    [Fact]
    public void Legacy_schema_migrates_known_units_without_changing_quantities_or_mass()
    {
        var legacy = JsonNode.Parse("""
            { "itemTypes": [
              { "typeId":"item.food-rations", "displayName":"Food", "unitMassKg":1 },
              { "typeId":"item.energy-cells", "displayName":"Cells", "unitMassKg":5 },
              { "typeId":"item.fuel", "displayName":"Fuel", "unitMassKg":0 },
              { "typeId":"item.custom", "displayName":"Custom", "unitMassKg":10 }
            ] }
            """)!.AsObject();
        var items = EngineContentLoader.LoadItemTypes(Write(legacy));
        Assert.Equal(TradeUnit.Ration, items[0].TradeUnit);
        Assert.Equal(TradeUnit.EnergyCell, items[1].TradeUnit);
        Assert.Equal(5, items[1].UnitMassKg);
        Assert.Equal(ItemStorageKind.FuelTank, items[2].StorageKind);
        Assert.Equal(TradeUnit.Kilogram, items[2].TradeUnit);
        Assert.Equal(0, items[2].UnitMassKg);
        Assert.Equal(TradeUnit.Piece, items[3].TradeUnit);
    }

    [Fact]
    public void Fuel_cannot_claim_cargo_and_kilogram_cargo_cannot_weigh_five_kg()
    {
        var document = Document(); Item(document)["typeId"] = "item.fuel";
        Assert.Contains("storageKind", Assert.Throws<ContentException>(() => EngineContentLoader.LoadItemTypes(Write(document))).Message);
        document = Document(); Item(document)["tradeUnit"] = "Kilogram";
        Assert.Contains("unitMassKg", Assert.Throws<ContentException>(() => EngineContentLoader.LoadItemTypes(Write(document))).Message);
    }

    [Fact]
    public void Fuel_mass_metadata_does_not_define_tank_quantity()
    {
        var document = Document(); var item = Item(document);
        item["typeId"] = "item.fuel"; item["tradeUnit"] = "Kilogram"; item["storageKind"] = "FuelTank";
        item["unitMassKg"] = 0;
        var fuel = Assert.Single(EngineContentLoader.LoadItemTypes(Write(document)));
        Assert.Equal(ItemStorageKind.FuelTank, fuel.StorageKind);
        Assert.Equal(TradeUnit.Kilogram, fuel.TradeUnit);
        Assert.Equal(0, fuel.UnitMassKg);
    }

    [Theory]
    [InlineData("itemTypes", "null")]
    [InlineData("itemTypes", "[null]")]
    public void Missing_or_null_entries_report_source(string field, string value)
    {
        var document = Document(); document[field] = JsonNode.Parse(value);
        var path = Write(document);
        Assert.Contains(path, Assert.Throws<ContentException>(() => EngineContentLoader.LoadItemTypes(path)).Message);
    }

    [Fact]
    public void Registry_validates_direct_definitions_and_recipe_references()
    {
        Assert.Throws<ContentException>(() => Registry(new("item.test", "Test", 0)));
        var recipe = new RecipeDefinition("recipe.test", "Test", [new("item.missing", 1)], [], 1000);
        Assert.Contains("item.missing", Assert.Throws<ContentException>(() =>
            GameDataRegistry.Create([], [], [new("item.test", "Test", 1)], [], recipes: [recipe])).Message);
        Assert.Contains("item.missing", Assert.Throws<ContentException>(() =>
            GameDataRegistry.Create([], [], [new("item.test", "Test", 1)], [],
                factoryTypes: [new("factory.test", "Test", recipe)])).Message);
        Assert.Throws<ContentException>(() => GameDataRegistry.Create([], [],
            [new("item.a", "A", 1, CatalogCode: "same"), new("item.b", "B", 1, CatalogCode: "same")], []));
    }

    [Fact]
    public void Registry_copies_definitions_and_hash_ignores_input_order()
    {
        ItemTypeDefinition[] items = [new("item.a", "A", 1), new("item.b", "B", 2)];
        var registry = GameDataRegistry.Create([], [], items, []);
        Assert.Equal(registry.CatalogCompatibility, GameDataRegistry.Create([], [], Enumerable.Reverse(items), []).CatalogCompatibility);
        items[0] = items[0] with { UnitMassKg = 9 };
        Assert.Equal(1, registry.ItemTypes.GetDefinition(0).UnitMassKg);
        Assert.NotEqual(registry.CatalogCompatibility, GameDataRegistry.Create([], [], items, []).CatalogCompatibility);
    }

    private static GameDataRegistry Registry(ItemTypeDefinition item, string? legacyFingerprint = null) =>
        GameDataRegistry.Create([], [], [item], [], legacyCatalogFingerprint: legacyFingerprint);
}
