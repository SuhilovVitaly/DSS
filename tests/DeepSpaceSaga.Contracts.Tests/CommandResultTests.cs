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
        Assert.Equal("invalid_package_quantity", CommandReasonCodes.InvalidPackageQuantity);
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
}
