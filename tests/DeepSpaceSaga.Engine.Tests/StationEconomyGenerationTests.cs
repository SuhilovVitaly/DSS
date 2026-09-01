using System.Linq;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Batch 2 (Trade economy generation, story-20260822-193700.md): deterministic generation
/// of station Credits/PriceCoefficient/Inventory (Docs\FirstRelease\Mechanics\
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

    // Story-20260825-084409 Batch 1 (U1): the real catalog grew from 3 to 10 tradeable items
    // (Docs\FirstRelease\TechnicalTasks\StationEconomyProductionAndSizing.md "Номенклатура") —
    // the fallback generator (SimulationEngine.ResolveStationInventory, Protect list, untouched
    // logic) still generates one entry per item type that carries a BasePriceCredits, which is
    // now all 10.
    private static string[] TradeableItemTypeIds =
    {
        "item.ice", "item.iron-ore", "item.silicon", "item.magnesium-ore",
        "item.water", "item.steel", "item.energy-cells", "item.fuel",
        "item.protein-mass", "item.food-rations"
    };

    [Fact]
    public void Generated_station_gets_credits_coefficient_and_inventory_within_documented_ranges()
    {
        var engine = CreateEngine(ScenarioJson(masterSeed: 42UL));

        var station = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationObjectId);

        Assert.InRange(station.Credits, 10_000, 50_000);
        Assert.InRange(station.PriceCoefficient, 500, 2000);

        Assert.Equal(TradeableItemTypeIds.Length, station.Inventory.Length);
        foreach (var itemTypeId in TradeableItemTypeIds)
        {
            var registry = LoadRealRegistry();
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
