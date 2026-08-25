using System.Collections.Immutable;
using System.Linq;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Story-20260825-084409 Batch 2 — requirements §59, Docs\FirstRelease\TechnicalTasks\
/// StationEconomyProductionAndSizing.md:
/// U5 (StationSize persistence + fallback, explicit-wins starting inventory),
/// U6 (ProducingStationModule type catalog: RecipeMaterial.NeedCoefficient),
/// U7 (station producing-module instances + ConsumedResource price-factor resolver),
/// U8 (station events/buffs — schema + persistence + participation in the price formula,
/// no triggering engine, CP-3).
/// </summary>
public class StationEconomyBatch2Tests
{
    private const string PlayerShipId = "SPC-0001";
    private const string CargoModuleId = "MOD-CARGO-01";
    private const string StationId = "STATION-01";

    private const string IceId = "item.ice";
    private const string IronOreId = "item.iron-ore";
    private const string EnergyCellsId = "item.energy-cells";

    private const string SmelterFactoryTypeId = "factory.smelter";

    private static GameDataRegistry CreateRegistry()
    {
        string[] cargoCommandIds = [TradeCommandTypes.Buy, TradeCommandTypes.Sell];

        return GameDataRegistry.Create(
            [
                new ModuleCategoryDefinition(
                    "module.container", "Container", SlotSize: 1, CommandTypeIds: cargoCommandIds.ToImmutableArray())
            ],
            [
                new ModuleTypeDefinition(
                    "module.container.basic", "Container", SlotSize: 1, MassKg: 20000,
                    StructurePointsMax: 400, PowerConsumptionW: 0,
                    CommandTypeIds: cargoCommandIds.ToImmutableArray(),
                    CargoCapacityKg: 1000,
                    BaseCycleTimeMs: 1000)
            ],
            [
                // Round base prices (100) deliberately avoid rounding-tie ambiguity — this
                // file is about factor SELECTION (size/ConsumedResource/events), not the
                // rounding rule itself (already covered by StationPricingTests).
                new ItemTypeDefinition(IceId, "Ice", UnitMassKg: 1, BasePriceCredits: 100, Category: TradeCategory.Resource),
                new ItemTypeDefinition(IronOreId, "Iron Ore", UnitMassKg: 1, BasePriceCredits: 100, Category: TradeCategory.Resource),
                new ItemTypeDefinition(EnergyCellsId, "Energy Cells", UnitMassKg: 1, BasePriceCredits: 100, Category: TradeCategory.Good)
            ],
            [
                new CommandDefinition(TradeCommandTypes.Buy, "Buy", Target: "none", Type: "module.container"),
                new CommandDefinition(TradeCommandTypes.Sell, "Sell", Target: "none", Type: "module.container")
            ],
            factoryTypes:
            [
                new FactoryTypeDefinition(
                    SmelterFactoryTypeId, "Smelter",
                    new RecipeDefinition(
                        SmelterFactoryTypeId, "Smelter Recipe",
                        Inputs: ImmutableArray.Create(new RecipeMaterial(IronOreId, Count: 10, NeedCoefficient: 1200)),
                        Outputs: ImmutableArray.Create(new RecipeMaterial(EnergyCellsId, Count: 1)),
                        CycleDurationMs: 5000))
            ]);
    }

