using System.Collections.Immutable;
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
            CommandType: "engine.match-target-speed",
            TargetObjectId: "obj-2");

        Assert.Equal("obj-2", command.TargetObjectId);
    }

    [Fact]
    public void ShipEngineCommandTypes_expose_stable_ids()
    {
        Assert.Equal("engine.accelerate", ShipEngineCommandTypes.Accelerate);
        Assert.Equal("engine.maintain-speed", ShipEngineCommandTypes.MaintainSpeed);
        Assert.Equal("engine.maintain-course", ShipEngineCommandTypes.MaintainCourse);
        Assert.Equal("engine.match-target-speed", ShipEngineCommandTypes.MatchTargetSpeed);
        Assert.Equal("engine.match-target-course", ShipEngineCommandTypes.MatchTargetCourse);
        Assert.Equal("engine.cancel-all", ShipEngineCommandTypes.CancelAll);
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
}
