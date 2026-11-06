using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;

namespace DeepSpaceSaga.Client.Tests;

public sealed class WorldSnapshotMailboxTests
{
    [Fact]
    public async Task CoalescingRetainsWorldEventsAndLatestCommandStatus()
    {
        var mailbox = new SnapshotMailbox();
        mailbox.Publish(new(1, 0, SimulationSpeed.Speed0, [],
            CommandResults: [new("command", "ship", "module", "engine.accelerate", CommandResultStatus.Deferred, 0)],
            ShipEvents: [new("event", "ship", "module", "completed", null, 0)]));
        mailbox.Publish(new(2, 100, SimulationSpeed.Speed1, [],
            CommandResults: [new("command", "ship", "module", "engine.accelerate", CommandResultStatus.Executed, 100)]));
        mailbox.Complete();
        await using var reader = mailbox.ReadAllAsync(default).GetAsyncEnumerator();
        Assert.True(await reader.MoveNextAsync());
        var snapshot = reader.Current;
        Assert.Equal(2ul, snapshot.SnapshotSequence);
        Assert.Equal(SimulationSpeed.Speed1, snapshot.CurrentSpeed);
        Assert.Single(snapshot.ShipEvents);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(snapshot.CommandResults).Status);
        Assert.False(await reader.MoveNextAsync());
    }
}
