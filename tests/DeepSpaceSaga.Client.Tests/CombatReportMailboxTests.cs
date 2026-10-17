using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatReportMailboxTests
{
    internal static BattleReportSnapshot Report(long sequence) => new(sequence, 12.5, BattleReportKind.PlayerTorpedoLaunch,
        $"projectile-{sequence}", new(BattleReportReporterRole.TorpedoOperator));
    internal static AuthoritativeSnapshot Snapshot(ulong tick, long latest, params BattleReportSnapshot[] reports) =>
        new(tick, 0, SimulationSpeed.Speed0, [], CombatReports: new(10, latest, reports.ToImmutableArray()));

    [Fact]
    public async Task CoalescingPreservesAllReportDeltas()
    {
        var mailbox = new SnapshotMailbox();
        mailbox.Publish(Snapshot(1, 11, Report(11)));
        mailbox.Publish(Snapshot(2, 13, Report(13), Report(12)));
        mailbox.Publish(Snapshot(3, 13));
        mailbox.Complete();
        await foreach (var snapshot in mailbox.ReadAllAsync(default))
        {
            Assert.Equal(3ul, snapshot.SnapshotSequence);
            Assert.Equal(new long[] { 11, 12, 13 }, snapshot.CombatReports!.Reports.Select(r => r.Sequence));
            Assert.Equal(10, snapshot.CombatReports.BaselineSequence);
            Assert.Equal(13, snapshot.CombatReports.LatestSequence);
        }
    }

    [Fact]
    public async Task RepeatedReportsDeduplicateInMailbox()
    {
        var mailbox = new SnapshotMailbox();
        mailbox.Publish(Snapshot(1, 11, Report(11)));
        mailbox.Publish(Snapshot(2, 11, Report(11)));
        mailbox.Publish(new(3, 0, SimulationSpeed.Speed0, []));
        mailbox.Complete();
        await foreach (var snapshot in mailbox.ReadAllAsync(default)) Assert.Single(snapshot.CombatReports!.Reports);
    }

    [Fact]
    public void ConflictingPayloadAndOverflowFailExplicitly()
    {
        var conflict = new SnapshotMailbox();
        conflict.Publish(Snapshot(1, 11, Report(11)));
        Assert.Throws<InvalidOperationException>(() => conflict.Publish(Snapshot(2, 11, Report(11) with { EpisodeId = "different" })));
        var overflow = new SnapshotMailbox();
        overflow.Publish(Snapshot(1, 4106, Enumerable.Range(11, 4096).Select(i => Report(i)).ToArray()));
        Assert.Throws<InvalidOperationException>(() => overflow.Publish(Snapshot(2, 4107, Report(4107))));
        var baseline = new SnapshotMailbox();
        baseline.Publish(Snapshot(1, 11, Report(11)));
        Assert.Throws<InvalidOperationException>(() => baseline.Publish(Snapshot(2, 11) with { CombatReports = new(9, 11, []) }));
    }

    [Fact]
    public async Task OtherEventStreamsStillMerge()
    {
        var mailbox = new SnapshotMailbox();
        mailbox.Publish(Snapshot(1, 11, Report(11)) with { ShipEvents = [new("event", "ship", "module", "completed", null, 0)] });
        mailbox.Publish(Snapshot(2, 12, Report(12)) with { CommandResults = [new("command", "ship", "module", "fire", CommandResultStatus.Executed, 0)] });
        mailbox.Complete();
        await foreach (var snapshot in mailbox.ReadAllAsync(default))
        {
            Assert.Equal(2, snapshot.CombatReports!.Reports.Length);
            Assert.Single(snapshot.ShipEvents);
            Assert.Single(snapshot.CommandResults);
        }
    }
}
