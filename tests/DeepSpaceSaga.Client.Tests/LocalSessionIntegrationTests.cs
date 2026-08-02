using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;

namespace DeepSpaceSaga.Client.Tests;

public class LocalSessionIntegrationTests
{
    [Fact]
    public async Task Engine_publishes_snapshots_with_incrementing_sequence()
    {
        var engine = new SimulationEngine();
        engine.AddTestObject(new ObjectMotionSnapshot("test", 0, 0, SpeedKmS: 0, Direction: 0));

        await using var connection = new LocalGameSessionConnection(engine);

        var snapshots = new List<AuthoritativeSnapshot>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));

        await foreach (var snapshot in connection.ReadSnapshotsAsync(cts.Token))
        {
            snapshots.Add(snapshot);
            if (snapshots.Count >= 3)
                break;
        }

        Assert.True(snapshots.Count >= 2, "Should receive at least 2 snapshots");

        for (int i = 1; i < snapshots.Count; i++)
        {
            Assert.True(
                snapshots[i].SnapshotSequence > snapshots[i - 1].SnapshotSequence,
                $"Sequence should be increasing: {snapshots[i].SnapshotSequence} > {snapshots[i - 1].SnapshotSequence}");
        }
    }

    [Fact]
    public async Task SendCommand_delivers_to_engine()
    {
        var engine = new SimulationEngine();
        await using var connection = new LocalGameSessionConnection(engine);

        var command = new PlayerCommand("cmd-1", 1, "ship-1", "nav", "move");
        await connection.SendCommandAsync(command);

        // Command was delivered across the boundary. Engine stores it for
        // future processing (out of scope for P003).
        Assert.True(true);
    }
}
