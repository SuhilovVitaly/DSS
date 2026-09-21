using System.Collections.Immutable;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class StationMarketProfileContentTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-market-profiles-{Guid.NewGuid():N}");
    private string ProfilePath => Path.Combine(_directory, "profiles.json");
    private string SettingsPath => Path.Combine(_directory, "settings.json");

    private const string ValidJson = """
        {
          "schemaVersion": 1,
          "sizeFactors": { "Outpost": 500, "Medium": 1000, "Large": 1500, "Huge": 2000 },
          "profiles": [{
            "typeId": "market.mining", "displayName": "Mining",
            "supplyItemTypeIds": ["item.ice", "item.electronics"],
            "demandItemTypeIds": ["item.water", "item.steel"],
            "initialInventory": [
              { "itemTypeId": "item.ice", "quantity": 162 },
              { "itemTypeId": "item.electronics", "quantity": 0 },
              { "itemTypeId": "item.water", "quantity": 36 },
              { "itemTypeId": "item.steel", "quantity": 1 }
            ],
            "initialCredits": 9600, "refuelStockKg": 200
          }]
        }
        """;

    public StationMarketProfileContentTests()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(ProfilePath, ValidJson);
        File.WriteAllText(Path.Combine(_directory, "modules.json"), """{"moduleTypes": []}""");
        File.WriteAllText(Path.Combine(_directory, "implementations.json"), """{"moduleImplementations": []}""");
        File.WriteAllText(Path.Combine(_directory, "commands.json"), """{"commandDefinitions": []}""");
        File.WriteAllText(Path.Combine(_directory, "items.json"), """
            { "schemaVersion": 2, "catalogVersion": 1, "itemTypes": [
              { "typeId": "item.ice", "displayName": "Ice", "unitMassKg": 1, "basePriceCredits": 10,
                "tradeCategory": "Resource", "tradeUnit": "Kilogram", "storageKind": "Cargo", "buyQuantityStep": 1, "sellQuantityStep": 1 },
              { "typeId": "item.electronics", "displayName": "Electronics", "unitMassKg": 1, "basePriceCredits": 150,
                "tradeCategory": "Good", "tradeUnit": "Piece", "storageKind": "Cargo", "buyQuantityStep": 1, "sellQuantityStep": 1 },
              { "typeId": "item.water", "displayName": "Water", "unitMassKg": 1, "basePriceCredits": 14,
                "tradeCategory": "Good", "tradeUnit": "Kilogram", "storageKind": "Cargo", "buyQuantityStep": 1, "sellQuantityStep": 1 },
              { "typeId": "item.steel", "displayName": "Steel", "unitMassKg": 1, "basePriceCredits": 40,
                "tradeCategory": "Good", "tradeUnit": "Kilogram", "storageKind": "Cargo", "buyQuantityStep": 1, "sellQuantityStep": 1 },
              { "typeId": "item.fuel", "displayName": "Fuel", "unitMassKg": 1, "basePriceCredits": 10,
                "tradeCategory": "Good", "tradeUnit": "Kilogram", "storageKind": "FuelTank", "buyQuantityStep": 1, "sellQuantityStep": 1 }
            ] }
            """);
        File.WriteAllText(SettingsPath, """
            { "typeData": {
                "moduleTypes": "modules.json", "moduleImplementations": "implementations.json",
                "commandDefinitions": "commands.json", "itemTypes": "items.json", "stationMarketProfiles": "profiles.json"
              }, "defaultScenario": "unused.json" }
            """);
    }

    private GameDataRegistry Load() => EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);
    private static JsonObject Document() => JsonNode.Parse(ValidJson)!.AsObject();
    private static JsonObject Profile(JsonObject root) => root["profiles"]![0]!.AsObject();
    private static JsonObject Stock(JsonObject root) => Profile(root)["initialInventory"]![0]!.AsObject();
    private void Write(JsonObject root) => File.WriteAllText(ProfilePath, root.ToJsonString());
    private static StationMarketProfileDefinition First(GameDataRegistry registry) => registry.StationMarketProfiles.GetDefinition(0);
    private static ItemTypeDefinition[] Items(GameDataRegistry registry) =>
        Enumerable.Range(0, registry.ItemTypes.Count).Select(registry.ItemTypes.GetDefinition).ToArray();
    private static GameDataRegistry Create(IEnumerable<ItemTypeDefinition> items, params StationMarketProfileDefinition[] profiles) =>
        GameDataRegistry.Create([], [], items, [], stationMarketProfiles: profiles);

    [Fact]
    public void Optional_profile_path_preserves_legacy_registry()
    {
        var settings = JsonNode.Parse(File.ReadAllText(SettingsPath))!;
        settings["typeData"]!.AsObject().Remove("stationMarketProfiles");
        File.WriteAllText(SettingsPath, settings.ToJsonString());
        File.Delete(ProfilePath);
        Assert.Equal(0, Load().StationMarketProfiles.Count);
        Assert.Equal(0, GameDataRegistry.Empty.StationMarketProfiles.Count);
        Assert.Equal(0, GameDataRegistry.Create([], [], [], []).StationMarketProfiles.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("missing.json")]
    [InlineData(null)]
    public void Declared_profile_path_is_required(string? path)
    {
        var settings = JsonNode.Parse(File.ReadAllText(SettingsPath))!;
        settings["typeData"]!["stationMarketProfiles"] = path;
        File.WriteAllText(SettingsPath, settings.ToJsonString());
        var error = Assert.Throws<ContentException>(() => Load());
        Assert.Contains(string.IsNullOrWhiteSpace(path) ? "stationMarketProfiles" : path, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Five_profiles_load_as_immutable_definitions()
    {
        var root = Document();
        var profiles = root["profiles"]!.AsArray();
        foreach (string role in new[] { "Industrial", "Hydroponic", "Transit", "Scientific" })
        {
            var profile = profiles[0]!.DeepClone();
            profile["typeId"] = $"market.{role.ToLowerInvariant()}";
            profile["displayName"] = role;
            if (role == "Transit")
            {
                profile["supplyItemTypeIds"] = new JsonArray();
                profile["initialInventory"]!.AsArray().RemoveAt(0);
                profile["initialInventory"]!.AsArray().RemoveAt(0);
                profile["initialCredits"] = 0;
                profile["refuelStockKg"] = 0;
            }
            profiles.Add(profile);
        }
        Write(root);
        var registry = Load();
        var repeated = Load();
        Assert.Equal(5, registry.StationMarketProfiles.Count);
        var transit = registry.StationMarketProfiles.GetDefinition(registry.StationMarketProfiles.GetIndex("market.transit"));
        Assert.Empty(transit.SupplyItemTypeIds);
        Assert.Equal(0, transit.InitialCredits);
        Assert.Equal(0, transit.RefuelStockKg);
        Assert.False(registry.StationMarketProfiles.Contains("MARKET.MINING"));
        for (int i = 0; i < 5; i++)
            Assert.Equal(registry.StationMarketProfiles.GetDefinition(i).Fingerprint, repeated.StationMarketProfiles.GetDefinition(i).Fingerprint);
        var mining = First(registry);
        Assert.Matches("^[0-9A-F]{64}$", mining.Fingerprint);
        Assert.Equal(0, mining.InitialInventory.Single(stock => stock.ItemTypeId == "item.electronics").Quantity);
        var changedSupply = mining.SupplyItemTypeIds.Add("item.other");
        var changedInventory = mining.InitialInventory.SetItem(0, mining.InitialInventory[0] with { Quantity = 0 });
        var changedFactors = mining.SizeFactors.SetItem(StationSize.Huge, 1);
        Assert.DoesNotContain("item.other", mining.SupplyItemTypeIds);
        Assert.Equal(162, mining.InitialInventory[0].Quantity);
        Assert.Equal(2000, mining.SizeFactors[StationSize.Huge]);
        Assert.NotEqual(mining.SupplyItemTypeIds, changedSupply);
        Assert.NotEqual(mining.InitialInventory, changedInventory);
        Assert.NotEqual(mining.SizeFactors, changedFactors);
        File.WriteAllText(ProfilePath, "{}");
        Assert.Equal(mining.Fingerprint, First(repeated).Fingerprint);
        Assert.Equal(Create(Items(registry)).CatalogCompatibility, registry.CatalogCompatibility);
    }

    [Theory]
    [InlineData("supplyItemTypeIds", "item.unknown")]
    [InlineData("demandItemTypeIds", "item.unknown")]
    [InlineData("initialInventory", "item.unknown")]
    [InlineData("refuelStockKg", "item.fuel")]
    public void Unknown_item_reports_file_profile_field_and_item(string field, string itemId)
    {
        var root = Document();
        if (field is "supplyItemTypeIds" or "demandItemTypeIds")
        {
            string original = Profile(root)[field]![0]!.GetValue<string>();
            Profile(root)[field]![0] = itemId;
            foreach (var stock in Profile(root)["initialInventory"]!.AsArray())
                if (stock!["itemTypeId"]!.GetValue<string>() == original) stock["itemTypeId"] = itemId;
        }
        else if (field == "initialInventory")
        {
            Profile(root)["initialInventory"]!.AsArray().Add(new JsonObject { ["itemTypeId"] = itemId, ["quantity"] = 1 });
        }
        else
        {
            var catalogPath = Path.Combine(_directory, "items.json");
            var catalog = JsonNode.Parse(File.ReadAllText(catalogPath))!;
            var items = catalog["itemTypes"]!.AsArray();
            items.Remove(items.Single(item => item!["typeId"]!.GetValue<string>() == itemId));
            File.WriteAllText(catalogPath, catalog.ToJsonString());
        }
        Write(root);
        var error = Assert.Throws<ContentException>(() => Load());
        Assert.Contains(ProfilePath, error.Message, StringComparison.Ordinal);
        Assert.Contains("market.mining", error.Message, StringComparison.Ordinal);
        Assert.Contains(field, error.Message, StringComparison.Ordinal);
        Assert.Contains(itemId, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Recipe_reference_error_does_not_report_profile_path()
    {
        File.WriteAllText(Path.Combine(_directory, "recipes.json"), """
            { "recipes": [{
                "typeId": "recipe.test", "displayName": "Test recipe",
                "inputs": [{ "itemTypeId": "item.unknown", "count": 1 }],
                "outputs": [{ "itemTypeId": "item.water", "count": 1 }],
                "cycleDurationMs": 1000
            }] }
            """);
        var settings = JsonNode.Parse(File.ReadAllText(SettingsPath))!;
        settings["typeData"]!["recipes"] = "recipes.json";
        File.WriteAllText(SettingsPath, settings.ToJsonString());

        var withProfiles = Assert.Throws<ContentException>(() => Load());
        Assert.Contains("Recipe 'recipe.test'", withProfiles.Message, StringComparison.Ordinal);
        Assert.Contains("item.unknown", withProfiles.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(ProfilePath, withProfiles.Message, StringComparison.Ordinal);

        settings["typeData"]!.AsObject().Remove("stationMarketProfiles");
        File.WriteAllText(SettingsPath, settings.ToJsonString());
        var withoutProfiles = Assert.Throws<ContentException>(() => Load());
        Assert.Equal(withoutProfiles.Message, withProfiles.Message);
    }

    public static IEnumerable<object[]> InvalidProfiles()
    {
        foreach (var (scope, fields) in new[]
        {
            ("root", new[] { "schemaVersion", "sizeFactors", "profiles" }),
            ("profile", new[] { "typeId", "displayName", "supplyItemTypeIds", "demandItemTypeIds", "initialInventory", "initialCredits", "refuelStockKg" }),
            ("stock", new[] { "itemTypeId", "quantity" }),
        })
        {
            foreach (string field in fields)
            {
                foreach (bool remove in new[] { true, false })
                {
                    var root = Document();
                    var target = scope == "root" ? root : scope == "profile" ? Profile(root) : Stock(root);
                    if (remove) target.Remove(field); else target[field] = null;
                    yield return [$"{scope}.{field} {(remove ? "missing" : "null")}", root.ToJsonString(), field];
                }
            }
        }
        (string Name, string Field, Action<JsonObject> Mutate)[] cases =
        [
            ("unknown root field", "typo", r => r["typo"] = 1),
            ("unknown profile field", "typo", r => Profile(r)["typo"] = 1),
            ("unknown stock field", "typo", r => Stock(r)["typo"] = 1),
            ("unsupported version", "schemaVersion", r => r["schemaVersion"] = 2),
            ("empty profiles", "profiles", r => r["profiles"] = new JsonArray()),
            ("null profile", "profiles", r => r["profiles"]![0] = null),
            ("null stock", "initialInventory", r => Profile(r)["initialInventory"]![0] = null),
            ("unknown size", "sizeFactors", r => r["sizeFactors"]!["Tiny"] = 500),
            ("numeric size", "sizeFactors", r => r["sizeFactors"]!["0"] = 500),
            ("lowercase size", "sizeFactors", r => r["sizeFactors"]!["huge"] = 500),
            ("missing size", "sizeFactors", r => r["sizeFactors"]!.AsObject().Remove("Huge")),
            ("zero factor", "sizeFactors", r => r["sizeFactors"]!["Huge"] = 0),
            ("negative factor", "sizeFactors", r => r["sizeFactors"]!["Huge"] = -1),
            ("wrong Medium", "sizeFactors", r => r["sizeFactors"]!["Medium"] = 999),
            ("fractional factor", "sizeFactors", r => r["sizeFactors"]!["Huge"] = 1.5m),
            ("factor overflow", "sizeFactors", r => r["sizeFactors"]!["Huge"] = long.MaxValue),
            ("null factor", "sizeFactors", r => r["sizeFactors"]!["Huge"] = null),
            ("duplicate ID", "typeId", r => { var p = Profile(r).DeepClone(); p["displayName"] = "Other"; r["profiles"]!.AsArray().Add(p); }),
            ("duplicate name", "displayName", r => { var p = Profile(r).DeepClone(); p["typeId"] = "market.other"; r["profiles"]!.AsArray().Add(p); }),
            ("empty ID", "typeId", r => Profile(r)["typeId"] = " "),
            ("empty name", "displayName", r => Profile(r)["displayName"] = " "),
            ("duplicate supply", "supplyItemTypeIds", r => Profile(r)["supplyItemTypeIds"]!.AsArray().Add("item.ice")),
            ("duplicate demand", "demandItemTypeIds", r => Profile(r)["demandItemTypeIds"]!.AsArray().Add("item.water")),
            ("empty supply ID", "supplyItemTypeIds", r => Profile(r)["supplyItemTypeIds"]![0] = ""),
            ("null demand ID", "demandItemTypeIds", r => Profile(r)["demandItemTypeIds"]![0] = null),
            ("empty demand", "demandItemTypeIds", r => Profile(r)["demandItemTypeIds"] = new JsonArray()),
            ("overlap", "demandItemTypeIds", r => Profile(r)["demandItemTypeIds"]!.AsArray().Add("item.ice")),
            ("duplicate stock", "initialInventory", r => Profile(r)["initialInventory"]!.AsArray().Add(Stock(r).DeepClone())),
            ("missing union item", "initialInventory", r => Profile(r)["initialInventory"]!.AsArray().RemoveAt(0)),
            ("extra union item", "initialInventory", r => Profile(r)["initialInventory"]!.AsArray().Add(new JsonObject { ["itemTypeId"] = "item.extra", ["quantity"] = 0 })),
            ("fuel supply", "supplyItemTypeIds", r => Profile(r)["supplyItemTypeIds"]![0] = "item.fuel"),
            ("fuel demand", "demandItemTypeIds", r => Profile(r)["demandItemTypeIds"]![0] = "item.fuel"),
            ("fuel inventory", "initialInventory", r => Stock(r)["itemTypeId"] = "item.fuel"),
            ("negative stock", "quantity", r => Stock(r)["quantity"] = -1),
            ("negative budget", "initialCredits", r => Profile(r)["initialCredits"] = -1),
            ("negative fuel", "refuelStockKg", r => Profile(r)["refuelStockKg"] = -1),
            ("stock scale overflow", "quantity", r => Stock(r)["quantity"] = long.MaxValue),
            ("budget scale overflow", "initialCredits", r => Profile(r)["initialCredits"] = long.MaxValue),
            ("fuel scale overflow", "refuelStockKg", r => Profile(r)["refuelStockKg"] = long.MaxValue),
            ("fractional stock", "quantity", r => Stock(r)["quantity"] = 0.5m),
            ("string budget", "initialCredits", r => Profile(r)["initialCredits"] = "9600"),
            ("fractional fuel", "refuelStockKg", r => Profile(r)["refuelStockKg"] = 0.5m),
        ];
        foreach (var (name, field, mutate) in cases)
        {
            var root = Document();
            mutate(root);
            yield return [name, root.ToJsonString(), field];
        }
        yield return ["duplicate size key", ValidJson.Replace("\"Huge\": 2000", "\"Huge\": 2000, \"Huge\": 3000", StringComparison.Ordinal), "sizeFactors"];
    }

    [Theory]
    [MemberData(nameof(InvalidProfiles))]
    public void Invalid_profile_schema_is_rejected(string description, string json, string field)
    {
        File.WriteAllText(ProfilePath, json);
        var error = Assert.Throws<ContentException>(() => Load());
        Assert.Contains(ProfilePath, error.Message, StringComparison.Ordinal);
        Assert.True(error.Message.Contains(field, StringComparison.Ordinal), $"{description}: {error.Message}");
    }

    [Fact]
    public void Direct_registry_creation_validates_profile_references()
    {
        var loaded = Load();
        var profile = First(loaded);
        var items = Items(loaded);
        Assert.Equal(profile.Fingerprint, First(Create(items, profile)).Fingerprint);
        foreach (string id in new[] { "item.ice", "item.water", "item.fuel" })
        {
            var error = Assert.Throws<ContentException>(() => Create(items.Where(item => item.TypeId != id), profile));
            Assert.Contains("market.mining", error.Message, StringComparison.Ordinal);
            Assert.Contains(id, error.Message, StringComparison.Ordinal);
        }
        foreach (string id in new[] { "item.ice", "item.fuel" })
        {
            var error = Assert.Throws<ContentException>(() => Create(items.Select(item => item.TypeId == id ? item with { BasePriceCredits = null } : item), profile));
            Assert.Contains(id, error.Message, StringComparison.Ordinal);
            Assert.Contains("BasePriceCredits", error.Message, StringComparison.Ordinal);
        }
        var tankItems = items.Select(item => item.TypeId == "item.water" ? item with { StorageKind = ItemStorageKind.FuelTank } : item);
        Assert.Contains("Cargo", Assert.Throws<ContentException>(() => Create(tankItems, profile)).Message, StringComparison.Ordinal);

        StationMarketProfileDefinition[] invalid =
        [
            profile with { TypeId = "" }, profile with { DisplayName = " " },
            profile with { SupplyItemTypeIds = default }, profile with { DemandItemTypeIds = default },
            profile with { DemandItemTypeIds = [] }, profile with { InitialInventory = default },
            profile with { SupplyItemTypeIds = ["item.ice", "item.ice"] },
            profile with { DemandItemTypeIds = ["item.ice"] },
            profile with { SupplyItemTypeIds = ["item.fuel"] },
            profile with { InitialInventory = profile.InitialInventory.RemoveAt(0) },
            profile with { InitialInventory = profile.InitialInventory.Add(profile.InitialInventory[0]) },
            profile with { InitialInventory = [null!] },
            profile with { InitialInventory = profile.InitialInventory.SetItem(0, new("item.ice", -1)) },
            profile with { InitialCredits = -1 }, profile with { RefuelStockKg = -1 },
            profile with { InitialCredits = long.MaxValue }, profile with { RefuelStockKg = long.MaxValue },
            profile with { InitialInventory = profile.InitialInventory.SetItem(0, new("item.ice", long.MaxValue)) },
            profile with { SizeFactors = null! }, profile with { SizeFactors = profile.SizeFactors.Remove(StationSize.Huge) },
            profile with { SizeFactors = profile.SizeFactors.SetItem(StationSize.Medium, 1001) },
            profile with { SizeFactors = profile.SizeFactors.SetItem(StationSize.Huge, 0) },
            profile with { SizeFactors = profile.SizeFactors.Remove(StationSize.Huge).Add((StationSize)99, 2000) },
        ];
        foreach (var candidate in invalid)
            Assert.Throws<ContentException>(() => Create(items, candidate));
        Assert.Throws<ContentException>(() => Create(items, profile, profile with { DisplayName = "Other" }));
        Assert.Throws<ContentException>(() => Create(items, profile, profile with { TypeId = "market.other" }));
    }

    [Fact]
    public void Profile_fingerprint_is_order_independent_but_detects_economic_changes()
    {
        var profile = First(Load());
        var reordered = profile with
        {
            DisplayName = "Localized name",
            SupplyItemTypeIds = profile.SupplyItemTypeIds.Reverse().ToImmutableArray(),
            DemandItemTypeIds = profile.DemandItemTypeIds.Reverse().ToImmutableArray(),
            InitialInventory = profile.InitialInventory.Reverse().ToImmutableArray(),
            SizeFactors = profile.SizeFactors.Reverse().ToImmutableDictionary(),
        };
        Assert.Equal(profile.Fingerprint, reordered.Fingerprint);
        StationMarketProfileDefinition[] changed =
        [
            profile with { TypeId = "market.other" },
            profile with { InitialCredits = profile.InitialCredits + 1 },
            profile with { RefuelStockKg = profile.RefuelStockKg + 1 },
            profile with { SizeFactors = profile.SizeFactors.SetItem(StationSize.Huge, 2001) },
            profile with { InitialInventory = profile.InitialInventory.SetItem(0, profile.InitialInventory[0] with { Quantity = 1 }) },
            profile with { SupplyItemTypeIds = profile.SupplyItemTypeIds.SetItem(0, "item.other") },
            profile with { DemandItemTypeIds = profile.DemandItemTypeIds.SetItem(0, "item.other") },
            profile with { InitialInventory = profile.InitialInventory.SetItem(0, profile.InitialInventory[0] with { ItemTypeId = "item.other" }) },
        ];
        foreach (var candidate in changed) Assert.NotEqual(profile.Fingerprint, candidate.Fingerprint);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
