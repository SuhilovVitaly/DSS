using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CatalogCompatibilityTests
{
    private static string SettingsPath => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client", "Settings.json"));

    private static GameDataRegistry RealRegistry() => EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);

    private static GameDataRegistry ChangeCatalog(GameDataRegistry source, Func<ItemTypeDefinition, ItemTypeDefinition> change,
        Func<StationMarketProfileDefinition, StationMarketProfileDefinition>? profileChange = null) =>
        GameDataRegistry.Create(
            Enumerable.Range(0, source.ModuleCategories.Count).Select(source.ModuleCategories.GetDefinition),
            Enumerable.Range(0, source.ModuleTypes.Count).Select(source.ModuleTypes.GetDefinition),
            Enumerable.Range(0, source.ItemTypes.Count).Select(i => change(source.ItemTypes.GetDefinition(i))),
            Enumerable.Range(0, source.CommandDefinitions.Count).Select(source.CommandDefinitions.GetDefinition),
            Enumerable.Range(0, source.FactoryTypes.Count).Select(source.FactoryTypes.GetDefinition),
            Enumerable.Range(0, source.Recipes.Count).Select(source.Recipes.GetDefinition),
            legacyCatalogFingerprint: source.LegacyCatalogFingerprint,
            stationMarketProfiles: Enumerable.Range(0, source.StationMarketProfiles.Count)
                .Select(source.StationMarketProfiles.GetDefinition).Select(profile => profileChange?.Invoke(profile) ?? profile),
            shipClasses: Enumerable.Range(0, source.ShipClasses.Count).Select(source.ShipClasses.GetDefinition),
            stationMarketEvents: Enumerable.Range(0, source.StationMarketEvents.Count).Select(source.StationMarketEvents.GetDefinition));

    private static ItemTypeDefinition[] Items(GameDataRegistry registry) =>
        Enumerable.Range(0, registry.ItemTypes.Count).Select(registry.ItemTypes.GetDefinition).ToArray();

    private static StationMarketProfileDefinition Profile(GameDataRegistry registry, string typeId) =>
        registry.StationMarketProfiles.GetDefinition(registry.StationMarketProfiles.GetIndex(typeId));

    /// <summary>
    /// Self-contained market-profile file carrying a valid optional economy fragment, used to drive
    /// the real JSON path (EngineContentLoader) rather than typed records — so DTO-level regressions
    /// (missing/null members, wrong JSON types, unknown nested members, case-sensitive
    /// productionSource) are caught. Item ids are never resolved against a catalog here:
    /// LoadStationMarketProfiles validates shape and profile-internal semantics only.
    /// </summary>
    private const string EconomyProfileJson = """
        {
          "schemaVersion": 1,
          "sizeFactors": { "Outpost": 500, "Medium": 1000, "Large": 1500, "Huge": 2000 },
          "profiles": [{
            "typeId": "market.fixture", "displayName": "Fixture",
            "supplyItemTypeIds": ["item.ice"],
            "demandItemTypeIds": ["item.water", "item.steel"],
            "initialInventory": [
              { "itemTypeId": "item.ice", "quantity": 108 },
              { "itemTypeId": "item.water", "quantity": 72 },
              { "itemTypeId": "item.steel", "quantity": 72 }
            ],
            "initialCredits": 9600, "refuelStockKg": 200,
            "economy": {
              "productionSource": "Profile",
              "hourlyInputs": [{ "itemTypeId": "item.water", "quantity": 4 }],
              "hourlyOutputs": [{ "itemTypeId": "item.ice", "quantity": 18 }],
              "hourlyConsumption": [{ "itemTypeId": "item.steel", "quantity": 2 }],
              "stockTargets": [
                { "itemTypeId": "item.ice", "targetStock": 108 },
                { "itemTypeId": "item.water", "targetStock": 72 },
                { "itemTypeId": "item.steel", "targetStock": 72 }
              ],
              "shortageThresholdPermille": 500,
              "surplusThresholdPermille": 1500,
              "budgetRegenerationDivisorPerDay": 24
            }
          }]
        }
        """;

    private static JsonObject EconomyDocument() => JsonNode.Parse(EconomyProfileJson)!.AsObject();
    private static JsonObject FixtureProfile(JsonObject root) => root["profiles"]![0]!.AsObject();
    private static JsonObject EconomyNode(JsonObject root) => FixtureProfile(root)["economy"]!.AsObject();

    /// <summary>Writes <paramref name="json"/> to a temporary file and loads it through the real content loader.</summary>
    private static T WithProfileFile<T>(string json, Func<string, T> use)
    {
        string path = Path.Combine(Path.GetTempPath(), $"dss-market-economy-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, json);
            return use(path);
        }
        finally { File.Delete(path); }
    }

    private static IReadOnlyList<StationMarketProfileDefinition> LoadProfiles(string json) =>
        WithProfileFile(json, EngineContentLoader.LoadStationMarketProfiles);

    /// <summary>
    /// Fixed US-0001-shaped profile behind <see cref="Us0001FrozenFingerprint"/>. Hard-coded rather
    /// than read from shipped content so the golden vector stays meaningful when TK-0004 edits the
    /// real profiles file.
    /// </summary>
    private static StationMarketProfileDefinition FrozenUs0001Profile() => new(
        TypeId: "market.mining",
        DisplayName: "Mining",
        SupplyItemTypeIds: ["item.ice", "item.iron-ore"],
        DemandItemTypeIds: ["item.water", "item.steel"],
        InitialInventory:
        [
            new("item.ice", 162), new("item.iron-ore", 162),
            new("item.water", 36), new("item.steel", 36),
        ],
        InitialCredits: 9600,
        RefuelStockKg: 200,
        SizeFactors: new Dictionary<StationSize, int>
        {
            [StationSize.Outpost] = 500,
            [StationSize.Medium] = 1000,
            [StationSize.Large] = 1500,
            [StationSize.Huge] = 2000,
        }.ToImmutableDictionary());

    /// <summary>Frozen pre-TK-0002 fingerprint of <see cref="FrozenUs0001Profile"/>; must never change.</summary>
    private const string Us0001FrozenFingerprint = "16204F065095A17A854D8D4764F9C81B8912758A5BC07D868D83E440FAC2FFED";

    /// <summary>
    /// The US-0001 hashing payload transcribed verbatim from the dependency, with no Economy member
    /// at all — an implementation independent of the (now-modified) production Fingerprint property.
    /// </summary>
    private static string LegacyUs0001Fingerprint(StationMarketProfileDefinition profile) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            profile.TypeId,
            SupplyItemTypeIds = profile.SupplyItemTypeIds.Order(StringComparer.Ordinal),
            DemandItemTypeIds = profile.DemandItemTypeIds.Order(StringComparer.Ordinal),
            InitialInventory = profile.InitialInventory.OrderBy(stock => stock.ItemTypeId, StringComparer.Ordinal),
            profile.InitialCredits,
            profile.RefuelStockKg,
            SizeFactors = profile.SizeFactors.OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal)
                .Select(pair => new { Size = pair.Key.ToString(), Factor = pair.Value }),
        })));

    private static ContentException LoadProfilesError(string json) =>
        WithProfileFile(json, path =>
        {
            var error = Assert.Throws<ContentException>(() => EngineContentLoader.LoadStationMarketProfiles(path));
            // Every content error must name the file, the offending profile and the field (ticket AC-01).
            Assert.Contains(path, error.Message, StringComparison.Ordinal);
            Assert.Contains("market.fixture", error.Message, StringComparison.Ordinal);
            return error;
        });

    /// <summary>
    /// Builds a schema-valid economy for <paramref name="profile"/>: for <see cref="StationMarketProductionSource.Profile"/>,
    /// hourlyOutputs exactly mirrors supplyItemTypeIds and demandItemTypeIds is split between
    /// hourlyInputs/hourlyConsumption (all consumption, none input, when supply is empty — the
    /// Transit shape); for <see cref="StationMarketProductionSource.Modules"/>, both stay empty
    /// and the full demand goes to hourlyConsumption. stockTargets covers every InitialInventory
    /// item with its own initial quantity as the target (so scaled InitialInventory never exceeds
    /// 2×scaled target, at any station size) and each hourly rate is a small fraction of it.
    /// </summary>
    private static StationMarketEconomyDefinition BuildEconomy(
        StationMarketProfileDefinition profile, StationMarketProductionSource source)
    {
        ImmutableArray<string> outputIds;
        ImmutableArray<string> inputIds;
        ImmutableArray<string> consumptionIds;
        if (source == StationMarketProductionSource.Modules)
        {
            outputIds = [];
            inputIds = [];
            consumptionIds = profile.DemandItemTypeIds;
        }
        else
        {
            outputIds = profile.SupplyItemTypeIds;
            if (outputIds.Length == 0)
            {
                inputIds = [];
                consumptionIds = profile.DemandItemTypeIds;
            }
            else
            {
                int half = Math.Max(1, profile.DemandItemTypeIds.Length / 2);
                inputIds = profile.DemandItemTypeIds.Take(half).ToImmutableArray();
                consumptionIds = profile.DemandItemTypeIds.Skip(half).ToImmutableArray();
            }
        }

        StationMarketStockDefinition Rate(string itemTypeId)
        {
            long initial = profile.InitialInventory.First(stock => stock.ItemTypeId == itemTypeId).Quantity;
            return new StationMarketStockDefinition(itemTypeId, Math.Max(1, initial / 20));
        }

        var targets = profile.InitialInventory
            .Select(stock => new StationMarketTargetDefinition(stock.ItemTypeId, stock.Quantity))
            .ToImmutableArray();

        return new StationMarketEconomyDefinition(
            source,
            HourlyInputs: inputIds.Select(Rate).ToImmutableArray(),
            HourlyOutputs: outputIds.Select(Rate).ToImmutableArray(),
            HourlyConsumption: consumptionIds.Select(Rate).ToImmutableArray(),
            StockTargets: targets,
            ShortageThresholdPermille: 500,
            SurplusThresholdPermille: 1500,
            BudgetRegenerationDivisorPerDay: 24);
    }

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
        Assert.Equal(15, SaveFormat.CurrentSaveFormatVersion);
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
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with
            {
                GameState = save.GameState with { PlayerTokens = 0, CatalogCompatibility = stamp }
            }));
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
        if (removePrice) registry = ChangeCatalog(registry, item => item.TypeId == itemId ? item with { BasePriceCredits = null } : item,
            profile => profile.Economy is not { } economy ? profile : profile with
            {
                // This fixture exercises scenario inventory validation; remove the optional profile
                // reference so the newly nontradeable catalog still reaches that exact boundary.
                Economy = economy with
                {
                    ExplicitStockTargets = economy.ExplicitStockTargets.IsDefault ? [] :
                    economy.ExplicitStockTargets.Where(target => target.ItemTypeId != itemId).ToImmutableArray()
                },
            });
        var objects = save.GameState.SpaceObjects.Select(obj => obj.ObjectId == "SPC-0002"
            ? obj with
            {
                MarketProfileId = null,
                MarketProfileFingerprint = null,
                MarketBudgetCredits = null,
                MarketRevision = null,
                Inventory = [new StationInventoryItemData(itemId, 1)]
            } : obj.MarketProfileId is { } profileId ? obj with
            {
                MarketProfileFingerprint = registry.StationMarketProfiles.GetDefinition(registry.StationMarketProfiles.GetIndex(profileId)).Fingerprint
            } : obj).ToArray();
        using var engine = new SimulationEngine(registry);
        var ex = Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                TradingMap = null,
                VoyageState = null,
                StationResourceFields = null,
                SpaceObjects = objects,
                CatalogCompatibility = registry.CatalogCompatibility
            }
        }));
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

    [Fact]
    public void Profile_economy_schema_accepts_five_role_shapes_and_modules_source()
    {
        var registry = RealRegistry();
        var items = Items(registry);
        Assert.True(registry.StationMarketProfiles.Count >= 5);

        for (int i = 0; i < registry.StationMarketProfiles.Count; i++)
        {
            var baseProfile = registry.StationMarketProfiles.GetDefinition(i);

            var profileSourced = baseProfile with { Economy = BuildEconomy(baseProfile, StationMarketProductionSource.Profile) };
            var createdProfile = GameDataRegistry.Create([], [], items, [], stationMarketProfiles: [profileSourced]);
            var loadedProfile = createdProfile.StationMarketProfiles.GetDefinition(0);
            Assert.NotNull(loadedProfile.Economy);
            Assert.Equal(StationMarketProductionSource.Profile, loadedProfile.Economy!.ProductionSource);

            var modulesSourced = baseProfile with { Economy = BuildEconomy(baseProfile, StationMarketProductionSource.Modules) };
            var createdModules = GameDataRegistry.Create([], [], items, [], stationMarketProfiles: [modulesSourced]);
            var loadedModules = createdModules.StationMarketProfiles.GetDefinition(0);
            Assert.NotNull(loadedModules.Economy);
            Assert.Equal(StationMarketProductionSource.Modules, loadedModules.Economy!.ProductionSource);
            Assert.Empty(loadedModules.Economy.HourlyInputs);
            Assert.Empty(loadedModules.Economy.HourlyOutputs);
        }

        // The same shapes must survive the real JSON path, not just typed construction.
        var fromJson = Assert.Single(LoadProfiles(EconomyProfileJson)).Economy;
        Assert.NotNull(fromJson);
        Assert.Equal(StationMarketProductionSource.Profile, fromJson!.ProductionSource);
        Assert.Equal("item.ice", Assert.Single(fromJson.HourlyOutputs).ItemTypeId);
        Assert.Equal(18, Assert.Single(fromJson.HourlyOutputs).Quantity);
        Assert.Equal("item.water", Assert.Single(fromJson.HourlyInputs).ItemTypeId);
        Assert.Equal(4, Assert.Single(fromJson.HourlyInputs).Quantity);
        Assert.Equal("item.steel", Assert.Single(fromJson.HourlyConsumption).ItemTypeId);
        Assert.Equal(3, fromJson.StockTargets.Length);
        Assert.Equal(108, fromJson.StockTargets.Single(target => target.ItemTypeId == "item.ice").TargetStock);
        Assert.Equal(500, fromJson.ShortageThresholdPermille);
        Assert.Equal(1500, fromJson.SurplusThresholdPermille);
        Assert.Equal(24, fromJson.BudgetRegenerationDivisorPerDay);

        // An absent or explicitly-null economy keeps the profile US-0001 bootstrap-only.
        var absent = EconomyDocument();
        FixtureProfile(absent).Remove("economy");
        Assert.Null(Assert.Single(LoadProfiles(absent.ToJsonString())).Economy);
        var explicitNull = EconomyDocument();
        FixtureProfile(explicitNull)["economy"] = null;
        Assert.Null(Assert.Single(LoadProfiles(explicitNull.ToJsonString())).Economy);

        // Transit shape through JSON: no supply, no production, pure consumption — empty arrays are
        // legal values (only a missing/null array is an error).
        var transitJson = EconomyDocument();
        FixtureProfile(transitJson)["supplyItemTypeIds"] = new JsonArray();
        FixtureProfile(transitJson)["initialInventory"]!.AsArray().RemoveAt(0);
        EconomyNode(transitJson)["hourlyOutputs"] = new JsonArray();
        EconomyNode(transitJson)["hourlyInputs"] = new JsonArray();
        EconomyNode(transitJson)["hourlyConsumption"] = new JsonArray
        {
            new JsonObject { ["itemTypeId"] = "item.water", ["quantity"] = 4 },
            new JsonObject { ["itemTypeId"] = "item.steel", ["quantity"] = 2 },
        };
        EconomyNode(transitJson)["stockTargets"]!.AsArray().RemoveAt(0);
        var transit = Assert.Single(LoadProfiles(transitJson.ToJsonString())).Economy;
        Assert.NotNull(transit);
        Assert.Empty(transit!.HourlyOutputs);
        Assert.Empty(transit.HourlyInputs);
        Assert.Equal(2, transit.HourlyConsumption.Length);
    }

    [Fact]
    public void Profile_economy_rejects_unknown_items_missing_fields_and_invalid_bounds()
    {
        var registry = RealRegistry();
        var items = Items(registry);
        var mining = Profile(registry, "market.mining");
        var miningEconomy = BuildEconomy(mining, StationMarketProductionSource.Profile);
        var miningProfile = mining with { Economy = miningEconomy };
        // Sanity: the baseline itself is accepted, so every failure below is attributable to its own mutation.
        GameDataRegistry.Create([], [], items, [], stationMarketProfiles: [miningProfile]);

        var transit = Profile(registry, "market.transit");
        var transitEconomy = BuildEconomy(transit, StationMarketProductionSource.Profile);
        var transitProfile = transit with { Economy = transitEconomy };
        GameDataRegistry.Create([], [], items, [], stationMarketProfiles: [transitProfile]);

        // Move every hourlyInput into hourlyConsumption (coverage of demandItemTypeIds is preserved) so
        // hourlyInputs becomes empty while hourlyOutputs (Mining's nonempty supply) stays untouched.
        var miningInputsEmptied = miningEconomy with
        {
            HourlyInputs = [],
            HourlyConsumption = miningEconomy.HourlyConsumption.AddRange(miningEconomy.HourlyInputs),
        };
        // Move one Transit consumption item into hourlyInputs (coverage still preserved, no overlap)
        // so hourlyInputs becomes nonempty while hourlyOutputs (Transit's empty supply) stays untouched.
        var transitInputsFilled = transitEconomy with
        {
            HourlyInputs = [transitEconomy.HourlyConsumption[0]],
            HourlyConsumption = transitEconomy.HourlyConsumption.RemoveAt(0),
        };

        (string Name, StationMarketProfileDefinition Profile, string ExpectedSubstring)[] cases =
        [
            ("unknown item in hourlyOutputs",
                miningProfile with { Economy = miningEconomy with { HourlyOutputs = miningEconomy.HourlyOutputs.Add(new("item.unknown", 1)) } },
                "unknown item"),
            ("non-positive hourly rate",
                miningProfile with { Economy = miningEconomy with { HourlyInputs = miningEconomy.HourlyInputs.SetItem(0, miningEconomy.HourlyInputs[0] with { Quantity = 0 }) } },
                "quantity must be positive"),
            ("duplicate item within hourlyInputs",
                miningProfile with { Economy = miningEconomy with { HourlyInputs = miningEconomy.HourlyInputs.Add(miningEconomy.HourlyInputs[0]) } },
                "duplicate item"),
            ("item shared between hourlyInputs and hourlyConsumption",
                miningProfile with { Economy = miningEconomy with { HourlyConsumption = miningEconomy.HourlyConsumption.Add(miningEconomy.HourlyInputs[0]) } },
                "also appears in hourlyInputs"),
            ("hourlyOutputs does not exactly match supplyItemTypeIds",
                miningProfile with { Economy = miningEconomy with { HourlyOutputs = miningEconomy.HourlyOutputs.RemoveAt(0) } },
                "must exactly match supplyItemTypeIds"),
            ("hourlyInputs+hourlyConsumption do not exactly cover demandItemTypeIds",
                miningProfile with { Economy = miningEconomy with { HourlyInputs = miningEconomy.HourlyInputs.RemoveAt(0) } },
                "must exactly cover demandItemTypeIds"),
            ("hourlyInputs nonempty while hourlyOutputs is empty (Transit shape)",
                transitProfile with { Economy = transitInputsFilled },
                "must be empty when hourlyOutputs is empty"),
            ("hourlyInputs empty while hourlyOutputs is nonempty",
                miningProfile with { Economy = miningInputsEmptied },
                "must not be empty when hourlyOutputs is not empty"),
            ("Modules source keeps a nonempty hourlyOutputs",
                miningProfile with { Economy = miningEconomy with { ProductionSource = StationMarketProductionSource.Modules } },
                "must be empty for Modules production source"),
            ("stockTargets missing an initialInventory item",
                miningProfile with { Economy = miningEconomy with { StockTargets = miningEconomy.StockTargets.RemoveAt(0) } },
                "must exactly cover initialInventory items"),
            ("duplicate stockTargets item",
                miningProfile with { Economy = miningEconomy with { StockTargets = miningEconomy.StockTargets.Add(miningEconomy.StockTargets[0]) } },
                "duplicate item"),
            ("non-positive stockTargets targetStock",
                miningProfile with { Economy = miningEconomy with { StockTargets = miningEconomy.StockTargets.SetItem(0, miningEconomy.StockTargets[0] with { TargetStock = 0 }) } },
                "targetStock must be positive"),
            ("shortageThresholdPermille out of bounds",
                miningProfile with { Economy = miningEconomy with { ShortageThresholdPermille = 1000 } },
                "shortageThresholdPermille"),
            ("surplusThresholdPermille out of bounds",
                miningProfile with { Economy = miningEconomy with { SurplusThresholdPermille = 1000 } },
                "surplusThresholdPermille"),
            ("budgetRegenerationDivisorPerDay below 24",
                miningProfile with { Economy = miningEconomy with { BudgetRegenerationDivisorPerDay = 23 } },
                "budgetRegenerationDivisorPerDay"),
            ("initialCredits not positive with economy configured",
                miningProfile with { InitialCredits = 0 },
                "must be positive when economy is configured"),
            ("hourly rate exceeds the smallest station size's capacity",
                miningProfile with { Economy = miningEconomy with { HourlyOutputs = miningEconomy.HourlyOutputs.SetItem(0, miningEconomy.HourlyOutputs[0] with { Quantity = 100_000 }) } },
                "exceeds capacity"),
            ("stockTargets scaling overflows Int64",
                miningProfile with { Economy = miningEconomy with { StockTargets = miningEconomy.StockTargets.SetItem(0, miningEconomy.StockTargets[0] with { TargetStock = long.MaxValue }) } },
                "overflows Int64"),
        ];

        foreach (var (name, candidate, expected) in cases)
        {
            var error = Assert.Throws<ContentException>(() =>
                GameDataRegistry.Create([], [], items, [], stationMarketProfiles: [candidate]));
            Assert.True(error.Message.Contains(expected, StringComparison.Ordinal), $"{name}: {error.Message}");
        }

        // The DTO/deserialization layer owns a different class of failures than the typed cases above:
        // missing vs null vs wrong-typed members, unknown nested members and a case-sensitive enum.
        (string Name, Action<JsonObject> Mutate, string ExpectedSubstring)[] jsonCases =
        [
            ("productionSource missing", root => EconomyNode(root).Remove("productionSource"), "productionSource"),
            ("productionSource null", root => EconomyNode(root)["productionSource"] = null, "productionSource"),
            ("productionSource lowercase", root => EconomyNode(root)["productionSource"] = "profile", "productionSource"),
            ("productionSource unknown", root => EconomyNode(root)["productionSource"] = "Factory", "productionSource"),
            ("productionSource numeric", root => EconomyNode(root)["productionSource"] = 0, "productionSource"),
            // Enum.TryParse would happily accept the underlying number as a string; only the
            // round-trip guard rejects it.
            ("productionSource numeric string", root => EconomyNode(root)["productionSource"] = "0", "productionSource"),
            ("hourlyInputs missing", root => EconomyNode(root).Remove("hourlyInputs"), "hourlyInputs"),
            ("hourlyInputs null", root => EconomyNode(root)["hourlyInputs"] = null, "hourlyInputs"),
            ("hourlyOutputs missing", root => EconomyNode(root).Remove("hourlyOutputs"), "hourlyOutputs"),
            ("hourlyConsumption missing", root => EconomyNode(root).Remove("hourlyConsumption"), "hourlyConsumption"),
            ("stockTargets missing", root => EconomyNode(root).Remove("stockTargets"), "stockTargets"),
            ("shortageThresholdPermille missing", root => EconomyNode(root).Remove("shortageThresholdPermille"), "shortageThresholdPermille"),
            ("surplusThresholdPermille null", root => EconomyNode(root)["surplusThresholdPermille"] = null, "surplusThresholdPermille"),
            ("budgetRegenerationDivisorPerDay missing", root => EconomyNode(root).Remove("budgetRegenerationDivisorPerDay"), "budgetRegenerationDivisorPerDay"),
            ("unknown economy member", root => EconomyNode(root)["typo"] = 1, "typo"),
            ("unknown stockTargets member", root => EconomyNode(root)["stockTargets"]![0]!["typo"] = 1, "typo"),
            ("unknown hourly member", root => EconomyNode(root)["hourlyInputs"]![0]!["typo"] = 1, "typo"),
            ("threshold as string", root => EconomyNode(root)["shortageThresholdPermille"] = "500", "shortageThresholdPermille"),
            ("hourly list as object", root => EconomyNode(root)["hourlyInputs"] = new JsonObject(), "hourlyInputs"),
            ("fractional hourly quantity", root => EconomyNode(root)["hourlyInputs"]![0]!["quantity"] = 0.5m, "hourlyInputs"),
            ("fractional targetStock", root => EconomyNode(root)["stockTargets"]![0]!["targetStock"] = 0.5m, "stockTargets"),
            ("null hourly entry", root => EconomyNode(root)["hourlyInputs"]![0] = null, "hourlyInputs"),
            ("null stockTargets entry", root => EconomyNode(root)["stockTargets"]![0] = null, "stockTargets"),
            ("hourly entry missing quantity", root => EconomyNode(root)["hourlyInputs"]![0]!.AsObject().Remove("quantity"), "quantity"),
            ("hourly entry missing itemTypeId", root => EconomyNode(root)["hourlyInputs"]![0]!.AsObject().Remove("itemTypeId"), "itemTypeId"),
            ("stockTargets entry missing targetStock", root => EconomyNode(root)["stockTargets"]![0]!.AsObject().Remove("targetStock"), "targetStock"),
            ("stockTargets entry missing itemTypeId", root => EconomyNode(root)["stockTargets"]![0]!.AsObject().Remove("itemTypeId"), "itemTypeId"),
            ("fuel as an hourly rate", root => EconomyNode(root)["hourlyOutputs"]![0]!["itemTypeId"] = "item.fuel", "item.fuel"),
            ("threshold out of bounds", root => EconomyNode(root)["shortageThresholdPermille"] = 1000, "shortageThresholdPermille"),
            ("divisor below 24", root => EconomyNode(root)["budgetRegenerationDivisorPerDay"] = 23, "budgetRegenerationDivisorPerDay"),
            ("initialCredits zero with economy", root => FixtureProfile(root)["initialCredits"] = 0, "initialCredits"),
        ];

        foreach (var (name, mutate, expected) in jsonCases)
        {
            var document = EconomyDocument();
            mutate(document);
            var error = LoadProfilesError(document.ToJsonString());
            Assert.True(error.Message.Contains(expected, StringComparison.Ordinal), $"{name}: {error.Message}");
        }
    }

    [Fact]
    public void Profile_without_economy_keeps_us0001_fingerprint()
    {
        // Golden vector: a frozen literal hash, NOT a value recomputed from current behaviour, so the
        // expected side cannot drift together with the production side. It was produced by the
        // US-0001 Fingerprint implementation for exactly this profile. A diff here means the
        // null-Economy hashing payload stopped being byte-identical — which would invalidate every
        // marketProfileFingerprint already written into US-0001 save files (AC-07).
        var frozen = FrozenUs0001Profile();
        Assert.Null(frozen.Economy);
        Assert.Equal(Us0001FrozenFingerprint, frozen.Fingerprint);
        // Second, independent lock: the literal is also what the transcribed US-0001 algorithm yields,
        // so the constant is pinned to the dependency's payload shape and not merely to today's output.
        Assert.Equal(Us0001FrozenFingerprint, LegacyUs0001Fingerprint(frozen));
        Assert.Equal(Us0001FrozenFingerprint, (frozen with { Economy = null }).Fingerprint);
        Assert.NotEqual(Us0001FrozenFingerprint,
            (frozen with { Economy = BuildEconomy(frozen, StationMarketProductionSource.Profile) }).Fingerprint);

        // Since US-0002 TK-0004 every shipped profile configures an economy, so its fingerprint must
        // move off the US-0001 payload shape; stripping the economy restores exactly that shape.
        // These cannot use a literal: shipped numbers are content and may be retuned.
        var registry = RealRegistry();
        for (int i = 0; i < registry.StationMarketProfiles.Count; i++)
        {
            var shipped = registry.StationMarketProfiles.GetDefinition(i);
            Assert.NotNull(shipped.Economy);
            Assert.NotEqual(LegacyUs0001Fingerprint(shipped), shipped.Fingerprint);
            Assert.Equal(LegacyUs0001Fingerprint(shipped), (shipped with { Economy = null }).Fingerprint);
        }
    }

    [Fact]
    public void Economy_fingerprint_covers_rates_targets_source_and_budget_policy()
    {
        var registry = RealRegistry();
        var mining = Profile(registry, "market.mining");
        var economy = BuildEconomy(mining, StationMarketProductionSource.Profile);
        var profile = mining with { Economy = economy };

        var reordered = profile with
        {
            Economy = economy with
            {
                HourlyInputs = economy.HourlyInputs.Reverse().ToImmutableArray(),
                HourlyOutputs = economy.HourlyOutputs.Reverse().ToImmutableArray(),
                HourlyConsumption = economy.HourlyConsumption.Reverse().ToImmutableArray(),
                StockTargets = economy.StockTargets.Reverse().ToImmutableArray(),
            },
        };
        Assert.Equal(profile.Fingerprint, reordered.Fingerprint);

        StationMarketProfileDefinition[] changed =
        [
            profile with { Economy = economy with { ProductionSource = StationMarketProductionSource.Modules } },
            profile with { Economy = economy with { ShortageThresholdPermille = economy.ShortageThresholdPermille + 1 } },
            profile with { Economy = economy with { SurplusThresholdPermille = economy.SurplusThresholdPermille + 1 } },
            profile with { Economy = economy with { BudgetRegenerationDivisorPerDay = economy.BudgetRegenerationDivisorPerDay + 1 } },
            profile with { Economy = economy with { StockTargets = economy.StockTargets.SetItem(0, economy.StockTargets[0] with { TargetStock = economy.StockTargets[0].TargetStock + 1 }) } },
            profile with { Economy = economy with { HourlyOutputs = economy.HourlyOutputs.SetItem(0, economy.HourlyOutputs[0] with { Quantity = economy.HourlyOutputs[0].Quantity + 1 }) } },
            profile with { Economy = economy with { HourlyInputs = economy.HourlyInputs.SetItem(0, economy.HourlyInputs[0] with { ItemTypeId = "item.other" }) } },
        ];
        foreach (var candidate in changed)
            Assert.NotEqual(profile.Fingerprint, candidate.Fingerprint);

        // Economy participates only in the profile fingerprint, never in the unrelated item catalog hash.
        var items = Items(registry);
        Assert.Equal(GameDataRegistry.Create([], [], items, []).CatalogCompatibility, registry.CatalogCompatibility);
    }

    [Fact]
    public void Explicit_stock_targets_are_optional_capacity_without_bootstrap_or_flow()
    {
        var baseline = Assert.Single(LoadProfiles(EconomyProfileJson));
        Assert.Equal("54D14BC95D1BFD36716F76195E9666759BB10F0FAA7C1815F405DBBE2B91EFFA", baseline.Fingerprint);
        Assert.True(baseline.Economy!.ExplicitStockTargets.IsDefaultOrEmpty);
        Assert.Equal(baseline.Fingerprint, (baseline with { Economy = baseline.Economy with { ExplicitStockTargets = [] } }).Fingerprint);
        var document = EconomyDocument();
        EconomyNode(document)["explicitStockTargets"] = new JsonArray
        {
            new JsonObject { ["itemTypeId"] = "item.energy-cells", ["targetStock"] = 144 },
            new JsonObject { ["itemTypeId"] = "item.silicon", ["targetStock"] = 144 },
        };
        var extended = Assert.Single(LoadProfiles(document.ToJsonString()));
        var created = GameDataRegistry.Create([], [], Items(RealRegistry()), [], stationMarketProfiles: [extended]);
        Assert.Equal(2, created.StationMarketProfiles.GetDefinition(0).Economy!.ExplicitStockTargets.Length);
        Assert.Equal(baseline.InitialInventory.ToArray(), extended.InitialInventory.ToArray());
        Assert.Equal(baseline.SupplyItemTypeIds.ToArray(), extended.SupplyItemTypeIds.ToArray());
        Assert.Equal(baseline.DemandItemTypeIds.ToArray(), extended.DemandItemTypeIds.ToArray());
        Assert.Equal(baseline.Economy.HourlyInputs.ToArray(), extended.Economy!.HourlyInputs.ToArray());
        Assert.Equal(baseline.Economy.HourlyOutputs.ToArray(), extended.Economy.HourlyOutputs.ToArray());
        Assert.Equal(baseline.Economy.HourlyConsumption.ToArray(), extended.Economy.HourlyConsumption.ToArray());
        Assert.Equal(5, extended.Economy.AllStockTargets.Count());
        Assert.NotEqual(baseline.Fingerprint, extended.Fingerprint);
        Assert.Equal(extended.Fingerprint, (extended with
        {
            Economy = extended.Economy with
            {
                ExplicitStockTargets = extended.Economy.ExplicitStockTargets.Reverse().ToImmutableArray(),
            }
        }).Fingerprint);
        Assert.NotEqual(extended.Fingerprint, (extended with
        {
            Economy = extended.Economy with
            {
                ExplicitStockTargets = extended.Economy.ExplicitStockTargets.SetItem(0, new("item.energy-cells", 145)),
            }
        }).Fingerprint);
    }

    [Fact]
    public void Explicit_stock_target_schema_rejects_malformed_json_with_field_context()
    {
        (JsonNode? Value, string Expected)[] cases =
        [
            (new JsonObject(), "explicitStockTargets"),
            (new JsonArray((JsonNode?)null), "explicitStockTargets"),
            (new JsonArray(new JsonObject { ["itemTypeId"] = "item.energy-cells" }), "targetStock"),
            (new JsonArray(new JsonObject { ["targetStock"] = 144 }), "itemTypeId"),
            (new JsonArray(new JsonObject { ["itemTypeId"] = "item.energy-cells", ["targetStock"] = 0.5m }), "explicitStockTargets"),
            (new JsonArray(new JsonObject { ["itemTypeId"] = "item.energy-cells", ["targetStock"] = 144, ["typo"] = 1 }), "typo"),
        ];
        foreach (var (value, expected) in cases)
        {
            var document = EconomyDocument();
            EconomyNode(document)["explicitStockTargets"] = value;
            Assert.Contains(expected, LoadProfilesError(document.ToJsonString()).Message);
        }
    }

    [Fact]
    public void Explicit_stock_target_semantics_are_validated_on_direct_registry_create()
    {
        var baseline = Assert.Single(LoadProfiles(EconomyProfileJson));
        (ImmutableArray<StationMarketTargetDefinition> Targets, string Expected)[] cases =
        [
            ([new("item.unknown", 144)], "unknown item"),
            ([new("item.fuel", 144)], "refuelStockKg"),
            ([new("item.ice", 144)], "duplicate item"),
            ([new("item.energy-cells", 144), new("item.energy-cells", 145)], "duplicate item"),
            ([new("item.energy-cells", 0)], "must be positive"),
            ([new("item.energy-cells", long.MaxValue)], "overflows Int64"),
            ([null!], "must not be null"),
        ];
        var items = Items(RealRegistry());
        foreach (var (targets, expected) in cases)
        {
            var profile = baseline with { Economy = baseline.Economy! with { ExplicitStockTargets = targets } };
            var error = Assert.Throws<ContentException>(() => GameDataRegistry.Create([], [], items, [], stationMarketProfiles: [profile]));
            Assert.Contains(expected, error.Message);
            Assert.Contains("explicitStockTargets", error.Message);
        }
    }

    [Fact]
    public void Save_v9_roundtrips_market_budget_and_pending_output()
    {
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(SettingsPath);
        var save = engine.CaptureSaveState();
        Assert.Equal(SaveFormat.CurrentSaveFormatVersion, save.SaveFormatVersion);

        var station = save.GameState.SpaceObjects.First(obj =>
            string.Equals(obj.ObjectType, "Station", StringComparison.OrdinalIgnoreCase));
        var pendingOutput = new[] { new StationInventoryItemData("item.ice", 7), new StationInventoryItemData("item.steel", 3) };
        var mutatedStation = station with
        {
            MarketBudgetCredits = 4200,
            ProducingModules = [new StationProducingModuleData("factory.test", Active: true, PendingOutput: pendingOutput)],
        };
        var objects = save.GameState.SpaceObjects
            .Select(obj => obj.ObjectId == station.ObjectId ? mutatedStation : obj).ToArray();
        var mutatedSave = save with { GameState = save.GameState with { SpaceObjects = objects } };

        var roundTripped = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(mutatedSave), allowNonZeroGameTime: true);
        var roundTrippedStation = roundTripped.GameState.SpaceObjects.Single(obj => obj.ObjectId == station.ObjectId);

        Assert.Equal(4200, roundTrippedStation.MarketBudgetCredits);
        var module = Assert.Single(roundTrippedStation.ProducingModules!);
        Assert.Equal("factory.test", module.ProducingModuleTypeId);
        Assert.NotNull(module.PendingOutput);
        Assert.Equal(2, module.PendingOutput!.Count);
        Assert.Contains(module.PendingOutput, item => item.ItemTypeId == "item.ice" && item.Quantity == 7);
        Assert.Contains(module.PendingOutput, item => item.ItemTypeId == "item.steel" && item.Quantity == 3);
    }

    [Fact]
    public void Legacy_save_shape_has_no_implicit_market_budget_or_pending_output()
    {
        const string legacyStationJson = """
            {"objectId":"STATION","objectType":"Station","persistenceType":"Permanent","name":null,
             "positionX":1,"positionY":0,"speedMps":0,"directionDegrees":0,"movementType":"Stationary",
             "massKg":null,"compositionType":null,"modules":null,
             "credits":100,"inventory":[{"itemTypeId":"item.legacy","quantity":10}]}
            """;
        var station = JsonSerializer.Deserialize<SpaceObjectData>(legacyStationJson);
        Assert.NotNull(station);
        Assert.Null(station!.MarketBudgetCredits);
        Assert.Null(station.ProducingModules);

        const string legacyModuleJson = """{"producingModuleTypeId":"factory.legacy"}""";
        var module = JsonSerializer.Deserialize<StationProducingModuleData>(legacyModuleJson);
        Assert.NotNull(module);
        Assert.True(module!.Active);
        Assert.Null(module.PendingOutput);
    }
}


