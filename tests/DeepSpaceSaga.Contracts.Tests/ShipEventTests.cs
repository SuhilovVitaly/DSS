using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class ShipEventTests
{
    [Fact]
    public void ShipEvent_is_instantiable()
    {
        var e = new ShipEvent(
            "EVE-000001",
            "ship-1",
            "engine-1",
            ShipEventTypes.CommandCompleted,
            ReasonCode: null,
            GameTimeMs: 500);

        Assert.Equal("EVE-000001", e.EventId);
        Assert.Equal("ship-1", e.ObjectId);
        Assert.Equal("engine-1", e.ModuleId);
        Assert.Equal(ShipEventTypes.CommandCompleted, e.EventType);
        Assert.Null(e.ReasonCode);
        Assert.Equal(500, e.GameTimeMs);
    }

    [Fact]
    public void ShipEventTypes_are_snake_case()
    {
        Assert.Equal("command_completed", ShipEventTypes.CommandCompleted);
        Assert.Equal("cycle_cancelled", ShipEventTypes.CycleCancelled);
        Assert.Equal("cycle_interrupted", ShipEventTypes.CycleInterrupted);
    }

    [Fact]
    public void ShipEventReasonCodes_are_snake_case()
    {
        Assert.Equal("cancelled_by_command", ShipEventReasonCodes.CancelledByCommand);
        Assert.Equal("power_off", ShipEventReasonCodes.PowerOff);
        Assert.Equal("no_power", ShipEventReasonCodes.NoPower);
        Assert.Equal("module_destroyed", ShipEventReasonCodes.ModuleDestroyed);
        Assert.Equal("module_disabled", ShipEventReasonCodes.ModuleDisabled);
        Assert.Equal("incompatible_state", ShipEventReasonCodes.IncompatibleState);
    }

    [Fact]
    public void AuthoritativeSnapshot_round_trips_ship_events()
    {
        var completed = new ShipEvent("EVE-000001", "ship-1", "engine-1",
            ShipEventTypes.CommandCompleted, ReasonCode: null, GameTimeMs: 500);
        var cancelled = new ShipEvent("EVE-000002", "ship-1", "engine-1",
            ShipEventTypes.CycleCancelled, ShipEventReasonCodes.CancelledByCommand, GameTimeMs: 600);

        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 700,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray<ObjectMotionSnapshot>.Empty,
            ShipEvents: ImmutableArray.Create(completed, cancelled));

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(2, roundTripped!.ShipEvents.Length);

        var r0 = roundTripped.ShipEvents[0];
        Assert.Equal("EVE-000001", r0.EventId);
        Assert.Equal("ship-1", r0.ObjectId);
        Assert.Equal("engine-1", r0.ModuleId);
        Assert.Equal(ShipEventTypes.CommandCompleted, r0.EventType);
        Assert.Null(r0.ReasonCode);
        Assert.Equal(500, r0.GameTimeMs);

        var r1 = roundTripped.ShipEvents[1];
        Assert.Equal("EVE-000002", r1.EventId);
        Assert.Equal(ShipEventTypes.CycleCancelled, r1.EventType);
        Assert.Equal(ShipEventReasonCodes.CancelledByCommand, r1.ReasonCode);
        Assert.Equal(600, r1.GameTimeMs);
    }

    [Fact]
    public void Empty_snapshot_round_trips_with_empty_ship_events()
    {
        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 500,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray<ObjectMotionSnapshot>.Empty,
            ShipEvents: ImmutableArray<ShipEvent>.Empty);

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.True(roundTripped!.ShipEvents.IsDefaultOrEmpty);
    }

    [Fact]
    public void Snapshot_ship_events_are_immutable()
    {
        var builder = ImmutableArray.CreateBuilder<ShipEvent>(1);
        builder.Add(new ShipEvent("EVE-000001", "ship-1", "engine-1",
            ShipEventTypes.CommandCompleted, ReasonCode: null, GameTimeMs: 0));
        var events = builder.MoveToImmutable();

        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray<ObjectMotionSnapshot>.Empty,
            ShipEvents: events);

        Assert.Equal(events, snapshot.ShipEvents);
        Assert.Single(snapshot.ShipEvents);
        Assert.Equal("EVE-000001", snapshot.ShipEvents[0].EventId);
    }
}
