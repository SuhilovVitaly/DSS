using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

public sealed class DebugTuningTransportTests
{
    [Fact]
    public void World_snapshots_publish_tuning_and_stale_packets_do_not_restore_old_values()
    {
        var buffer = new SnapshotBuffer(() => 0);
        var original = new DebugTuningSnapshot(0, [new("engine", "maxSpeedKmS", 5)], [new("engine", "maxSpeedKmS", 5)]);
        var state = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [], DebugTuning: original);
        buffer.Update(state);
        var changed = original with { Revision = 1, Values = [new("engine", "maxSpeedKmS", 2)] };
        buffer.Update(state with { SnapshotSequence = 2, DebugTuning = changed });
        buffer.Update(state);
        Assert.Equal(1, buffer.Presented!.Snapshot.DebugTuning!.Revision);
        Assert.Equal(2, Assert.Single(buffer.Presented.Snapshot.DebugTuning.Values).Value);
    }
}
