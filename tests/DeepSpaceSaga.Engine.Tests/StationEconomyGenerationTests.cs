using System.Collections.Immutable;
using System.Linq;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Batch 2 (Trade economy generation, story-20260822-193700.md): deterministic generation
/// of station Credits/PriceCoefficient/Inventory (Documentation\02-FirstRelease\Mechanics\
/// {Money,StationInventory}.md) and PlayerCredits, wired through LoadScenario/CaptureSaveState.
/// </summary>
public class StationEconomyGenerationTests
{
    private const string StationObjectId = "STN-0001";
    private const string ShipObjectId = "SHIP";

    private static string ScenarioJson(
        ulong? masterSeed = null,
        long? credits = null,
        int? priceCoefficient = null,
        string? inventoryJson = null,
        long? playerCredits = null)
    {
        string masterSeedField = masterSeed is { } seed ? $"\"masterSeed\": {seed}," : "";
        string creditsField = credits is { } c ? $"\"credits\": {c}," : "";
        string coefficientField = priceCoefficient is { } pc ? $"\"priceCoefficient\": {pc}," : "";
        string inventoryField = inventoryJson is not null ? $"\"inventory\": {inventoryJson}," : "";
        string playerTokensField = playerCredits is { } pcr ? $"\"playerTokens\": {pcr}," : "";

        return $$"""
        {
          "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
          "gameState": {
            {{masterSeedField}}
            {{playerTokensField}}
            "gameTimeMs": 0, "currentSpeed": "Speed0",
            "playerShipObjectId": "{{ShipObjectId}}",
            "spaceObjects": [
              { "objectId": "{{ShipObjectId}}", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" },
              { "objectId": "{{StationObjectId}}", "objectType": "Station", "persistenceType": "Permanent",
                {{creditsField}}
                {{coefficientField}}
                {{inventoryField}}
                "positionX": 1000, "positionY": 1000, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" }
            ]
          }
        }
        """;
    }

    private static GameDataRegistry LoadRealRegistry()
    {
        string settingsPath = ResolveRealSettingsPath();
        return EngineContentLoader.LoadRegistryFromSettingsFile(settingsPath, out _, out _);
    }

