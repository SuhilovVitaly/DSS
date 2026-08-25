using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Tests for the Engine -> AuthoritativeSnapshot trade projection (story-20260822-193700,
/// Batch 4): <see cref="AuthoritativeSnapshot.PlayerCredits"/>,
/// <see cref="AuthoritativeSnapshot.DockedStationTrade"/>, and
/// <see cref="InstalledModuleSnapshot.Cargo"/>. Batch 3 (<see cref="TradeCommandTests"/>)
/// already covers the underlying Buy/Sell/Refuel mutations against
/// <c>SimulationEngine.RuntimeObjects</c>/<c>PlayerCredits</c> directly; these tests only
/// check that the same state is correctly projected into the client-facing snapshot.
/// </summary>
public class TradeSnapshotProjectionTests
{
    private const string PlayerShipId = "SPC-0001";
    private const string CargoModuleId = "MOD-CARGO-01";
    private const string EngineModuleId = "MOD-ENG-01";
    private const string StationId = "STATION-01";

    private const string EnergyCellsId = "item.energy-cells";
    private const string FuelId = "item.fuel";
    private const string IceId = "item.ice";

    private static PlayerCommand BuyCommand(string itemTypeId = EnergyCellsId, long? quantity = 10) =>
        new("cmd-buy", 1, PlayerShipId, CargoModuleId, TradeCommandTypes.Buy, ItemTypeId: itemTypeId, Quantity: quantity);

    private static PlayerCommand SellCommand(string itemTypeId = EnergyCellsId, long? quantity = 10) =>
        new("cmd-sell", 1, PlayerShipId, CargoModuleId, TradeCommandTypes.Sell, ItemTypeId: itemTypeId, Quantity: quantity);

    private static SimulationEngine CreateEngine(
        long playerCredits = 100_000,
        bool isDocked = true,
        long stationCredits = 1_000_000,
        int stationPriceCoefficient = 1000,
        IEnumerable<(string ItemTypeId, long Quantity)>? stationInventory = null,
        IEnumerable<(string ItemTypeId, long Quantity)>? shipCargo = null,
        long shipFuelAmountKg = 0,
        long containerCargoCapacityKg = 1000,
        long engineFuelCapacityKg = 500)
    {
        var inventory = stationInventory ?? new[]
        {
            (EnergyCellsId, 100L),
            (FuelId, 200L),
            (IceId, 100L)
        };
        var cargo = shipCargo ?? Enumerable.Empty<(string, long)>();

        string inventoryJson = string.Join(",", inventory.Select(i =>
            $$"""{ "itemTypeId": "{{i.ItemTypeId}}", "quantity": {{i.Quantity}} }"""));
        string cargoJson = string.Join(",", cargo.Select(c =>
            $$"""{ "itemTypeId": "{{c.Item1}}", "quantity": {{c.Item2}} }"""));
        string dockedStationField = isDocked ? $"\"dockedStationObjectId\": \"{StationId}\"," : "";

        var engine = new SimulationEngine(CreateRegistry(containerCargoCapacityKg, engineFuelCapacityKg));
        engine.LoadScenario(ScenarioLoader.LoadFromJson($$"""
        {
          "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
          "gameState": {
            "gameTimeMs": 0,
            "currentSpeed": "Speed0",
            "playerShipObjectId": "{{PlayerShipId}}",
            "playerCredits": {{playerCredits}},
            "spaceObjects": [
              {
                "objectId": "{{PlayerShipId}}",
                "objectType": "PlayerShip",
                "persistenceType": "Permanent",
                "positionX": 10000,
                "positionY": 10000,
                "speedMps": 0,
                "directionDegrees": 0,
                "movementType": "Stationary",
                "isDocked": {{(isDocked ? "true" : "false")}},
                {{dockedStationField}}
                "hullLayout": { "width": 2, "height": 1, "cells": [ {"x":0,"y":0}, {"x":1,"y":0} ] },
                "modules": [
                  {
                    "moduleId": "{{CargoModuleId}}",
                    "moduleTypeId": "module.container.basic",
                    "occupiedCells": [ {"x":0,"y":0} ],
                    "structurePoints": 400,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "activeCycle": null,
                    "cargo": [ {{cargoJson}} ]
                  },
                  {
                    "moduleId": "{{EngineModuleId}}",
                    "moduleTypeId": "module.engine.basic",
                    "occupiedCells": [ {"x":1,"y":0} ],
                    "structurePoints": 100,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "activeCycle": null,
                    "cargo": [],
                    "fuelAmountKg": {{shipFuelAmountKg}}
                  }
                ]
              },
              {
                "objectId": "{{StationId}}",
                "objectType": "Station",
                "persistenceType": "Permanent",
                "credits": {{stationCredits}},
                "priceCoefficient": {{stationPriceCoefficient}},
                "inventory": [ {{inventoryJson}} ],
                "positionX": 10001,
                "positionY": 10000,
                "speedMps": 0,
                "directionDegrees": 0,
                "movementType": "Stationary"
              }
            ]
          }
        }
        """));

        return engine;
    }

    private static GameDataRegistry CreateRegistry(long containerCargoCapacityKg, long engineFuelCapacityKg)
    {
        string[] cargoCommandIds = [TradeCommandTypes.Buy, TradeCommandTypes.Sell];
        string[] engineCommandIds = [TradeCommandTypes.Refuel];

        return GameDataRegistry.Create(
            [
                new ModuleCategoryDefinition(
                    "module.container", "Container", SlotSize: 1, CommandTypeIds: cargoCommandIds.ToImmutableArray()),
                new ModuleCategoryDefinition(
                    "module.engine", "Engine", SlotSize: 1, CommandTypeIds: engineCommandIds.ToImmutableArray())
            ],
            [
                new ModuleTypeDefinition(
                    "module.container.basic", "Container", SlotSize: 1, MassKg: 20000,
                    StructurePointsMax: 400, PowerConsumptionW: 0,
                    CommandTypeIds: cargoCommandIds.ToImmutableArray(),
                    CargoCapacityKg: containerCargoCapacityKg,
                    BaseCycleTimeMs: 1000),
                new ModuleTypeDefinition(
                    "module.engine.basic", "Engine", SlotSize: 1, MassKg: 5000,
                    StructurePointsMax: 100, PowerConsumptionW: 0,
                    CommandTypeIds: engineCommandIds.ToImmutableArray(),
                    FuelCapacityKg: engineFuelCapacityKg,
                    BaseCycleTimeMs: 1000)
            ],
            [
                // EnergyCells/Fuel default to TradeCategory.Good (record default); Ice is
                // explicitly Resource — story-20260825-084409 Batch 1 tests below rely on this
                // split for both the sell-package floor and the "legacy PriceCoefficient no
                // longer participates" regression.
                new ItemTypeDefinition(EnergyCellsId, "Energy Cells", UnitMassKg: 10, BasePriceCredits: 200),
                new ItemTypeDefinition(FuelId, "Fuel", UnitMassKg: 0, BasePriceCredits: 200),
                new ItemTypeDefinition(IceId, "Ice", UnitMassKg: 10, BasePriceCredits: 30, Category: TradeCategory.Resource)
            ],
            [
                new CommandDefinition(TradeCommandTypes.Buy, "Buy", Target: "none", Type: "module.container"),
                new CommandDefinition(TradeCommandTypes.Sell, "Sell", Target: "none", Type: "module.container"),
                new CommandDefinition(TradeCommandTypes.Refuel, "Refuel", Target: "none", Type: "module.engine")
            ]);
    }

    // --- 4.1 PlayerCredits ---------------------------------------------------

    [Fact]
    public void Snapshot_PlayerCredits_matches_engine_state()
    {
        var engine = CreateEngine(playerCredits: 12_345, isDocked: false);

        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);

        Assert.Equal(12_345, snapshot.PlayerCredits);
        Assert.Equal(engine.PlayerCredits, snapshot.PlayerCredits);
    }

    [Fact]
    public void Snapshot_PlayerCredits_reflects_buy_mutation()
    {
        var engine = CreateEngine(playerCredits: 5000, isDocked: true);

        // unitPrice = 200 * 1.15 (Good @ Medium fallback — story-20260825-084409 Batch 2,
        // U5: this fixture never sets an explicit stationSize) = 230.
        engine.ReceiveCommand(BuyCommand(EnergyCellsId, quantity: 10)); // cost 2300
        var snapshot = engine.CaptureSnapshotForTests();

        Assert.Equal(2700, snapshot.PlayerCredits);
    }

    // --- 4.2 DockedStationTrade -----------------------------------------------

    [Fact]
    public void DockedStationTrade_is_null_when_not_docked()
    {
        var engine = CreateEngine(isDocked: false);

        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);

        Assert.Null(snapshot.DockedStationTrade);
    }

    [Fact]
    public void DockedStationTrade_projects_station_inventory_with_correct_prices_and_max_sellable()
    {
        // Story-20260825-084409 Batch 1 (U4): legacy station.PriceCoefficient no longer
        // participates in the price formula (§59) — priceCoefficient 1500 here is deliberately
        // non-neutral to prove it has NO effect. Batch 2 (U5) now resolves a real per-station
        // StationSize factor instead: this fixture never sets an explicit stationSize, so it
        // falls back to StationSize.Medium (SimulationEngine.ResolveStationSize) — Good @
        // Medium = 1.15, general Resource (Ice is not a producing-module input here) @ Medium
        // = 1.10. unitPrice = BasePriceCredits * that factor:
        // EnergyCells/Fuel (Good, base 200) -> 230; Ice (Resource, base 30) -> 33.
        // MaxSellableQuantity additionally floors to whole sell packages (U9): EnergyCells/Fuel
        // are Good (package 10), Ice is Resource (package 100).
        var engine = CreateEngine(
            isDocked: true,
            stationCredits: 10_000,
            stationPriceCoefficient: 1500,
            stationInventory:
            [
                (EnergyCellsId, 100L),
                (FuelId, 50L),
                (IceId, 20L)
            ]);

        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);

        Assert.NotNull(snapshot.DockedStationTrade);
        Assert.Equal(StationId, snapshot.DockedStationTrade!.StationObjectId);
        Assert.Equal(3, snapshot.DockedStationTrade.Items.Length);

        var energyCells = snapshot.DockedStationTrade.Items.Single(i => i.ItemTypeId == EnergyCellsId);
        Assert.Equal(100, energyCells.StockQuantity);
        Assert.Equal(230, energyCells.UnitPriceCredits);
        Assert.Equal(10_000 / 230 / 10 * 10, energyCells.MaxSellableQuantity); // raw 43 -> floors to 40

        var fuel = snapshot.DockedStationTrade.Items.Single(i => i.ItemTypeId == FuelId);
        Assert.Equal(50, fuel.StockQuantity);
        Assert.Equal(230, fuel.UnitPriceCredits);
        Assert.Equal(10_000 / 230 / 10 * 10, fuel.MaxSellableQuantity); // 40

        var ice = snapshot.DockedStationTrade.Items.Single(i => i.ItemTypeId == IceId);
        Assert.Equal(20, ice.StockQuantity);
        Assert.Equal(33, ice.UnitPriceCredits);
        Assert.Equal(10_000 / 33 / 100 * 100, ice.MaxSellableQuantity); // raw 303 -> floors to 300
    }

    [Fact]
    public void Serialized_snapshot_never_contains_raw_station_credits()
    {
        // Recognizable station Credits value that must never leak into the wire format —
        // only the derived MaxSellableQuantity may appear.
        var engine = CreateEngine(
            isDocked: true,
            stationCredits: 12345,
            stationPriceCoefficient: 1000,
            stationInventory: [(EnergyCellsId, 100L)]);

        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);
        Assert.NotNull(snapshot.DockedStationTrade);

        string json = JsonSerializer.Serialize(snapshot);

        Assert.DoesNotContain("12345", json);
    }

    [Fact]
    public void DockedStationTrade_reflects_sell_mutation()
    {
        var engine = CreateEngine(
            playerCredits: 0,
            isDocked: true,
            stationCredits: 1_000_000,
            stationPriceCoefficient: 1000,
            stationInventory: [(EnergyCellsId, 100L)],
            shipCargo: [(EnergyCellsId, 10L)]);

        // unitPrice = 200 * 1.15 (Good @ Medium fallback — story-20260825-084409 Batch 2,
        // U5) = 230.
        engine.ReceiveCommand(SellCommand(EnergyCellsId, quantity: 10)); // unitPrice 230
        var snapshot = engine.CaptureSnapshotForTests();

        Assert.NotNull(snapshot.DockedStationTrade);
        var energyCells = snapshot.DockedStationTrade!.Items.Single(i => i.ItemTypeId == EnergyCellsId);
        Assert.Equal(110, energyCells.StockQuantity); // 100 + 10 sold
        Assert.Equal(2300, snapshot.PlayerCredits); // 10 * 230
    }

    // --- 4.3 InstalledModuleSnapshot.Cargo -------------------------------------

    [Fact]
    public void InstalledModuleSnapshot_Cargo_is_empty_for_module_without_cargo()
    {
        var engine = CreateEngine(isDocked: false, shipCargo: Enumerable.Empty<(string, long)>());

        var snapshot = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);

        var cargoModule = snapshot.InstalledModules.Single(m => m.ModuleId == CargoModuleId);
        Assert.True(cargoModule.Cargo.IsDefault || cargoModule.Cargo.IsEmpty);

        var engineModule = snapshot.InstalledModules.Single(m => m.ModuleId == EngineModuleId);
        Assert.True(engineModule.Cargo.IsDefault || engineModule.Cargo.IsEmpty);
    }

    [Fact]
    public void InstalledModuleSnapshot_Cargo_reflects_buy_mutation()
    {
        var engine = CreateEngine(playerCredits: 5000, isDocked: true);

        engine.ReceiveCommand(BuyCommand(EnergyCellsId, quantity: 10));
        var snapshot = engine.CaptureSnapshotForTests();

        var cargoModule = snapshot.InstalledModules.Single(m => m.ModuleId == CargoModuleId);
        Assert.False(cargoModule.Cargo.IsDefault);
        var stack = Assert.Single(cargoModule.Cargo);
        Assert.Equal(EnergyCellsId, stack.ItemTypeId);
        Assert.Equal(10, stack.Quantity);
    }
}
