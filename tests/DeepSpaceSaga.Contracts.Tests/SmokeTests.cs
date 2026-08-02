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
    }

    [Fact]
    public void AuthoritativeSnapshot_is_instantiable()
    {
        var objects = new List<ObjectMotionSnapshot>
        {
            new("obj-1", 100, 200, 50, 0)
        };

        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 1000,
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