    private static string ScenarioJson(
        string? stationSizeJson = null,
        string? producingModulesJson = null,
        string? eventsJson = null,
        long stationCredits = 1_000_000)
    {
        string stationSizeField = stationSizeJson is not null ? $"\"stationSize\": {stationSizeJson}," : "";
        string producingModulesField = producingModulesJson is not null ? $"\"producingModules\": {producingModulesJson}," : "";
        string eventsField = eventsJson is not null ? $"\"events\": {eventsJson}," : "";

        return $$"""
        {
          "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
          "gameState": {
            "gameTimeMs": 0,
            "currentSpeed": "Speed0",
            "playerShipObjectId": "{{PlayerShipId}}",
            "spaceObjects": [
              {
                "objectId": "{{PlayerShipId}}",
                "objectType": "PlayerShip",
                "persistenceType": "Permanent",
                "positionX": 10000, "positionY": 10000,
                "speedMps": 0, "directionDegrees": 0, "movementType": "Stationary",
                "isDocked": true,
                "dockedStationObjectId": "{{StationId}}",
                "hullLayout": { "width": 1, "height": 1, "cells": [ {"x":0,"y":0} ] },
                "modules": [
                  {
                    "moduleId": "{{CargoModuleId}}",
                    "moduleTypeId": "module.container.basic",
                    "occupiedCells": [ {"x":0,"y":0} ],
                    "structurePoints": 400,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "activeCycle": null,
                    "cargo": []
                  }
                ]
              },
              {
                "objectId": "{{StationId}}",
                "objectType": "Station",
                "persistenceType": "Permanent",
                "credits": {{stationCredits}},
                {{stationSizeField}}
                {{producingModulesField}}
                {{eventsField}}
                "inventory": [
                  { "itemTypeId": "{{IceId}}", "quantity": 100 },
                  { "itemTypeId": "{{IronOreId}}", "quantity": 100 },
                  { "itemTypeId": "{{EnergyCellsId}}", "quantity": 100 }
                ],
                "positionX": 10001, "positionY": 10000,
                "speedMps": 0, "directionDegrees": 0, "movementType": "Stationary"
              }
            ]
          }
        }
        """;
    }

    private static SimulationEngine CreateEngine(string scenarioJson)
    {
        var engine = new SimulationEngine(CreateRegistry());
        engine.LoadScenario(ScenarioLoader.LoadFromJson(scenarioJson));
        return engine;
    }

