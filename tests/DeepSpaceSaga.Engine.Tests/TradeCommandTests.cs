using System.Collections.Immutable;
using System.Linq;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Tests for trade.buy / trade.sell / trade.refuel — station trading (requirements
/// Docs\FirstRelease\Mechanics\{Money,StationInventory,Trading}.md), story-20260822-193700
/// Batch 3: immediate one-shot authoritative actions dispatched by
/// <c>SimulationEngine.TryStartTradeCommand</c>, distinct from the timed ActiveCycle Engine
/// commands (<see cref="EngineCommandTests"/>) and the immediate Dock action
/// (<see cref="DockCommandTests"/>).
/// </summary>
public class TradeCommandTests
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

    private static PlayerCommand RefuelCommand(string itemTypeId = FuelId, long? quantity = 100) =>
        new("cmd-refuel", 1, PlayerShipId, EngineModuleId, TradeCommandTypes.Refuel, ItemTypeId: itemTypeId, Quantity: quantity);

    private static SimulationEngine CreateEngine(
        long playerCredits = 100_000,
        bool isDocked = true,
        long stationCredits = 1_000_000,
        int stationPriceCoefficient = 1000,
        IEnumerable<(string ItemTypeId, long Quantity)>? stationInventory = null,
        IEnumerable<(string ItemTypeId, long Quantity)>? shipCargo = null,
        long shipFuelAmountKg = 0,
        long containerCargoCapacityKg = 1000,
        long engineFuelCapacityKg = 500,
        string cargoPowerState = "On",
        string cargoOperationalState = "Ready",
        int cargoStructurePoints = 400,
        string enginePowerState = "On",
        string engineOperationalState = "Ready",
        int engineStructurePoints = 100)
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
                    "structurePoints": {{cargoStructurePoints}},
                    "powerState": "{{cargoPowerState}}",
                    "operationalState": "{{cargoOperationalState}}",
                    "activeCycle": null,
                    "cargo": [ {{cargoJson}} ]
                  },
                  {
                    "moduleId": "{{EngineModuleId}}",
                    "moduleTypeId": "module.engine.basic",
                    "occupiedCells": [ {"x":1,"y":0} ],
                    "structurePoints": {{engineStructurePoints}},
                    "powerState": "{{enginePowerState}}",
                    "operationalState": "{{engineOperationalState}}",
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
                new ItemTypeDefinition(EnergyCellsId, "Energy Cells", UnitMassKg: 10, BasePriceCredits: 200),
                new ItemTypeDefinition(FuelId, "Fuel", UnitMassKg: 0, BasePriceCredits: 200),
                new ItemTypeDefinition(IceId, "Ice", UnitMassKg: 10, BasePriceCredits: 30)
            ],
            [
                new CommandDefinition(TradeCommandTypes.Buy, "Buy", Target: "none", Type: "module.container"),
                new CommandDefinition(TradeCommandTypes.Sell, "Sell", Target: "none", Type: "module.container"),
                new CommandDefinition(TradeCommandTypes.Refuel, "Refuel", Target: "none", Type: "module.engine")
            ]);
    }

    // --- Buy ---------------------------------------------------------------

    [Fact]
    public void Buy_new_item_creates_cargo_stack_and_moves_credits_and_stock()
    {
        var engine = CreateEngine(playerCredits: 5000);

        engine.ReceiveCommand(BuyCommand(EnergyCellsId, quantity: 10));
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Executed, result.Status);
        Assert.Null(result.ExecutedQuantity);

        Assert.Equal(5000 - 2000, engine.PlayerCredits); // unitPrice 200 * 10

        var station = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationId);
        Assert.Equal(1_000_000 + 2000, station.Credits);
        var registry = CreateRegistry(1000, 500);
        int energyCellsIndex = registry.ItemTypes.GetIndex(EnergyCellsId);
        Assert.Equal(90, station.Inventory.Single(i => i.ItemTypeIndex == energyCellsIndex).StockQuantity);

        var ship = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId);
        var cargoModule = ship.Modules.Single(m => m.ModuleId == CargoModuleId);
        var stack = Assert.Single(cargoModule.Cargo);
        Assert.Equal(energyCellsIndex, stack.ItemTypeIndex);
        Assert.Equal(10, stack.Quantity);
    }

    [Fact]
    public void Buy_existing_item_increases_cargo_stack_quantity()
    {
        var engine = CreateEngine(shipCargo: [(EnergyCellsId, 5)]);

        engine.ReceiveCommand(BuyCommand(EnergyCellsId, quantity: 10));
        var snapshot = engine.CaptureSnapshotForTests();

        Assert.Equal(CommandResultStatus.Executed, Assert.Single(snapshot.CommandResults).Status);

        var ship = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId);
        var cargoModule = ship.Modules.Single(m => m.ModuleId == CargoModuleId);
        var stack = Assert.Single(cargoModule.Cargo);
        Assert.Equal(15, stack.Quantity);
    }

    [Fact]
    public void Buy_with_insufficient_player_credits_is_rejected()
    {
        var engine = CreateEngine(playerCredits: 100);

        engine.ReceiveCommand(BuyCommand(EnergyCellsId, quantity: 10)); // cost 2000
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.InsufficientPlayerCredits, result.ReasonCode);
        Assert.Equal(100, engine.PlayerCredits);
    }

    [Fact]
    public void Buy_more_than_station_stock_is_rejected()
    {
        var engine = CreateEngine(stationInventory: [(EnergyCellsId, 5)]);

        engine.ReceiveCommand(BuyCommand(EnergyCellsId, quantity: 10));
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.InsufficientStationStock, result.ReasonCode);
    }

    [Fact]
    public void Buy_exceeding_cargo_capacity_is_rejected()
    {
        var engine = CreateEngine(containerCargoCapacityKg: 50, stationInventory: [(IceId, 100)]);

        engine.ReceiveCommand(BuyCommand(IceId, quantity: 10)); // 10 * 10kg = 100kg > 50kg
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.CargoCapacityExceeded, result.ReasonCode);
    }

    [Fact]
    public void Buy_while_not_docked_is_rejected()
    {
        var engine = CreateEngine(isDocked: false);

        engine.ReceiveCommand(BuyCommand());
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.NotDocked, result.ReasonCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Buy_with_invalid_quantity_is_rejected(long quantity)
    {
        var engine = CreateEngine();

        engine.ReceiveCommand(BuyCommand(EnergyCellsId, quantity: quantity));
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.InvalidQuantity, result.ReasonCode);
    }

    [Fact]
    public void Buy_unknown_item_type_is_rejected()
    {
        var engine = CreateEngine();

        engine.ReceiveCommand(BuyCommand("item.does-not-exist", quantity: 1));
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.UnknownItemType, result.ReasonCode);
    }

    // --- Sell ----------------------------------------------------------------

    [Fact]
    public void Sell_fully_executes_when_station_can_afford_it()
    {
        var engine = CreateEngine(shipCargo: [(EnergyCellsId, 20)], stationCredits: 1_000_000);

        engine.ReceiveCommand(SellCommand(EnergyCellsId, quantity: 10));
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Executed, result.Status);
        Assert.Null(result.ExecutedQuantity); // fully executed

        Assert.Equal(100_000 + 2000, engine.PlayerCredits);

        var station = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationId);
        Assert.Equal(1_000_000 - 2000, station.Credits);
        var registry = CreateRegistry(1000, 500);
        int energyCellsIndex = registry.ItemTypes.GetIndex(EnergyCellsId);
        Assert.Equal(110, station.Inventory.Single(i => i.ItemTypeIndex == energyCellsIndex).StockQuantity);

        var ship = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId);
        var cargoModule = ship.Modules.Single(m => m.ModuleId == CargoModuleId);
        var stack = Assert.Single(cargoModule.Cargo);
        Assert.Equal(10, stack.Quantity);
    }

    [Fact]
    public void Sell_all_of_a_stack_removes_it_from_cargo()
    {
        var engine = CreateEngine(shipCargo: [(EnergyCellsId, 10)]);

        engine.ReceiveCommand(SellCommand(EnergyCellsId, quantity: 10));
        var snapshot = engine.CaptureSnapshotForTests();

        Assert.Equal(CommandResultStatus.Executed, Assert.Single(snapshot.CommandResults).Status);

        var ship = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId);
        var cargoModule = ship.Modules.Single(m => m.ModuleId == CargoModuleId);
        Assert.Empty(cargoModule.Cargo);
    }

    [Fact]
    public void Sell_more_than_cargo_holds_is_rejected()
    {
        var engine = CreateEngine(shipCargo: [(EnergyCellsId, 5)]);

        engine.ReceiveCommand(SellCommand(EnergyCellsId, quantity: 10));
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.InsufficientCargoQuantity, result.ReasonCode);
        Assert.Equal(100_000, engine.PlayerCredits);

        var ship = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId);
        var cargoModule = ship.Modules.Single(m => m.ModuleId == CargoModuleId);
        Assert.Equal(5, Assert.Single(cargoModule.Cargo).Quantity);
    }

    [Fact]
    public void Sell_partially_executes_when_station_cannot_afford_full_request()
    {
        // unitPrice = 200; station can afford 5000 / 200 = 25 units, less than the 50 requested.
        var engine = CreateEngine(shipCargo: [(EnergyCellsId, 50)], stationCredits: 5000);

        engine.ReceiveCommand(SellCommand(EnergyCellsId, quantity: 50));
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Executed, result.Status);
        Assert.Equal(25, result.ExecutedQuantity);

        Assert.Equal(100_000 + 25 * 200, engine.PlayerCredits);

        var station = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationId);
        Assert.Equal(0, station.Credits);
        var registry = CreateRegistry(1000, 500);
        int energyCellsIndex = registry.ItemTypes.GetIndex(EnergyCellsId);
        Assert.Equal(125, station.Inventory.Single(i => i.ItemTypeIndex == energyCellsIndex).StockQuantity); // 100 + 25

        var ship = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId);
        var cargoModule = ship.Modules.Single(m => m.ModuleId == CargoModuleId);
        Assert.Equal(25, Assert.Single(cargoModule.Cargo).Quantity); // 50 - 25
    }

    [Fact]
    public void Sell_rejected_when_station_cannot_afford_even_one_unit()
    {
        // unitPrice = 200; station credits 100 < 200 -> cannot buy a single unit.
        var engine = CreateEngine(shipCargo: [(EnergyCellsId, 10)], stationCredits: 100);

        engine.ReceiveCommand(SellCommand(EnergyCellsId, quantity: 10));
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.InsufficientStationStock, result.ReasonCode);

        Assert.Equal(100_000, engine.PlayerCredits);
        var station = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationId);
        Assert.Equal(100, station.Credits);

        var ship = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId);
        var cargoModule = ship.Modules.Single(m => m.ModuleId == CargoModuleId);
        Assert.Equal(10, Assert.Single(cargoModule.Cargo).Quantity);
    }

    // --- Refuel ----------------------------------------------------------------

    [Fact]
    public void Refuel_success_increases_fuel_and_moves_credits_and_stock()
    {
        var engine = CreateEngine(playerCredits: 50_000, shipFuelAmountKg: 0, engineFuelCapacityKg: 500);

        engine.ReceiveCommand(RefuelCommand(FuelId, quantity: 100));
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Executed, result.Status);

        Assert.Equal(50_000 - 100 * 200, engine.PlayerCredits);

        var station = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == StationId);
        Assert.Equal(1_000_000 + 100 * 200, station.Credits);
        var registry = CreateRegistry(1000, 500);
        int fuelIndex = registry.ItemTypes.GetIndex(FuelId);
        Assert.Equal(100, station.Inventory.Single(i => i.ItemTypeIndex == fuelIndex).StockQuantity); // 200 - 100

        var ship = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == PlayerShipId);
        var engineModule = ship.Modules.Single(m => m.ModuleId == EngineModuleId);
        Assert.Equal(100, engineModule.FuelAmountKg);
    }

    [Fact]
    public void Refuel_exceeding_fuel_capacity_is_rejected()
    {
        var engine = CreateEngine(shipFuelAmountKg: 450, engineFuelCapacityKg: 500);

        engine.ReceiveCommand(RefuelCommand(FuelId, quantity: 100)); // 450 + 100 = 550 > 500
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.FuelCapacityExceeded, result.ReasonCode);
    }

    [Fact]
    public void Refuel_with_insufficient_player_credits_is_rejected()
    {
        var engine = CreateEngine(playerCredits: 100);

        engine.ReceiveCommand(RefuelCommand(FuelId, quantity: 100)); // cost 20000
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.InsufficientPlayerCredits, result.ReasonCode);
    }

    [Fact]
    public void Refuel_more_than_station_fuel_stock_is_rejected()
    {
        var engine = CreateEngine(stationInventory: [(FuelId, 50)]);

        engine.ReceiveCommand(RefuelCommand(FuelId, quantity: 100));
        var snapshot = engine.CaptureSnapshotForTests();

        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(CommandReasonCodes.InsufficientStationStock, result.ReasonCode);
    }
}
