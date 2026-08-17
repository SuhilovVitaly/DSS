using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class SmokeTests
{
    [Fact]
    public void PlayerCommand_is_instantiable()
    {
        var command = new PlayerCommand(
            CommandId: "cmd-1",
            ClientSequence: 1,
            ObjectId: "ship-1",
            ModuleId: "nav",
            CommandType: "move");

        Assert.NotNull(command);
        Assert.Null(command.TargetObjectId);
    }

    [Fact]
    public void PlayerCommand_carries_explicit_target_object_id()
    {
        var command = new PlayerCommand(
            CommandId: "cmd-2",
            ClientSequence: 2,
            ObjectId: "ship-1",
            ModuleId: "nav",
            CommandType: "engine.speed-synchronization",
            TargetObjectId: "obj-2");

        Assert.Equal("obj-2", command.TargetObjectId);
    }

    [Fact]
    public void PlayerCommand_serialization_round_trip_preserves_target_object_id()
    {
        // AC4: a match command's explicit target survives serialization/deserialization —
        // required so the target reaches the authoritative engine across the connection.
        var command = new PlayerCommand(
            CommandId: "cmd-1",
            ClientSequence: 1,
            ObjectId: "ship-1",
            ModuleId: "engine-1",
            CommandType: "engine.speed-synchronization",
            TargetObjectId: "obj-2");

        var json = JsonSerializer.Serialize(command);
        var roundTripped = JsonSerializer.Deserialize<PlayerCommand>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal("cmd-1", roundTripped!.CommandId);
        Assert.Equal(1ul, roundTripped.ClientSequence);
        Assert.Equal("ship-1", roundTripped.ObjectId);
        Assert.Equal("engine-1", roundTripped.ModuleId);
        Assert.Equal("engine.speed-synchronization", roundTripped.CommandType);
        Assert.Equal("obj-2", roundTripped.TargetObjectId);
    }

    [Fact]
    public void PlayerCommand_serialization_round_trip_preserves_null_target()
    {
        // AC4 (null-target branch): a command without a target stays without one
        // after the round trip.
        var command = new PlayerCommand(
            CommandId: "cmd-2",
            ClientSequence: 2,
            ObjectId: "ship-1",
            ModuleId: "engine-1",
            CommandType: "engine.accelerate");

        var json = JsonSerializer.Serialize(command);
        var roundTripped = JsonSerializer.Deserialize<PlayerCommand>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal("cmd-2", roundTripped!.CommandId);
        Assert.Equal("engine.accelerate", roundTripped.CommandType);
        Assert.Null(roundTripped.TargetObjectId);
    }

    [Fact]
    public void ShipEngineCommandTypes_expose_stable_ids()
    {
        Assert.Equal("engine.accelerate", ShipEngineCommandTypes.Accelerate);
        Assert.Equal("engine.maintain-speed", ShipEngineCommandTypes.MaintainSpeed);
        Assert.Equal("engine.maintain-course", ShipEngineCommandTypes.MaintainCourse);
        Assert.Equal("engine.speed-synchronization", ShipEngineCommandTypes.SpeedSynchronization);
        Assert.Equal("engine.direction-synchronization", ShipEngineCommandTypes.DirectionSynchronization);
        Assert.Equal("engine.cancel-all", ShipEngineCommandTypes.CancelAll);
    }

    [Fact]
    public void ScannerCommandTypes_expose_stable_ids()
    {
        Assert.Equal("scanner.general-scan", ScannerCommandTypes.GeneralScan);
        Assert.Equal("scanner.structural-scan", ScannerCommandTypes.StructuralScan);
    }

    [Fact]
    public void AuthoritativeSnapshot_is_instantiable()
    {
        var objects = ImmutableArray.Create(
            new ObjectMotionSnapshot("obj-1", 100, 200, SpeedKmS: 5, Direction: 90));

        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 1000,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: objects);

        Assert.NotNull(snapshot);
        Assert.Single(snapshot.Objects);
    }

    [Fact]
    public void IGameSessionConnection_is_defined()
    {
        var type = typeof(IGameSessionConnection);
        Assert.True(type.IsInterface);
    }

    [Fact]
    public void IGameSessionConnection_declares_SetObjectInteractionStateAsync()
    {
        var method = typeof(IGameSessionConnection).GetMethod("SetObjectInteractionStateAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void AuthoritativeSnapshot_defaults_object_interaction_ids_to_null()
    {
        // Backward compatibility: existing callers that never mention the new
        // trailing parameters must still compile and get null for both.
        var objects = ImmutableArray.Create(
            new ObjectMotionSnapshot("obj-1", 100, 200, SpeedKmS: 5, Direction: 90));

        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 1000,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: objects);

        Assert.Null(snapshot.ActiveObjectId);
        Assert.Null(snapshot.SelectedObjectId);
    }

    [Fact]
    public void AuthoritativeSnapshot_serialization_round_trip_preserves_object_interaction_ids()
    {
        var objects = ImmutableArray.Create(
            new ObjectMotionSnapshot("obj-1", 100, 200, SpeedKmS: 5, Direction: 90));

        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 1000,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: objects,
            ActiveObjectId: "obj-1",
            SelectedObjectId: "obj-2");

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal("obj-1", roundTripped!.ActiveObjectId);
        Assert.Equal("obj-2", roundTripped.SelectedObjectId);
    }

    [Fact]
    public void AuthoritativeSnapshot_serialization_round_trip_preserves_null_object_interaction_ids()
    {
        var objects = ImmutableArray.Create(
            new ObjectMotionSnapshot("obj-1", 100, 200, SpeedKmS: 5, Direction: 90));

        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 1000,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: objects);

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.Null(roundTripped!.ActiveObjectId);
        Assert.Null(roundTripped.SelectedObjectId);
    }
}