    private static long UnitPriceOf(SimulationEngine engine, string itemTypeId)
    {
        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);
        return snapshot.DockedStationTrade!.Items.Single(i => i.ItemTypeId == itemTypeId).UnitPriceCredits;
    }

    // --- U5: StationSize persistence + fallback --------------------------------------------

    [Fact]
    public void Explicit_stationSize_is_used_as_is()
    {
        var engine = CreateEngine(ScenarioJson(stationSizeJson: "\"Huge\""));

        var station = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationId);
        Assert.Equal(StationSize.Huge, station.StationSize);
    }

    [Fact]
    public void Missing_stationSize_falls_back_to_Medium()
    {
        // §59 gives no guidance for an unspecified station; SimulationEngine.ResolveStationSize
        // documents Medium as the fallback (never RNG-resolved).
        var engine = CreateEngine(ScenarioJson());

        var station = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationId);
        Assert.Equal(StationSize.Medium, station.StationSize);
    }

    [Fact]
    public void Unknown_stationSize_value_throws_ScenarioException()
    {
        Assert.Throws<ScenarioException>(() => CreateEngine(ScenarioJson(stationSizeJson: "\"Gigantic\"")));
    }

    [Fact]
    public void StationSize_round_trips_through_save_and_load_without_changing()
    {
        var engine = CreateEngine(ScenarioJson(stationSizeJson: "\"Outpost\""));

        var save1 = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var savedStation1 = save1.GameState.SpaceObjects.Single(o => o.ObjectId == StationId);
        Assert.Equal("Outpost", savedStation1.StationSize);

        var reloaded = new SimulationEngine(CreateRegistry());
        reloaded.LoadScenario(save1);
        var save2 = reloaded.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var savedStation2 = save2.GameState.SpaceObjects.Single(o => o.ObjectId == StationId);

        Assert.Equal(savedStation1.StationSize, savedStation2.StationSize);
    }

    [Fact]
    public void Fallback_stationSize_materializes_explicitly_into_save()
    {
        // The fallback (Medium) is resolved once and then persisted explicitly — same
        // "resolved once, explicit from then on" shape as Credits/PriceCoefficient/Inventory,
        // even though it is never RNG-generated.
        var engine = CreateEngine(ScenarioJson());

        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var savedStation = save.GameState.SpaceObjects.Single(o => o.ObjectId == StationId);

        Assert.Equal("Medium", savedStation.StationSize);
    }

    // --- U5/U7 integration: size factor drives UnitPriceCredits (no producing modules) -----

    [Fact]
    public void Large_station_good_and_general_resource_prices_match_acceptance_criteria()
    {
        // §59 acceptance criteria: "Для Large станции Good получает коэффициент 1.10, общий
        // Resource получает 1.15". Base price 100 for every fixture item (see CreateRegistry).
        var engine = CreateEngine(ScenarioJson(stationSizeJson: "\"Large\""));

        Assert.Equal(110, UnitPriceOf(engine, EnergyCellsId)); // Good: 100 * 1.10
        Assert.Equal(115, UnitPriceOf(engine, IceId)); // general Resource (no producing module needs it): 100 * 1.15
    }

    // --- U6/U7: producing-module instances + ConsumedResource resolver ---------------------

    [Fact]
    public void IronOre_becomes_ConsumedResource_when_an_active_producing_module_needs_it()
    {
        // §59 acceptance criteria: "потребляемый Resource получает 1.25" on Large.
        string producingModules = $$"""[ { "producingModuleTypeId": "{{SmelterFactoryTypeId}}", "active": true } ]""";
        var engine = CreateEngine(ScenarioJson(stationSizeJson: "\"Large\"", producingModulesJson: producingModules));

        Assert.Equal(125, UnitPriceOf(engine, IronOreId)); // consumed Resource: 100 * 1.25
        Assert.Equal(115, UnitPriceOf(engine, IceId)); // Ice is not a smelter input -> stays general: 100 * 1.15
    }

    [Fact]
    public void Inactive_producing_module_does_not_make_its_input_a_ConsumedResource()
    {
        string producingModules = $$"""[ { "producingModuleTypeId": "{{SmelterFactoryTypeId}}", "active": false } ]""";
        var engine = CreateEngine(ScenarioJson(stationSizeJson: "\"Large\"", producingModulesJson: producingModules));

        Assert.Equal(115, UnitPriceOf(engine, IronOreId)); // inactive -> stays general Resource: 100 * 1.15
    }

    [Fact]
    public void ProducingModuleTypeId_active_defaults_to_true_when_omitted()
    {
        string producingModules = $$"""[ { "producingModuleTypeId": "{{SmelterFactoryTypeId}}" } ]""";
        var engine = CreateEngine(ScenarioJson(stationSizeJson: "\"Large\"", producingModulesJson: producingModules));

        Assert.Equal(125, UnitPriceOf(engine, IronOreId));
    }

    [Fact]
    public void Unknown_producingModuleTypeId_throws_ContentException()
    {
        string producingModules = """[ { "producingModuleTypeId": "factory.does-not-exist", "active": true } ]""";

        Assert.Throws<ContentException>(() => CreateEngine(ScenarioJson(producingModulesJson: producingModules)));
    }

    [Fact]
    public void ProducingModules_round_trip_through_save_and_load()
    {
        string producingModules = $$"""[ { "producingModuleTypeId": "{{SmelterFactoryTypeId}}", "active": false } ]""";
        var engine = CreateEngine(ScenarioJson(producingModulesJson: producingModules));

        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var savedStation = save.GameState.SpaceObjects.Single(o => o.ObjectId == StationId);

        var savedModule = Assert.Single(savedStation.ProducingModules!);
        Assert.Equal(SmelterFactoryTypeId, savedModule.ProducingModuleTypeId);
        Assert.False(savedModule.Active);

        var reloaded = new SimulationEngine(CreateRegistry());
        reloaded.LoadScenario(save);
        var station = reloaded.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationId);
        var runtimeModule = Assert.Single(station.ProducingModules);
        Assert.False(runtimeModule.Active);
    }

    // --- U6: RecipeMaterial.NeedCoefficient content shape -----------------------------------

    [Fact]
    public void RecipeMaterial_NeedCoefficient_defaults_to_neutral_1000()
    {
        var material = new RecipeMaterial(IronOreId, Count: 10);
        Assert.Equal(1000, material.NeedCoefficient);
    }

    [Fact]
    public void LoadFactoryTypes_parses_needCoefficient_on_inputs()
    {
        string json = $$"""
        {
          "factoryTypes": [
            {
              "typeId": "{{SmelterFactoryTypeId}}",
              "displayName": "Smelter",
              "recipe": {
                "inputs": [ { "itemTypeId": "{{IronOreId}}", "count": 10, "needCoefficient": 1.5 } ],
                "outputs": [ { "itemTypeId": "{{EnergyCellsId}}", "count": 1 } ],
                "cycleTimeMs": 5000
              }
            }
          ]
        }
        """;

        var path = WriteTempFile(json);
        try
        {
            var factoryTypes = EngineContentLoader.LoadFactoryTypes(path);
            var factoryType = Assert.Single(factoryTypes);
            var input = Assert.Single(factoryType.Recipe.Inputs);
            Assert.Equal(1500, input.NeedCoefficient);

            var output = Assert.Single(factoryType.Recipe.Outputs);
            Assert.Equal(1000, output.NeedCoefficient); // missing on outputs -> neutral default
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFactoryTypes_negative_needCoefficient_throws_ContentException()
    {
        string json = $$"""
        {
          "factoryTypes": [
            {
              "typeId": "{{SmelterFactoryTypeId}}",
              "displayName": "Smelter",
              "recipe": {
                "inputs": [ { "itemTypeId": "{{IronOreId}}", "count": 10, "needCoefficient": -1 } ],
                "outputs": [ { "itemTypeId": "{{EnergyCellsId}}", "count": 1 } ],
                "cycleTimeMs": 5000
              }
            }
          ]
        }
        """;

        var path = WriteTempFile(json);
        try
        {
            Assert.Throws<ContentException>(() => EngineContentLoader.LoadFactoryTypes(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteTempFile(string json)
    {
        string path = Path.Combine(Path.GetTempPath(), $"dss-factorytypes-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }

    // --- U8: station events/buffs — schema-only, participates in the price formula ---------

    [Fact]
    public void Category_wide_event_factor_multiplies_with_the_size_factor()
    {
        // Large -> Good factor 1.10. A +20% Good-category event on top: 1.10 * 1.20 = 1.32.
        string events = """
        [
          {
            "eventId": "evt-good-boom",
            "displayName": "Good demand spike",
            "startedGameTimeMs": 0,
            "durationMs": null,
            "priceFactors": [ { "category": "Good", "itemTypeId": null, "factor": 1200 } ]
          }
        ]
        """;
        var engine = CreateEngine(ScenarioJson(stationSizeJson: "\"Large\"", eventsJson: events));

        Assert.Equal(132, UnitPriceOf(engine, EnergyCellsId)); // 100 * 1.10 * 1.20
        Assert.Equal(115, UnitPriceOf(engine, IceId)); // Resource item unaffected by a Good-only event
    }

    [Fact]
    public void Item_specific_event_factor_applies_only_to_that_item()
    {
        string events = $$"""
        [
          {
            "eventId": "evt-ice-shortage",
            "displayName": "Ice shortage",
            "startedGameTimeMs": 0,
            "durationMs": 60000,
            "priceFactors": [ { "category": null, "itemTypeId": "{{IceId}}", "factor": 1500 } ]
          }
        ]
        """;
        var engine = CreateEngine(ScenarioJson(stationSizeJson: "\"Large\"", eventsJson: events));

        // 100 * 1.15 (general Resource @ Large) * 1.50 (event) = 172.5 -> AwayFromZero -> 173.
        Assert.Equal(173, UnitPriceOf(engine, IceId));
        Assert.Equal(115, UnitPriceOf(engine, IronOreId)); // untouched: 100 * 1.15 (general Resource)
    }

    [Fact]
    public void Station_wide_event_factor_applies_to_every_item()
    {
        string events = """
        [
          {
            "eventId": "evt-blackout",
            "displayName": "Station-wide surcharge",
            "startedGameTimeMs": 0,
            "durationMs": null,
            "priceFactors": [ { "category": null, "itemTypeId": null, "factor": 1100 } ]
          }
        ]
        """;
        var engine = CreateEngine(ScenarioJson(stationSizeJson: "\"Large\"", eventsJson: events));

        Assert.Equal(121, UnitPriceOf(engine, EnergyCellsId)); // 100 * 1.10 (Good) * 1.10 (station-wide)
        // 100 * 1.15 (general Resource @ Large) * 1.10 (station-wide) = 126.5 -> AwayFromZero -> 127.
        Assert.Equal(127, UnitPriceOf(engine, IceId));
    }

    [Fact]
    public void Two_events_at_the_same_start_time_apply_in_deterministic_eventId_order()
    {
        // Both apply to Good; order must not change the product (multiplication is
        // commutative and StationPricing rounds once at the end), but the resolver itself
        // must still walk them in a fixed (StartedGameTimeMs, EventId) order — this test pins
        // that both are applied exactly once regardless of their declaration order.
        string events = """
        [
          {
            "eventId": "evt-b",
            "displayName": "B",
            "startedGameTimeMs": 500,
            "durationMs": null,
            "priceFactors": [ { "category": "Good", "itemTypeId": null, "factor": 1100 } ]
          },
          {
            "eventId": "evt-a",
            "displayName": "A",
            "startedGameTimeMs": 500,
            "durationMs": null,
            "priceFactors": [ { "category": "Good", "itemTypeId": null, "factor": 1200 } ]
          }
        ]
        """;
        var engine = CreateEngine(ScenarioJson(stationSizeJson: "\"Large\"", eventsJson: events));

        // 100 * 1.10 (size) * 1.10 (evt-b) * 1.20 (evt-a) = 145.2 -> 145 (AwayFromZero on the
        // single combined product, order-independent per StationPricingTests).
        Assert.Equal(145, UnitPriceOf(engine, EnergyCellsId));
    }

    [Fact]
    public void Unknown_event_price_factor_category_throws_ScenarioException()
    {
        string events = """
        [
          {
            "eventId": "evt-bad",
            "displayName": "Bad",
            "startedGameTimeMs": 0,
            "durationMs": null,
            "priceFactors": [ { "category": "Module", "itemTypeId": null, "factor": 1100 } ]
          }
        ]
        """;

        Assert.Throws<ScenarioException>(() => CreateEngine(ScenarioJson(eventsJson: events)));
    }

    [Fact]
    public void Unknown_event_price_factor_itemTypeId_throws_ContentException()
    {
        string events = """
        [
          {
            "eventId": "evt-bad",
            "displayName": "Bad",
            "startedGameTimeMs": 0,
            "durationMs": null,
            "priceFactors": [ { "category": null, "itemTypeId": "item.does-not-exist", "factor": 1100 } ]
          }
        ]
        """;

        Assert.Throws<ContentException>(() => CreateEngine(ScenarioJson(eventsJson: events)));
    }

    [Fact]
    public void Station_events_round_trip_through_save_and_load()
    {
        string events = $$"""
        [
          {
            "eventId": "evt-ice-shortage",
            "displayName": "Ice shortage",
            "description": "Local ice reserves ran low.",
            "startedGameTimeMs": 1234,
            "durationMs": 60000,
            "priceFactors": [ { "category": null, "itemTypeId": "{{IceId}}", "factor": 1500 } ]
          }
        ]
        """;
        var engine = CreateEngine(ScenarioJson(eventsJson: events));

        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var savedStation = save.GameState.SpaceObjects.Single(o => o.ObjectId == StationId);
        var savedEvent = Assert.Single(savedStation.Events!);

        Assert.Equal("evt-ice-shortage", savedEvent.EventId);
        Assert.Equal("Ice shortage", savedEvent.DisplayName);
        Assert.Equal("Local ice reserves ran low.", savedEvent.Description);
        Assert.Equal(1234, savedEvent.StartedGameTimeMs);
        Assert.Equal(60000, savedEvent.DurationMs);
        var savedFactor = Assert.Single(savedEvent.PriceFactors);
        Assert.Null(savedFactor.Category);
        Assert.Equal(IceId, savedFactor.ItemTypeId);
        Assert.Equal(1500, savedFactor.Factor);

        var reloaded = new SimulationEngine(CreateRegistry());
        reloaded.LoadScenario(save);

        // Same price as before reload -> the event round-tripped and still applies.
        Assert.Equal(UnitPriceOf(engine, IceId), UnitPriceOf(reloaded, IceId));
    }

    [Fact]
    public void Event_with_missing_priceFactors_field_contributes_no_factors()
    {
        // A hand-authored event JSON that omits "priceFactors" entirely deserializes it as
        // null (same STJ behavior the codebase already relies on for FactoryTypeDefinitionDto.
        // Recipe.Inputs/Outputs) — tolerated as "no price effect", not a load error.
        string events = """
        [ { "eventId": "evt-lore-only", "displayName": "Lore-only event", "startedGameTimeMs": 0 } ]
        """;

        var engine = CreateEngine(ScenarioJson(stationSizeJson: "\"Large\"", eventsJson: events));

        Assert.Equal(110, UnitPriceOf(engine, EnergyCellsId)); // unaffected: 100 * 1.10 (Good, size only)
    }

    // --- U5 acceptance criteria: real Default/Docked scenario SPC-0002 ---------------------

    private static string ResolveRealSettingsPath()
    {
        string settingsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "DeepSpaceSaga.Client", "Settings.json"));

        if (!File.Exists(settingsPath))
            settingsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Settings.json"));

        return settingsPath;
    }

    private static readonly (string ItemTypeId, long Quantity)[] StartStationInventory =
    [
        ("item.food-rations", 500),
        ("item.energy-cells", 350),
        ("item.fuel", 700),
        ("item.ice", 320),
        ("item.iron-ore", 410),
        ("item.silicon", 70),
        ("item.magnesium-ore", 120),
    ];

    [Fact]
    public void Real_default_scenario_start_station_has_Large_size_and_documented_inventory()
    {
        // §59 acceptance criteria: "Стартовая станция SPC-0002 в Default и Docked сценариях
        // имеет stationSize = Large" + the 7-line explicit starting inventory table.
        string settingsPath = ResolveRealSettingsPath();
        var engine = SimulationEngine.CreateFromSettingsFile(settingsPath);

        var station = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "SPC-0002");
        Assert.Equal(StationSize.Large, station.StationSize);

        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settingsPath, out _, out _);
        foreach (var (itemTypeId, quantity) in StartStationInventory)
        {
            int index = registry.ItemTypes.GetIndex(itemTypeId);
            var entry = station.Inventory.Single(i => i.ItemTypeIndex == index);
            Assert.Equal(quantity, entry.StockQuantity);
        }
    }

    [Fact]
    public void Real_docked_scenario_start_station_has_Large_size_and_documented_inventory()
    {
        string settingsPath = ResolveRealSettingsPath();
        string dockedScenarioPath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(settingsPath)!, "Scenarios", "Docked", "scenario.json"));

        var engine = SimulationEngine.CreateFromScenarioFile(settingsPath, dockedScenarioPath);

        var station = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "SPC-0002");
        Assert.Equal(StationSize.Large, station.StationSize);

        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settingsPath, out _, out _);
        foreach (var (itemTypeId, quantity) in StartStationInventory)
        {
            int index = registry.ItemTypes.GetIndex(itemTypeId);
            var entry = station.Inventory.Single(i => i.ItemTypeIndex == index);
            Assert.Equal(quantity, entry.StockQuantity);
        }
    }

    [Fact]
    public void Real_default_500_scenario_start_station_keeps_the_old_random_fallback_inventory()
    {
        // Protect list: Default_500/scenario.json is explicitly out of scope for this batch —
        // its SPC-0002 has no explicit stationSize/inventory, so it must still resolve via the
        // untouched fallback generator (Medium size, 20..500 random inventory per item), not
        // the new Large/explicit-inventory acceptance criteria above.
        string settingsPath = ResolveRealSettingsPath();
        string default500ScenarioPath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(settingsPath)!, "Scenarios", "Default_500", "scenario.json"));

        var engine = SimulationEngine.CreateFromScenarioFile(settingsPath, default500ScenarioPath);

        var station = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "SPC-0002");
        Assert.Equal(StationSize.Medium, station.StationSize); // fallback, not the explicit Large

        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settingsPath, out _, out _);
        int iceIndex = registry.ItemTypes.GetIndex("item.ice");
        Assert.InRange(station.Inventory.Single(i => i.ItemTypeIndex == iceIndex).StockQuantity, 20, 500);
    }

    // --- Batch 4 (U11): save -> reload does not change the real SPC-0002's resolved size,
    // inventory, or DockedStationTrade prices ------------------------------------------------

    [Fact]
    public void Real_docked_scenario_save_then_reload_does_not_change_stationSize_inventory_or_prices()
    {
        // §59 acceptance criteria (last bullet): "Save/load сохраняет размер станции и не
        // меняет цены/склад после загрузки при тех же данных." Unlike
        // StationSize_round_trips_through_save_and_load_without_changing (synthetic fixture,
        // size only) and StationEconomyGenerationTests.Save_then_load_does_not_regenerate_
        // already_resolved_station_economy (fallback-generated STN-0001, no DockedStationTrade
        // price check), this pins the real Docked/scenario.json SPC-0002 end to end: resolved
        // stationSize, the explicit 7-line inventory, AND the final formula-computed
        // UnitPriceCredits the Trade UI would show — all three must be byte-for-byte identical
        // after a save -> reload round trip with no intervening mutation.
        string settingsPath = ResolveRealSettingsPath();
        string dockedScenarioPath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(settingsPath)!, "Scenarios", "Docked", "scenario.json"));

        var engine = SimulationEngine.CreateFromScenarioFile(settingsPath, dockedScenarioPath);

        var stationBefore = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "SPC-0002");
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settingsPath, out _, out _);
        var inventoryBefore = stationBefore.Inventory
            .OrderBy(i => i.ItemTypeIndex)
            .Select(i => (i.ItemTypeIndex, i.StockQuantity))
            .ToArray();

        var snapshotBefore = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);
        Assert.NotNull(snapshotBefore.DockedStationTrade);
        var pricesBefore = snapshotBefore.DockedStationTrade!.Items
            .OrderBy(i => i.ItemTypeId, StringComparer.Ordinal)
            .Select(i => (i.ItemTypeId, i.UnitPriceCredits))
            .ToArray();
        Assert.NotEmpty(pricesBefore);

        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);

        var reloadedEngine = new SimulationEngine(registry);
        reloadedEngine.LoadScenario(save);

        var stationAfter = reloadedEngine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "SPC-0002");
        Assert.Equal(stationBefore.StationSize, stationAfter.StationSize);

        var inventoryAfter = stationAfter.Inventory
            .OrderBy(i => i.ItemTypeIndex)
            .Select(i => (i.ItemTypeIndex, i.StockQuantity))
            .ToArray();
        Assert.Equal(inventoryBefore, inventoryAfter);

        var snapshotAfter = reloadedEngine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);
        Assert.NotNull(snapshotAfter.DockedStationTrade);
        var pricesAfter = snapshotAfter.DockedStationTrade!.Items
            .OrderBy(i => i.ItemTypeId, StringComparer.Ordinal)
            .Select(i => (i.ItemTypeId, i.UnitPriceCredits))
            .ToArray();

        Assert.Equal(pricesBefore, pricesAfter);
    }
}