    private static SimulationEngine CreateEngine(string scenarioJson)
    {
        var registry = LoadRealRegistry();
        var engine = new SimulationEngine(registry);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(scenarioJson));
        return engine;
    }

    [Fact]
    public void Generated_station_gets_credits_coefficient_and_inventory_within_documented_ranges()
    {
        var registry = LoadRealRegistry();
        var engine = new SimulationEngine(registry);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioJson(masterSeed: 42UL)));
        string[] tradeableItemTypeIds = Enumerable.Range(0, registry.ItemTypes.Count)
            .Select(registry.ItemTypes.GetDefinition)
            .Where(item => item.BasePriceCredits is not null)
            .Select(item => item.TypeId)
            .ToArray();

        var station = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationObjectId);

        Assert.InRange(station.Credits, 10_000, 50_000);
        Assert.InRange(station.PriceCoefficient, 500, 2000);

        Assert.Equal(tradeableItemTypeIds.Length, station.Inventory.Length);
        foreach (var itemTypeId in tradeableItemTypeIds)
        {
            int index = registry.ItemTypes.GetIndex(itemTypeId);
            var entry = station.Inventory.Single(i => i.ItemTypeIndex == index);
            Assert.InRange(entry.StockQuantity, 20, 500);
        }
    }

    [Fact]
    public void Same_masterSeed_produces_identical_generated_station_economy_twice()
    {
        var engine1 = CreateEngine(ScenarioJson(masterSeed: 777UL));
        var engine2 = CreateEngine(ScenarioJson(masterSeed: 777UL));

        var station1 = engine1.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationObjectId);
        var station2 = engine2.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationObjectId);

        Assert.Equal(station1.Credits, station2.Credits);
        Assert.Equal(station1.PriceCoefficient, station2.PriceCoefficient);

        var inventory1 = station1.Inventory.OrderBy(i => i.ItemTypeIndex).ToArray();
        var inventory2 = station2.Inventory.OrderBy(i => i.ItemTypeIndex).ToArray();
        Assert.Equal(inventory1.Length, inventory2.Length);
        for (int i = 0; i < inventory1.Length; i++)
        {
            Assert.Equal(inventory1[i].ItemTypeIndex, inventory2[i].ItemTypeIndex);
            Assert.Equal(inventory1[i].StockQuantity, inventory2[i].StockQuantity);
        }
    }

    [Fact]
    public void Different_masterSeed_changes_at_least_one_generated_value()
    {
        var engine1 = CreateEngine(ScenarioJson(masterSeed: 1UL));
        var engine2 = CreateEngine(ScenarioJson(masterSeed: 2UL));

        var station1 = engine1.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationObjectId);
        var station2 = engine2.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationObjectId);

        var inventory1 = station1.Inventory.OrderBy(i => i.ItemTypeIndex).Select(i => i.StockQuantity).ToArray();
        var inventory2 = station2.Inventory.OrderBy(i => i.ItemTypeIndex).Select(i => i.StockQuantity).ToArray();

        bool anyDifferent = station1.Credits != station2.Credits
            || station1.PriceCoefficient != station2.PriceCoefficient
            || !inventory1.SequenceEqual(inventory2);

        Assert.True(anyDifferent, "Expected at least one generated value to differ across different masterSeed values (sanity check RNG is actually used).");
    }

    [Fact]
    public void Explicit_credits_priceCoefficient_and_inventory_are_used_as_is_and_ignore_masterSeed()
    {
        const long explicitCredits = 12345;
        const int explicitCoefficient = 1500;
        const string explicitInventoryJson = """
        [
          { "itemTypeId": "item.energy-cells", "quantity": 111 },
          { "itemTypeId": "item.fuel", "quantity": 222 },
          { "itemTypeId": "item.ice", "quantity": 333 }
        ]
        """;

        var engineA = CreateEngine(ScenarioJson(
            masterSeed: 1UL, credits: explicitCredits, priceCoefficient: explicitCoefficient,
            inventoryJson: explicitInventoryJson));
        var engineB = CreateEngine(ScenarioJson(
            masterSeed: 999999UL, credits: explicitCredits, priceCoefficient: explicitCoefficient,
            inventoryJson: explicitInventoryJson));

        var stationA = engineA.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationObjectId);
        var stationB = engineB.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationObjectId);

        Assert.Equal(explicitCredits, stationA.Credits);
        Assert.Equal(explicitCoefficient, stationA.PriceCoefficient);
        Assert.Equal(explicitCredits, stationB.Credits);
        Assert.Equal(explicitCoefficient, stationB.PriceCoefficient);

        var registry = LoadRealRegistry();
        long EnergyCellsQuantity(SimulationEngine engine)
        {
            var station = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationObjectId);
            int index = registry.ItemTypes.GetIndex("item.energy-cells");
            return station.Inventory.Single(i => i.ItemTypeIndex == index).StockQuantity;
        }

        Assert.Equal(111, EnergyCellsQuantity(engineA));
        Assert.Equal(111, EnergyCellsQuantity(engineB));
    }

    [Fact]
    public void Save_then_load_does_not_regenerate_already_resolved_station_economy()
    {
        var engine = CreateEngine(ScenarioJson(masterSeed: 55UL));

        var save1 = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var savedStation1 = save1.GameState.SpaceObjects.Single(o => o.ObjectId == StationObjectId);

        // First resolve must have materialized explicit (non-null) values into the save.
        Assert.NotNull(savedStation1.Credits);
        Assert.NotNull(savedStation1.PriceCoefficient);
        Assert.NotNull(savedStation1.Inventory);

        var registry = LoadRealRegistry();
        var loadedEngine = new SimulationEngine(registry);
        loadedEngine.LoadScenario(save1);

        var save2 = loadedEngine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var savedStation2 = save2.GameState.SpaceObjects.Single(o => o.ObjectId == StationObjectId);

        Assert.Equal(savedStation1.Credits, savedStation2.Credits);
        Assert.Equal(savedStation1.PriceCoefficient, savedStation2.PriceCoefficient);
        Assert.Equal(
            savedStation1.Inventory!.OrderBy(i => i.ItemTypeId).Select(i => (i.ItemTypeId, i.Quantity)),
            savedStation2.Inventory!.OrderBy(i => i.ItemTypeId).Select(i => (i.ItemTypeId, i.Quantity)));
    }

    [Fact]
    public void PlayerCredits_defaults_to_zero_when_not_specified_in_scenario()
    {
        var engine = CreateEngine(ScenarioJson(masterSeed: 1UL));

        Assert.Equal(0, engine.PlayerCredits);
    }

    [Fact]
    public void PlayerCredits_uses_explicit_value_when_specified()
    {
        var engine = CreateEngine(ScenarioJson(masterSeed: 1UL, playerCredits: 5000));

        Assert.Equal(5000, engine.PlayerCredits);
    }

    [Fact]
    public void PlayerCredits_round_trips_through_save_and_load()
    {
        var engine = CreateEngine(ScenarioJson(masterSeed: 1UL, playerCredits: 7500));

        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        Assert.Equal(7500, save.GameState.PlayerTokens);

        var registry = LoadRealRegistry();
        var loadedEngine = new SimulationEngine(registry);
        loadedEngine.LoadScenario(save);

        Assert.Equal(7500, loadedEngine.PlayerCredits);
    }

    private const string ProfileSupplyItemId = "item.profile-supply";
    private const string ProfileDemandItemId = "item.profile-demand";
    private const string ProfileExtraItemId = "item.profile-extra";
    private const string ProfileOutsideItemId = "item.profile-outside";
    private const string FuelItemId = "item.fuel";
    private const string CargoModuleId = "CARGO";
    private const string EngineModuleId = "ENGINE";

    private static readonly string[] ProfileIds =
        ["market.mining", "market.agricultural", "market.industrial", "market.high-tech", "market.transit"];

    private static GameDataRegistry CreateProfileRegistry()
    {
        string[] cargoCommands = [TradeCommandTypes.Buy, TradeCommandTypes.Sell];
        string[] engineCommands = [TradeCommandTypes.Refuel];
        var categories = new[]
        {
            new ModuleCategoryDefinition("module.container", "Container", 1, cargoCommands.ToImmutableArray()),
            new ModuleCategoryDefinition("module.engine", "Engine", 1, engineCommands.ToImmutableArray()),
        };
        var modules = new[]
        {
            new ModuleTypeDefinition("module.container.basic", "Container", 1, 100, 100, 0,
                cargoCommands.ToImmutableArray(), CargoCapacityKg: 10_000, BaseCycleTimeMs: 1000),
            new ModuleTypeDefinition("module.engine.basic", "Engine", 1, 100, 100, 0,
                engineCommands.ToImmutableArray(), FuelCapacityKg: 1_000, BaseCycleTimeMs: 1000),
        };
        var items = new[]
        {
            new ItemTypeDefinition(ProfileSupplyItemId, "Supply", 1, 10),
            new ItemTypeDefinition(ProfileDemandItemId, "Demand", 1, 20),
            new ItemTypeDefinition(ProfileExtraItemId, "Extra", 1, 30),
            new ItemTypeDefinition(ProfileOutsideItemId, "Outside", 1, 40),
            new ItemTypeDefinition(FuelItemId, "Fuel", 0, 5, TradeUnit: TradeUnit.Kilogram,
                StorageKind: ItemStorageKind.FuelTank),
        };
        var commands = new[]
        {
            new CommandDefinition(TradeCommandTypes.Buy, "Buy", Target: "none", Type: "module.container"),
            new CommandDefinition(TradeCommandTypes.Sell, "Sell", Target: "none", Type: "module.container"),
            new CommandDefinition(TradeCommandTypes.Refuel, "Refuel", Target: "none", Type: "module.engine"),
        };
        var sizeFactors = new Dictionary<StationSize, int>
        {
            [StationSize.Outpost] = 500,
            [StationSize.Medium] = 1000,
            [StationSize.Large] = 1500,
            [StationSize.Huge] = 2000,
        }.ToImmutableDictionary();
        var profiles = ProfileIds.Select((id, index) => new StationMarketProfileDefinition(
            id,
            id,
            ImmutableArray.Create(ProfileSupplyItemId),
            ImmutableArray.Create(ProfileDemandItemId),
            ImmutableArray.Create(
                new StationMarketStockDefinition(ProfileSupplyItemId, 10 + index),
                new StationMarketStockDefinition(ProfileDemandItemId, 20 + index)),
            InitialCredits: 1000 + index,
            RefuelStockKg: 30 + index,
            sizeFactors)).ToArray();

        GameDataRegistry Build(string? legacyFingerprint) => GameDataRegistry.Create(
            categories, modules, items, commands,
            legacyCatalogFingerprint: legacyFingerprint,
            stationMarketProfiles: profiles);
        var current = Build(null);
        return Build(current.CatalogCompatibility.Fingerprint);
    }

    private static string ProfileScenarioJson(
        string? profileId = "market.mining",
        string? stationSize = "Large",
        long? credits = null,
        IEnumerable<(string ItemTypeId, long Quantity)>? inventory = null,
        string? fingerprint = null,
        bool profileOnShip = false)
    {
        string inventoryField = inventory is null ? "" :
            $"\"inventory\": [{string.Join(",", inventory.Select(i => $$"""{ "itemTypeId": "{{i.ItemTypeId}}", "quantity": {{i.Quantity}} }"""))}],";
        string creditsField = credits is null ? "" : $"\"credits\": {credits},";
        string sizeField = stationSize is null ? "" : $"\"stationSize\": \"{stationSize}\",";
        string profileField = profileId is null ? "" : $"\"marketProfileId\": \"{profileId}\",";
        string fingerprintField = fingerprint is null ? "" : $"\"marketProfileFingerprint\": \"{fingerprint}\",";
        string shipProfileField = profileOnShip ? profileField : "";
        string stationProfileField = profileOnShip ? "" : profileField;

        return $$"""
        {
          "scenarioMetadata": { "scenarioId": "profile-test", "name": "Profile Test" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed0", "playerShipObjectId": "{{ShipObjectId}}",
            "masterSeed": 42, "playerTokens": 100000,
            "spaceObjects": [
              { "objectId": "{{ShipObjectId}}", "objectType": "PlayerShip", "persistenceType": "Permanent",
                {{shipProfileField}}
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0, "movementType": "Stationary",
                "isDocked": true, "dockedStationObjectId": "{{StationObjectId}}",
                "hullLayout": { "width": 2, "height": 1, "cells": [ {"x":0,"y":0}, {"x":1,"y":0} ] },
                "modules": [
                  { "moduleId": "{{CargoModuleId}}", "moduleTypeId": "module.container.basic",
                    "occupiedCells": [ {"x":0,"y":0} ], "structurePoints": 100,
                    "powerState": "On", "operationalState": "Ready", "cargo": [] },
                  { "moduleId": "{{EngineModuleId}}", "moduleTypeId": "module.engine.basic",
                    "occupiedCells": [ {"x":1,"y":0} ], "structurePoints": 100,
                    "powerState": "On", "operationalState": "Ready", "cargo": [], "fuelAmountKg": 0 }
                ] },
              { "objectId": "{{StationObjectId}}", "objectType": "Station", "persistenceType": "Permanent",
                {{stationProfileField}}{{fingerprintField}}{{creditsField}}{{sizeField}}{{inventoryField}}
                "positionX": 1, "positionY": 0, "speedMps": 0, "directionDegrees": 0, "movementType": "Stationary" }
            ]
          }
        }
        """;
    }

    private static (SimulationEngine Engine, GameDataRegistry Registry) CreateProfileEngine(
        string profileId = "market.mining",
        long? credits = null,
        IEnumerable<(string ItemTypeId, long Quantity)>? inventory = null)
    {
        var registry = CreateProfileRegistry();
        var engine = new SimulationEngine(registry);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ProfileScenarioJson(profileId, credits: credits, inventory: inventory)));
        return (engine, registry);
    }

    private static long Stock(SpaceObjectRuntime station, GameDataRegistry registry, string itemTypeId) =>
        station.Inventory.Single(item => item.ItemTypeIndex == registry.ItemTypes.GetIndex(itemTypeId)).StockQuantity;

    [Fact]
    public void Profile_bootstrap_uses_only_profile_inventory_and_budget()
    {
        var (engine, registry) = CreateProfileEngine(inventory: []);
        var station = engine.RuntimeObjects.Single(obj => obj.InitialMotion.ObjectId == StationObjectId);

        Assert.Equal(1500, station.Credits);
        Assert.Equal("market.mining", station.MarketProfileId);
        Assert.Equal(registry.StationMarketProfiles.GetDefinition(0).Fingerprint, station.MarketProfileFingerprint);
        Assert.Equal(new[] { FuelItemId, ProfileDemandItemId, ProfileSupplyItemId },
            station.Inventory.Select(item => registry.ItemTypes.GetDefinition(item.ItemTypeIndex).TypeId));
        Assert.Equal(15, Stock(station, registry, ProfileSupplyItemId));
        Assert.Equal(30, Stock(station, registry, ProfileDemandItemId));
        Assert.Equal(45, Stock(station, registry, FuelItemId));
        Assert.DoesNotContain(station.Inventory, item =>
            registry.ItemTypes.GetDefinition(item.ItemTypeIndex).TypeId == ProfileOutsideItemId);
    }

    [Fact]
    public void Explicit_zero_extra_item_and_credits_override_profile()
    {
        var (engine, registry) = CreateProfileEngine(credits: 0,
            inventory: [(ProfileSupplyItemId, 0), (ProfileExtraItemId, 777)]);
        var station = engine.RuntimeObjects.Single(obj => obj.InitialMotion.ObjectId == StationObjectId);

        Assert.Equal(0, station.Credits);
        Assert.Equal(0, Stock(station, registry, ProfileSupplyItemId));
        Assert.Equal(777, Stock(station, registry, ProfileExtraItemId));
        Assert.Equal(30, Stock(station, registry, ProfileDemandItemId));
        Assert.Equal(45, Stock(station, registry, FuelItemId));
    }

    [Theory]
    [InlineData("Default")]
    [InlineData("Docked")]
    public void Legacy_default_and_docked_keep_required_explicit_stocks(string scenarioName)
    {
        string settingsPath = ResolveRealSettingsPath();
        string path = Path.Combine(Path.GetDirectoryName(settingsPath)!, "Scenarios", scenarioName, "scenario.json");
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(settingsPath, path);
        var registry = LoadRealRegistry();
        var station = engine.RuntimeObjects.Single(obj => obj.InitialMotion.ObjectId == "SPC-0002");
        var expected = new Dictionary<string, long>
        {
            ["item.food-rations"] = 500, ["item.energy-cells"] = 350, ["item.fuel"] = 700,
            ["item.ice"] = 320, ["item.iron-ore"] = 410, ["item.silicon"] = 70,
            ["item.magnesium-ore"] = 120,
        };

        Assert.Equal(StationSize.Large, station.StationSize);
        Assert.Null(station.MarketProfileId);
        foreach (var pair in expected)
            Assert.Equal(pair.Value, Stock(station, registry, pair.Key));
    }

    [Fact]
    public void Invalid_profile_reference_preserves_running_world()
    {
        var (engine, registry) = CreateProfileEngine();
        string before = ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0));
        var valid = ScenarioLoader.LoadFromJson(ProfileScenarioJson());
        var station = valid.GameState.SpaceObjects.Single(obj => obj.ObjectId == StationObjectId);
        var ship = valid.GameState.SpaceObjects.Single(obj => obj.ObjectId == ShipObjectId);
        ScenarioFile WithObjects(params SpaceObjectData[] objects) => valid with
            { GameState = valid.GameState with { SpaceObjects = objects } };
        var invalidScenarios = new[]
        {
            WithObjects(ship, station with { MarketProfileId = "market.unknown" }),
            WithObjects(ship with { MarketProfileId = "market.mining" }, station),
            WithObjects(ship, station with { Inventory =
                [new StationInventoryItemData(ProfileSupplyItemId, 1), new StationInventoryItemData(ProfileSupplyItemId, 2)] }),
            WithObjects(ship, station with { MarketProfileFingerprint = "changed" }),
        };

        foreach (var invalid in invalidScenarios)
        {
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(invalid));
            Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0)));
        }
        Assert.Equal(registry.StationMarketProfiles.GetDefinition(0).Fingerprint,
            engine.RuntimeObjects.Single(obj => obj.InitialMotion.ObjectId == StationObjectId).MarketProfileFingerprint);
    }

    [Theory]
    [InlineData("market.mining")]
    [InlineData("market.agricultural")]
    [InlineData("market.industrial")]
    [InlineData("market.high-tech")]
    [InlineData("market.transit")]
    public void Each_profile_supports_buy_sell_and_refuel(string profileId)
    {
        var (engine, registry) = CreateProfileEngine(profileId);
        int supplyIndex = registry.ItemTypes.GetIndex(ProfileSupplyItemId);
        int fuelIndex = registry.ItemTypes.GetIndex(FuelItemId);
        var initialSnapshot = engine.CaptureSnapshotForTests();
        long supplyPrice = initialSnapshot.DockedStationTrade!.Items.Single(item => item.ItemTypeId == ProfileSupplyItemId).UnitPriceCredits;
        long fuelPrice = initialSnapshot.DockedStationTrade.Items.Single(item => item.ItemTypeId == FuelItemId).UnitPriceCredits;
        long initialPlayerCredits = engine.PlayerCredits;
        var initialStation = engine.RuntimeObjects.Single(obj => obj.InitialMotion.ObjectId == StationObjectId);
        long initialStationCredits = initialStation.Credits;
        long initialSupply = Stock(initialStation, registry, ProfileSupplyItemId);
        long initialFuel = Stock(initialStation, registry, FuelItemId);

        engine.ReceiveCommand(new PlayerCommand($"{profileId}-buy", 1, ShipObjectId, CargoModuleId,
            TradeCommandTypes.Buy, ItemTypeId: ProfileSupplyItemId, Quantity: 1));
        var buy = engine.CaptureSnapshotForTests();
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(buy.CommandResults).Status);
        Assert.Null(buy.CommandResults[0].ExecutedQuantity);
        var stationAfterBuy = engine.RuntimeObjects.Single(obj => obj.InitialMotion.ObjectId == StationObjectId);
        var shipAfterBuy = engine.RuntimeObjects.Single(obj => obj.InitialMotion.ObjectId == ShipObjectId);
        Assert.Equal(initialPlayerCredits - supplyPrice, engine.PlayerCredits);
        Assert.Equal(initialStationCredits + supplyPrice, stationAfterBuy.Credits);
        Assert.Equal(initialSupply - 1, Stock(stationAfterBuy, registry, ProfileSupplyItemId));
        Assert.Equal(1, shipAfterBuy.Modules.Single(module => module.ModuleId == CargoModuleId).Cargo
            .Single(stack => stack.ItemTypeIndex == supplyIndex).Quantity);

        engine.ReceiveCommand(new PlayerCommand($"{profileId}-sell", 2, ShipObjectId, CargoModuleId,
            TradeCommandTypes.Sell, ItemTypeId: ProfileSupplyItemId, Quantity: 1));
        var sell = engine.CaptureSnapshotForTests();
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(sell.CommandResults).Status);
        Assert.Null(sell.CommandResults[0].ExecutedQuantity);
        var stationAfterSell = engine.RuntimeObjects.Single(obj => obj.InitialMotion.ObjectId == StationObjectId);
        var shipAfterSell = engine.RuntimeObjects.Single(obj => obj.InitialMotion.ObjectId == ShipObjectId);
        Assert.Equal(initialPlayerCredits, engine.PlayerCredits);
        Assert.Equal(initialStationCredits, stationAfterSell.Credits);
        Assert.Equal(initialSupply, Stock(stationAfterSell, registry, ProfileSupplyItemId));
        Assert.DoesNotContain(shipAfterSell.Modules.Single(module => module.ModuleId == CargoModuleId).Cargo,
            stack => stack.ItemTypeIndex == supplyIndex);

        engine.ReceiveCommand(new PlayerCommand($"{profileId}-refuel", 3, ShipObjectId, EngineModuleId,
            TradeCommandTypes.Refuel, ItemTypeId: FuelItemId, Quantity: 1));
        var refuel = engine.CaptureSnapshotForTests();
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(refuel.CommandResults).Status);
        Assert.Null(refuel.CommandResults[0].ExecutedQuantity);
        var stationAfterRefuel = engine.RuntimeObjects.Single(obj => obj.InitialMotion.ObjectId == StationObjectId);
        var shipAfterRefuel = engine.RuntimeObjects.Single(obj => obj.InitialMotion.ObjectId == ShipObjectId);
        Assert.Equal(initialPlayerCredits - fuelPrice, engine.PlayerCredits);
        Assert.Equal(initialStationCredits + fuelPrice, stationAfterRefuel.Credits);
        Assert.Equal(initialFuel - 1, Stock(stationAfterRefuel, registry, FuelItemId));
        Assert.Equal(1, shipAfterRefuel.Modules.Single(module => module.ModuleId == EngineModuleId).FuelAmountKg);
        Assert.Equal(fuelIndex, stationAfterRefuel.Inventory.Single(item => item.ItemTypeIndex == fuelIndex).ItemTypeIndex);
    }

    [Fact]
    public void Non_profile_item_cannot_be_traded()
    {
        var (engine, registry) = CreateProfileEngine();
        var stationBefore = engine.RuntimeObjects.Single(obj => obj.InitialMotion.ObjectId == StationObjectId);
        var stocksBefore = stationBefore.Inventory;
        long playerCreditsBefore = engine.PlayerCredits;
        long stationCreditsBefore = stationBefore.Credits;

        engine.ReceiveCommand(new PlayerCommand("outside-buy", 1, ShipObjectId, CargoModuleId,
            TradeCommandTypes.Buy, ItemTypeId: ProfileOutsideItemId, Quantity: 1));
        var snapshot = engine.CaptureSnapshotForTests();
        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.UnknownItemType, result.ReasonCode);
        var stationAfter = engine.RuntimeObjects.Single(obj => obj.InitialMotion.ObjectId == StationObjectId);
        Assert.Equal(playerCreditsBefore, engine.PlayerCredits);
        Assert.Equal(stationCreditsBefore, stationAfter.Credits);
        Assert.Equal(stocksBefore, stationAfter.Inventory);
        Assert.DoesNotContain(stationAfter.Inventory,
            item => item.ItemTypeIndex == registry.ItemTypes.GetIndex(ProfileOutsideItemId));
    }

    [Fact]
    public void Profile_save_roundtrip_preserves_post_trade_state()
    {
        var (engine, registry) = CreateProfileEngine(inventory: [(ProfileExtraItemId, 0)]);
        engine.ReceiveCommand(new PlayerCommand("roundtrip-buy", 1, ShipObjectId, CargoModuleId,
            TradeCommandTypes.Buy, ItemTypeId: ProfileSupplyItemId, Quantity: 1));
        engine.CaptureSnapshotForTests();
        engine.ReceiveCommand(new PlayerCommand("roundtrip-refuel", 2, ShipObjectId, EngineModuleId,
            TradeCommandTypes.Refuel, ItemTypeId: FuelItemId, Quantity: 1));
        engine.CaptureSnapshotForTests();
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var savedStation = save.GameState.SpaceObjects.Single(obj => obj.ObjectId == StationObjectId);

        Assert.Equal(8, save.SaveFormatVersion);
        Assert.NotNull(savedStation.MarketProfileId);
        Assert.NotNull(savedStation.MarketProfileFingerprint);
        Assert.Contains(savedStation.Inventory!, item => item.ItemTypeId == ProfileExtraItemId && item.Quantity == 0);
        using var loaded = new SimulationEngine(registry);
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), allowNonZeroGameTime: true), isSave: true);
        var reSaved = loaded.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var loadedStation = reSaved.GameState.SpaceObjects.Single(obj => obj.ObjectId == StationObjectId);

        Assert.Equal(savedStation.MarketProfileId, loadedStation.MarketProfileId);
        Assert.Equal(savedStation.MarketProfileFingerprint, loadedStation.MarketProfileFingerprint);
        Assert.Equal(savedStation.Credits, loadedStation.Credits);
        Assert.Equal(savedStation.StationSize, loadedStation.StationSize);
        Assert.Equal(savedStation.Inventory!.Select(item => (item.ItemTypeId, item.Quantity)),
            loadedStation.Inventory!.Select(item => (item.ItemTypeId, item.Quantity)));
        Assert.Equal(engine.PlayerCredits, loaded.PlayerCredits);
    }

    [Fact]
    public void Changed_or_missing_profile_stamp_or_inventory_is_rejected()
    {
        var (engine, registry) = CreateProfileEngine();
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var station = save.GameState.SpaceObjects.Single(obj => obj.ObjectId == StationObjectId);
        var ship = save.GameState.SpaceObjects.Single(obj => obj.ObjectId == ShipObjectId);
        ScenarioFile WithStation(SpaceObjectData replacement) => save with
            { GameState = save.GameState with { SpaceObjects = [ship, replacement] } };
        var invalidSaves = new[]
        {
            WithStation(station with { MarketProfileFingerprint = "changed" }),
            WithStation(station with { MarketProfileFingerprint = null }),
            WithStation(station with { Inventory = station.Inventory!.Where(item => item.ItemTypeId != FuelItemId).ToArray() }),
            WithStation(station with { Inventory = null }),
        };

        foreach (var invalid in invalidSaves)
        {
            using var loaded = new SimulationEngine(registry);
            Assert.Throws<ScenarioException>(() => loaded.LoadScenario(invalid, isSave: true));
        }
    }

    [Fact]
    public void Legacy_unprofiled_save_remains_compatible_with_matching_catalog()
    {
        var registry = CreateProfileRegistry();
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ProfileScenarioJson(profileId: null)));
        var captured = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var legacy = captured with
        {
            SaveFormatVersion = 6,
            GameState = captured.GameState with { CatalogCompatibility = null },
        };

        using var loaded = new SimulationEngine(registry);
        loaded.LoadScenario(legacy, isSave: true);
        Assert.Equal(engine.PlayerCredits, loaded.PlayerCredits);
        Assert.All(loaded.RuntimeObjects.Where(obj => obj.ObjectType == SpaceObjectType.Station),
            station => Assert.Null(station.MarketProfileId));
    }

    private static string ResolveRealSettingsPath()
    {
        string settingsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DeepSpaceSaga.Client", "Settings.json"));

        if (!File.Exists(settingsPath))
        {
            settingsPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "Settings.json"));
        }

        return settingsPath;
    }
}
