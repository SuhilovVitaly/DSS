using System.Diagnostics;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

public sealed class DebugTuningTransportTests
{
    [Fact]
    public void Compact_frame_merges_revisions_and_omission_retains_confirmed_values()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        var session = Guid.NewGuid();
        var original = new DebugTuningSnapshot(0, [new("engine", "maxSpeedKmS", 5)], [new("engine", "maxSpeedKmS", 5)]);
        var state = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [], DebugTuning: original);
        buffer.UpdatePresentation(new(session, 1, 0, [new(0, state)], state, false, 0));
        var changed = original with { Revision = 1, Values = [new("engine", "maxSpeedKmS", 2)] };
        buffer.UpdatePresentation(new(session, 2, 0, [new(0, state with { DebugTuning = changed })], null, false, 0));
        now += Stopwatch.Frequency;
        buffer.PreparePresentation();
        Assert.Equal(1, buffer.Presented!.Snapshot.DebugTuning!.Revision);
        buffer.UpdatePresentation(new(session, 3, 0, [new(0, state with { DebugTuning = null })], null, false, 0));
        now += Stopwatch.Frequency;
        buffer.PreparePresentation();
        Assert.Equal(1, buffer.Presented!.Snapshot.DebugTuning!.Revision);
        Assert.Equal(2, Assert.Single(buffer.Presented.Snapshot.DebugTuning.Values).Value);
    }
}
