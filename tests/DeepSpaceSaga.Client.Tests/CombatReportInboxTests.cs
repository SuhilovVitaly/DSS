using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Client.Tests.CombatReportMailboxTests;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatReportInboxTests
{
    [Fact]
    public void TwoUpdatesBeforeDrainKeepEveryReport()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(Snapshot(1, 11, Report(11)));
        buffer.Update(Snapshot(2, 12, Report(12)));
        buffer.Update(new(3, 0, SimulationSpeed.Speed0, []));
        Assert.Equal(new long[] { 11, 12 }, buffer.DrainCombatReports().Select(r => r.Sequence));
        Assert.Empty(buffer.DrainCombatReports());
    }

    [Fact]
    public void FirstSnapshotNewReportsAreNotSuppressed()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(Snapshot(5, 12, Report(11), Report(12)));
        Assert.Equal(2, buffer.DrainCombatReports().Length);
    }

    [Fact]
    public void DuplicateAndOutOfOrderDelivery()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(Snapshot(3, 13, Report(13)));
        Assert.Empty(buffer.DrainCombatReports());
        buffer.Update(Snapshot(1, 11, Report(11)));
        buffer.Update(Snapshot(2, 12, Report(12)));
        Assert.Equal(new long[] { 11, 12, 13 }, buffer.DrainCombatReports().Select(r => r.Sequence));
        buffer.Update(Snapshot(1, 11, Report(11)));
        Assert.Empty(buffer.DrainCombatReports());
        Assert.Throws<InvalidOperationException>(() => buffer.Update(Snapshot(4, 11, Report(11) with { EpisodeId = "conflict" })));
    }

    [Fact]
    public async Task ConcurrentUpdateAndDrain()
    {
        var buffer = new SnapshotBuffer();
        var received = new List<long>();
        var writer = Task.Run(() =>
        {
            for (int i = 11; i <= 1010; i++) buffer.Update(Snapshot((ulong)i, i, Report(i)));
        });
        while (!writer.IsCompleted)
        {
            received.AddRange(buffer.DrainCombatReports().Select(r => r.Sequence));
            await Task.Yield();
        }
        await writer;
        received.AddRange(buffer.DrainCombatReports().Select(r => r.Sequence));
        Assert.Equal(Enumerable.Range(11, 1000).Select(i => (long)i), received);
    }

    [Fact]
    public void SessionBufferIsolationAndBoundedMemory()
    {
        var first = new SnapshotBuffer();
        first.Update(Snapshot(1, 11, Report(11)));
        Assert.Empty(new SnapshotBuffer().DrainCombatReports());
        Assert.Single(first.DrainCombatReports());
        var inbox = new CombatReportInbox();
        for (int i = 11; i < 10011; i++)
        {
            inbox.Receive(new(10, i, [Report(i)]));
            Assert.Single(inbox.Drain());
        }
        Assert.Equal(0, inbox.PendingCount);
        Assert.Equal(4096, inbox.RecentCount);
        var overflow = new CombatReportInbox();
        overflow.Receive(new(10, 4106, Enumerable.Range(11, 4096).Select(i => Report(i)).ToImmutableArray()));
        Assert.Throws<InvalidOperationException>(() => overflow.Receive(new(10, 4107, [Report(4107)])));
        Assert.Equal(4096, overflow.PendingCount);
    }
}