public sealed class CargoCostPersistenceTests
{
    private const string Cargo = QuotedTradeExecutionTests.CargoModuleId;
    private static ScenarioFile Template(int version, CargoStackData stack)
    {
        using var engine = QuotedTradeExecutionTests.CreateMarketEngine();
        var save = engine.CaptureSaveStateForTests(0, DeepSpaceSaga.Contracts.SimulationSpeed.Speed0, 0);
        save = QuotedTradeExecutionTests.WithShipModules(save, m => m.ModuleId == Cargo ? m with { Cargo = [stack] } : m);
        if (version < 9)
            save = save with
            {
                GameState = save.GameState with
                {
                    SpaceObjects = save.GameState.SpaceObjects.Select(o =>
                o.ObjectType != "Station" ? o : o with
                {
                    MarketBudgetCredits = null,
                    MarketRevision = null,
                    MarketProfileId = version == 0 ? o.MarketProfileId : null,
                    MarketProfileFingerprint = null
                }).ToArray()
                }
            };
        return save with { SaveFormatVersion = version, GameState = save.GameState with { MarketKnowledge = version < 14 ? null : save.GameState.MarketKnowledge, TradingEconomyContinuation = version < 15 ? null : save.GameState.TradingEconomyContinuation } };
    }
    private static CargoStackData Stack(SimulationEngine engine) => engine.CaptureSaveStateForTests(0, DeepSpaceSaga.Contracts.SimulationSpeed.Speed0, 0)
        .GameState.SpaceObjects.Single(o => o.ObjectId == QuotedTradeExecutionTests.ShipId).Modules!.Single(m => m.ModuleId == Cargo).Cargo!.Single();

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    public void Legacy_save_without_basis_stays_unknown_instead_of_zero_or_current_price(int version)
    {
        using var engine = new SimulationEngine(QuotedTradeExecutionTests.Registry);
        engine.LoadScenario(Template(version, new("item.ice", 3)), true);
        var stack = Stack(engine);
        Assert.Null(stack.CostBasisCredits);
        Assert.Equal(new[] { "legacy-unknown" }, stack.AcquisitionSources);
        var save = engine.CaptureSaveStateForTests(0, DeepSpaceSaga.Contracts.SimulationSpeed.Speed0, 0);
        Assert.Equal(SaveFormat.CurrentSaveFormatVersion, save.SaveFormatVersion);
        using var loaded = new SimulationEngine(QuotedTradeExecutionTests.Registry);
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), true);
        Assert.Equal(JsonSerializer.Serialize(stack), JsonSerializer.Serialize(Stack(loaded)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Current_save_roundtrips_known_and_legacy_unknown_cargo_metadata(bool unknown)
    {
        var stack = unknown ? new CargoStackData("item.ice", 3, null, ["legacy-unknown"]) :
            new CargoStackData("item.ice", 3, long.MaxValue, ["produced", "mined"]);
        using var engine = new SimulationEngine(QuotedTradeExecutionTests.Registry);
        engine.LoadScenario(Template(SaveFormat.CurrentSaveFormatVersion, stack), true);
        var actual = Stack(engine);
        Assert.Equal(stack.CostBasisCredits, actual.CostBasisCredits);
        Assert.Equal(unknown ? new[] { "legacy-unknown" } : ["mined", "produced"], actual.AcquisitionSources);
        using var loaded = new SimulationEngine(QuotedTradeExecutionTests.Registry);
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(0, DeepSpaceSaga.Contracts.SimulationSpeed.Speed0, 0)), true), true);
        Assert.Equal(JsonSerializer.Serialize(actual), JsonSerializer.Serialize(Stack(loaded)));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("negative")]
    [InlineData("duplicate")]
    [InlineData("unknown-source")]
    [InlineData("blank")]
    [InlineData("mixed-legacy")]
    [InlineData("known-legacy")]
    [InlineData("null-known")]
    [InlineData("empty-sources")]
    [InlineData("empty-positive")]
    public void Current_save_rejects_invalid_cost_metadata_without_world_replacement(string failure)
    {
        using var engine = new SimulationEngine(QuotedTradeExecutionTests.Registry);
        var good = Template(13, new("item.ice", 3, 123, ["purchased"]));
        engine.LoadScenario(good, true);
        var badStack = failure switch
        {
            "missing" => new CargoStackData("item.ice", 3),
            "negative" => new("item.ice", 3, -1, ["purchased"]),
            "duplicate" => new("item.ice", 3, 1, ["purchased", "purchased"]),
            "unknown-source" => new("item.ice", 3, 1, ["market-price"]),
            "blank" => new("item.ice", 3, 1, [" "]),
            "mixed-legacy" => new("item.ice", 3, null, ["legacy-unknown", "purchased"]),
            "known-legacy" => new("item.ice", 3, 1, ["legacy-unknown"]),
            "null-known" => new("item.ice", 3, null, ["purchased"]),
            "empty-sources" => new("item.ice", 3, 0, []),
            _ => new("item.ice", 0, 1, ["purchased"])
        };
        var bad = QuotedTradeExecutionTests.WithShipModules(good, m => m.ModuleId == Cargo ? m with { Cargo = [badStack] } : m);
        var before = JsonSerializer.Serialize(Stack(engine));
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, true));
        Assert.Equal(before, JsonSerializer.Serialize(Stack(engine)));
    }

    [Fact]
    public void New_scenario_bootstraps_exact_basis_and_source_while_overflow_is_atomic()
    {
        using var engine = new SimulationEngine(QuotedTradeExecutionTests.Registry);
        engine.LoadScenario(Template(0, new("item.ice", 3)));
        var stack = Stack(engine);
        long price = QuotedTradeExecutionTests.Registry.ItemTypes.GetDefinition(QuotedTradeExecutionTests.Registry.ItemTypes.GetIndex("item.ice")).BasePriceCredits!.Value;
        Assert.Equal(3 * price, stack.CostBasisCredits);
        Assert.Equal(new[] { "bootstrap" }, stack.AcquisitionSources);
        var before = JsonSerializer.Serialize(stack);
        var overflow = Template(0, new("item.ice", long.MaxValue));
        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(overflow));
        Assert.Contains("overflow", error.Message);
        Assert.Equal(before, JsonSerializer.Serialize(Stack(engine)));
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(Template(0, new("item.ice", 3, null, ["legacy-unknown"]))));
    }
    [Fact]
    public void New_scenario_without_positive_catalog_price_requires_explicit_cost()
    {
        var source = QuotedTradeExecutionTests.Registry;
        var registry = GameDataRegistry.Create(
            Enumerable.Range(0, source.ModuleCategories.Count).Select(source.ModuleCategories.GetDefinition),
            Enumerable.Range(0, source.ModuleTypes.Count).Select(source.ModuleTypes.GetDefinition),
            [new ItemTypeDefinition("item.ice", "Ice", 1)],
            Enumerable.Range(0, source.CommandDefinitions.Count).Select(source.CommandDefinitions.GetDefinition),
            shipClasses: Enumerable.Range(0, source.ShipClasses.Count).Select(source.ShipClasses.GetDefinition));
        var save = Template(0, new("item.ice", 3));
        var ship = save.GameState.SpaceObjects.Single(o => o.ObjectId == QuotedTradeExecutionTests.ShipId);
        save = save with
        {
            GameState = save.GameState with
            {
                CatalogCompatibility = null,
                DefenseState = null,
                CombatState = null,
                SpaceObjects = [ship with { IsDocked = false, DockedStationObjectId = null,
                Modules = ship.Modules!.Where(m => m.ModuleId == Cargo).ToArray() }]
            }
        };
        using var engine = new SimulationEngine(registry);
        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(save));
        Assert.Contains("positive base price", error.Message);
        save = QuotedTradeExecutionTests.WithShipModules(save, m => m with { Cargo = [new("item.ice", 3, 0, ["produced"])] });
        engine.LoadScenario(save);
        Assert.Equal(0, Stack(engine).CostBasisCredits);
        Assert.Equal(new[] { "produced" }, Stack(engine).AcquisitionSources);
    }

}
