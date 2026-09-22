using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class CommandResultTests
{
    [Fact]
    public void CommandResult_is_instantiable()
    {
        var result = new CommandResult(
            CommandId: "cmd-1",
            ObjectId: "ship-1",
            ModuleId: "engine-1",
            CommandType: "engine.accelerate",
            Status: CommandResultStatus.Executed,
            EffectiveGameTimeMs: 1000);

        Assert.NotNull(result);
        Assert.Equal("cmd-1", result.CommandId);
        Assert.Equal("ship-1", result.ObjectId);
        Assert.Equal("engine-1", result.ModuleId);
        Assert.Equal("engine.accelerate", result.CommandType);
        Assert.Equal(CommandResultStatus.Executed, result.Status);
        Assert.Equal(1000, result.EffectiveGameTimeMs);
        Assert.Null(result.ReasonCode);
    }

    [Fact]
    public void CommandResultStatus_has_all_five_values()
    {
        Assert.Equal(0, (int)CommandResultStatus.Executed);
        Assert.Equal(1, (int)CommandResultStatus.Rejected);
        Assert.Equal(2, (int)CommandResultStatus.Deferred);
        Assert.Equal(3, (int)CommandResultStatus.Cancelled);
        Assert.Equal(4, (int)CommandResultStatus.Failed);

        Assert.True(Enum.IsDefined(typeof(CommandResultStatus), CommandResultStatus.Executed));
        Assert.True(Enum.IsDefined(typeof(CommandResultStatus), CommandResultStatus.Rejected));
        Assert.True(Enum.IsDefined(typeof(CommandResultStatus), CommandResultStatus.Deferred));
        Assert.True(Enum.IsDefined(typeof(CommandResultStatus), CommandResultStatus.Cancelled));
        Assert.True(Enum.IsDefined(typeof(CommandResultStatus), CommandResultStatus.Failed));
    }

    [Fact]
    public void CommandReasonCodes_are_snake_case()
    {
        Assert.Equal("unknown_object", CommandReasonCodes.UnknownObject);
        Assert.Equal("unknown_module", CommandReasonCodes.UnknownModule);
        Assert.Equal("unknown_command_type", CommandReasonCodes.UnknownCommandType);
        Assert.Equal("module_unavailable", CommandReasonCodes.ModuleUnavailable);
        Assert.Equal("busy", CommandReasonCodes.Busy);
    }

    [Fact]
    public void Trade_CommandReasonCodes_are_snake_case()
    {
        Assert.Equal("insufficient_player_credits", CommandReasonCodes.InsufficientPlayerCredits);
        Assert.Equal("insufficient_station_stock", CommandReasonCodes.InsufficientStationStock);
        Assert.Equal("cargo_capacity_exceeded", CommandReasonCodes.CargoCapacityExceeded);
        Assert.Equal("fuel_capacity_exceeded", CommandReasonCodes.FuelCapacityExceeded);
        Assert.Equal("unknown_item_type", CommandReasonCodes.UnknownItemType);
        Assert.Equal("not_docked", CommandReasonCodes.NotDocked);
        Assert.Equal("insufficient_cargo_quantity", CommandReasonCodes.InsufficientCargoQuantity);
        Assert.Equal("invalid_quantity", CommandReasonCodes.InvalidQuantity);
    }

    [Fact]
    public void CommandResult_executed_quantity_defaults_to_null()
    {
        var result = new CommandResult(
            CommandId: "cmd-1",
            ObjectId: "ship-1",
            ModuleId: "container-1",
            CommandType: TradeCommandTypes.Sell,
            Status: CommandResultStatus.Executed,
            EffectiveGameTimeMs: 1000);

        Assert.Null(result.ExecutedQuantity);
    }

    [Fact]
    public void CommandResult_executed_quantity_round_trips_via_json()
    {
        var result = new CommandResult(
            CommandId: "cmd-1",
            ObjectId: "ship-1",
            ModuleId: "container-1",
            CommandType: TradeCommandTypes.Sell,
            Status: CommandResultStatus.Executed,
            EffectiveGameTimeMs: 1000,
            ExecutedQuantity: 7);

        var json = JsonSerializer.Serialize(result);
        var roundTripped = JsonSerializer.Deserialize<CommandResult>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(7, roundTripped!.ExecutedQuantity);
    }

    [Fact]
    public void AuthoritativeSnapshot_round_trips_command_results()
    {
        var results = ImmutableArray.Create(
            new CommandResult(
                CommandId: "cmd-1",
                ObjectId: "ship-1",
                ModuleId: "engine-1",
                CommandType: "engine.accelerate",
                Status: CommandResultStatus.Executed,
                EffectiveGameTimeMs: 1000),
            new CommandResult(
                CommandId: "cmd-2",
                ObjectId: "ship-1",
                ModuleId: "engine-1",
                CommandType: "engine.turnRightStep",
                Status: CommandResultStatus.Deferred,
                EffectiveGameTimeMs: 1000,
                ReasonCode: CommandReasonCodes.Busy),
            new CommandResult(
                CommandId: "cmd-3",
                ObjectId: "ship-1",
                ModuleId: "engine-1",
                CommandType: "engine.unknown",
                Status: CommandResultStatus.Rejected,
                EffectiveGameTimeMs: 1000,
                ReasonCode: CommandReasonCodes.UnknownCommandType));

        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 1000,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray<ObjectMotionSnapshot>.Empty,
            PlayerShipObjectId: "ship-1",
            CommandResults: results);

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(3, roundTripped!.CommandResults.Length);

        var first = roundTripped.CommandResults[0];
        Assert.Equal("cmd-1", first.CommandId);
        Assert.Equal("ship-1", first.ObjectId);
        Assert.Equal("engine-1", first.ModuleId);
        Assert.Equal("engine.accelerate", first.CommandType);
        Assert.Equal(CommandResultStatus.Executed, first.Status);
        Assert.Equal(1000, first.EffectiveGameTimeMs);
        Assert.Null(first.ReasonCode);

        Assert.Equal(CommandResultStatus.Deferred, roundTripped.CommandResults[1].Status);
        Assert.Equal(CommandReasonCodes.Busy, roundTripped.CommandResults[1].ReasonCode);
        Assert.Equal(CommandResultStatus.Rejected, roundTripped.CommandResults[2].Status);
        Assert.Equal(CommandReasonCodes.UnknownCommandType, roundTripped.CommandResults[2].ReasonCode);
    }

    [Fact]
    public void Empty_snapshot_round_trips_with_empty_command_results()
    {
        // The engine's BuildSnapshot always drains to ImmutableArray.Empty (never a
        // default instance) for a tick without results — mirror that here.
        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 500,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray<ObjectMotionSnapshot>.Empty,
            CommandResults: ImmutableArray<CommandResult>.Empty);

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.True(roundTripped!.CommandResults.IsDefaultOrEmpty);
    }

    [Fact]
    public void Four_argument_constructor_defaults_round_trip_via_json()
    {
        // The 4-positional-argument constructor leaves CommandResults = default.
        // The JsonConverter must serialize default as [] and not throw
        // InvalidOperationException (review finding 7.3).
        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 500,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray<ObjectMotionSnapshot>.Empty);

        Assert.True(snapshot.CommandResults.IsDefault); // proves we hit the edge case

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.True(roundTripped!.CommandResults.IsDefaultOrEmpty);
        Assert.True(roundTripped!.ShipEvents.IsDefaultOrEmpty);
    }

    [Fact]
    public void Snapshot_command_results_are_immutable()
    {
        var builder = ImmutableArray.CreateBuilder<CommandResult>(1);
        builder.Add(new CommandResult(
            CommandId: "cmd-1",
            ObjectId: "ship-1",
            ModuleId: "engine-1",
            CommandType: "engine.accelerate",
            Status: CommandResultStatus.Executed,
            EffectiveGameTimeMs: 0));
        var results = builder.MoveToImmutable();

        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray<ObjectMotionSnapshot>.Empty,
            CommandResults: results);

        // The snapshot carries the array handed to it — no defensive copy, no
        // mutable surface to mutate externally (ImmutableArray wraps the same
        // underlying array, so content equality holds).
        Assert.Equal(results, snapshot.CommandResults);
        Assert.Single(snapshot.CommandResults);
        Assert.Equal("cmd-1", snapshot.CommandResults[0].CommandId);
    }

    [Fact]
    public void Legacy_command_and_result_json_keep_null_quote_and_receipt()
    {
        const string commandJson =
            "{\"CommandId\":\"cmd-1\",\"ClientSequence\":3,\"ObjectId\":\"ship-1\",\"ModuleId\":\"container-1\"," +
            "\"CommandType\":\"trade.buy\",\"ItemTypeId\":\"item.ore\",\"Quantity\":5}";
        const string resultJson =
            "{\"CommandId\":\"cmd-1\",\"ObjectId\":\"ship-1\",\"ModuleId\":\"container-1\"," +
            "\"CommandType\":\"trade.buy\",\"Status\":0,\"EffectiveGameTimeMs\":1000,\"ReasonCode\":null,\"ExecutedQuantity\":null}";

        var command = JsonSerializer.Deserialize<PlayerCommand>(commandJson);
        var result = JsonSerializer.Deserialize<CommandResult>(resultJson);

        Assert.NotNull(command);
        Assert.Equal(5, command!.Quantity);
        Assert.Null(command.QuoteId);
        Assert.Null(command.MarketRevision);
        Assert.NotNull(result);
        Assert.Null(result!.TradeReceipt);
        Assert.Null(result.ExecutedQuantity);

        var legacy = new PlayerCommand("cmd-2", 1, "ship-1", "container-1", TradeCommandTypes.Sell, ItemTypeId: "item.ore", Quantity: 2);
        Assert.Null(legacy.QuoteId);
        Assert.Null(legacy.MarketRevision);
        Assert.Equal(legacy, JsonSerializer.Deserialize<PlayerCommand>(JsonSerializer.Serialize(legacy)));
    }

    [Fact]
    public void Quoted_command_roundtrips_quote_id_revision_and_quantity()
    {
        var command = new PlayerCommand(
            CommandId: "cmd-q",
            ClientSequence: ulong.MaxValue,
            ObjectId: "ship-1",
            ModuleId: "container-1",
            CommandType: TradeCommandTypes.Sell,
            ItemTypeId: "item.ore",
            Quantity: long.MaxValue,
            QuoteId: "QTE-abc-17",
            MarketRevision: long.MaxValue - 1);

        var json = JsonSerializer.Serialize(command);
        var roundTripped = JsonSerializer.Deserialize<PlayerCommand>(json);

        Assert.Contains("\"QuoteId\":\"QTE-abc-17\"", json);
        Assert.Equal(command, roundTripped);
        Assert.Equal(ulong.MaxValue, roundTripped!.ClientSequence);
        Assert.Equal(long.MaxValue, roundTripped.Quantity);
        Assert.Equal(long.MaxValue - 1, roundTripped.MarketRevision);

        // A partially specified binding stays representable so the engine can reject it with invalid_quote.
        var partial = command with { MarketRevision = null };
        Assert.Equal(partial, JsonSerializer.Deserialize<PlayerCommand>(JsonSerializer.Serialize(partial)));
    }

    [Fact]
    public void Full_partial_and_rejected_trade_receipts_roundtrip_exact_totals()
    {
        var full = new CommandResult("cmd-buy", "ship-1", "container-1", TradeCommandTypes.Buy, CommandResultStatus.Executed, 1000,
            TradeReceipt: new TradeExecutionReceipt("station-1", "item.ore", "QTE-a-1", 4, 5, 10, 10, 1237));
        var partial = new CommandResult("cmd-sell", "ship-1", "container-1", TradeCommandTypes.Sell, CommandResultStatus.Executed, 1000,
            ExecutedQuantity: 6,
            TradeReceipt: new TradeExecutionReceipt("station-1", "item.ore", "QTE-a-2", 5, 6, 10, 6, 431,
                ImmutableArray.Create("station_budget", "station_capacity")));
        var rejected = new CommandResult("cmd-stale", "ship-1", "container-1", TradeCommandTypes.Sell, CommandResultStatus.Rejected, 1000,
            ReasonCode: CommandReasonCodes.StaleQuote,
            TradeReceipt: new TradeExecutionReceipt("station-1", "item.ore", "QTE-a-1", 4, 6, -3, 0, 0));
        var unresolved = new CommandResult("cmd-bad", "ship-1", "container-1", TradeCommandTypes.Buy, CommandResultStatus.Rejected, 1000,
            ReasonCode: CommandReasonCodes.InvalidQuote,
            TradeReceipt: new TradeExecutionReceipt(null, null, null, null, null, null, 0, 0));

        foreach (var original in new[] { full, partial, rejected, unresolved })
        {
            var roundTripped = JsonSerializer.Deserialize<CommandResult>(JsonSerializer.Serialize(original));
            Assert.NotNull(roundTripped);
            var expected = original.TradeReceipt!;
            var actual = roundTripped!.TradeReceipt!;
            Assert.Equal(original.Status, roundTripped.Status);
            Assert.Equal(original.ReasonCode, roundTripped.ReasonCode);
            Assert.Equal(original.ExecutedQuantity, roundTripped.ExecutedQuantity);
            Assert.Equal(expected.StationObjectId, actual.StationObjectId);
            Assert.Equal(expected.ItemTypeId, actual.ItemTypeId);
            Assert.Equal(expected.QuoteId, actual.QuoteId);
            Assert.Equal(expected.QuotedMarketRevision, actual.QuotedMarketRevision);
            Assert.Equal(expected.ResultMarketRevision, actual.ResultMarketRevision);
            Assert.Equal(expected.RequestedQuantity, actual.RequestedQuantity);
            Assert.Equal(expected.ExecutedQuantity, actual.ExecutedQuantity);
            Assert.Equal(expected.TotalCredits, actual.TotalCredits);
            Assert.Equal(expected.LimitReasons.IsDefault ? [] : expected.LimitReasons.ToArray(), actual.LimitReasons.ToArray());
        }

        var big = new TradeExecutionReceipt("s", "i", "q", long.MaxValue - 1, long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue);
        var bigRoundTripped = JsonSerializer.Deserialize<TradeExecutionReceipt>(JsonSerializer.Serialize(big))!;
        Assert.Equal(long.MaxValue - 1, bigRoundTripped.QuotedMarketRevision);
        Assert.Equal(long.MaxValue, bigRoundTripped.ResultMarketRevision);
        Assert.Equal(long.MaxValue, bigRoundTripped.TotalCredits);
    }

    [Fact]
    public void Receipt_default_limit_reasons_roundtrip_as_empty()
    {
        var receipt = new TradeExecutionReceipt("station-1", "item.ore", "QTE-a-1", 1, 2, 3, 3, 30);
        Assert.True(receipt.LimitReasons.IsDefault);

        var json = JsonSerializer.Serialize(receipt);
        var roundTripped = JsonSerializer.Deserialize<TradeExecutionReceipt>(json);

        Assert.Contains("\"LimitReasons\":[]", json);
        Assert.NotNull(roundTripped);
        Assert.True(roundTripped!.LimitReasons.IsDefaultOrEmpty);

        var withoutField = JsonSerializer.Deserialize<TradeExecutionReceipt>(
            "{\"StationObjectId\":\"station-1\",\"ItemTypeId\":\"item.ore\",\"QuoteId\":\"QTE-a-1\"," +
            "\"QuotedMarketRevision\":1,\"ResultMarketRevision\":2,\"RequestedQuantity\":3,\"ExecutedQuantity\":3,\"TotalCredits\":30}");
        Assert.NotNull(withoutField);
        Assert.True(withoutField!.LimitReasons.IsDefaultOrEmpty);
    }

    [Fact]
    public void Existing_command_status_values_and_executed_quantity_are_unchanged()
    {
        Assert.Equal(0, (int)CommandResultStatus.Executed);
        Assert.Equal(1, (int)CommandResultStatus.Rejected);
        Assert.Equal(2, (int)CommandResultStatus.Deferred);
        Assert.Equal(3, (int)CommandResultStatus.Cancelled);
        Assert.Equal(4, (int)CommandResultStatus.Failed);

        var positional = new CommandResult("cmd-1", "ship-1", "container-1", TradeCommandTypes.Sell, CommandResultStatus.Executed, 1000, null, 7);
        Assert.Equal(7, positional.ExecutedQuantity);
        Assert.Null(positional.TradeReceipt);

        Assert.Equal("quote_required", CommandReasonCodes.QuoteRequired);
        Assert.Equal("stale_quote", CommandReasonCodes.StaleQuote);
        Assert.Equal("invalid_quote", CommandReasonCodes.InvalidQuote);
        Assert.Equal("station_budget_exceeded", CommandReasonCodes.StationBudgetExceeded);
        Assert.Equal("station_capacity_exceeded", CommandReasonCodes.StationCapacityExceeded);
        Assert.Equal("fuel_trade_forbidden", CommandReasonCodes.FuelTradeForbidden);
        Assert.Equal("insufficient_cargo_quantity", CommandReasonCodes.InsufficientCargoQuantity);
    }
}
