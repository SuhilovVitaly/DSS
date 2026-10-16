using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
using static DeepSpaceSaga.Client.Tests.CombatReportMailboxTests;
using static DeepSpaceSaga.Client.Tests.CombatReportScreenFlowTests;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatReportLifecycleTests
{
    [Fact]
    public void FailedLoadKeepsExistingHistory()
    {
        var buffer = new SnapshotBuffer(); buffer.Update(World(1, 11, Report(11)));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        screen.PrepareCombatReports(); screen.OnDeactivated(); screen.OnActivated();
        Assert.Single(screen.ReportStore.Reports); Assert.False(screen.CombatReportResourcesDisposed);
        screen.DisposeCombatReports();
    }

    [Fact]
    public void SuccessfulLoadDoesNotReplayHistory()
    {
        var old = new SnapshotBuffer(); old.Update(World(1, 11, Report(11)));
        var screen = new GameSessionScreen(old, new LinearMotionPredictor()); screen.PrepareCombatReports();
        var replacement = new SnapshotBuffer(); replacement.Update(World(1, 11) with { CombatReports = new(11, 11, []) });
        var next = new GameSessionScreen(replacement, new LinearMotionPredictor()); next.PrepareCombatReports();
        Assert.Empty(next.ReportStore.Reports); Assert.Single(screen.ReportStore.Reports);
        screen.DisposeCombatReports(); next.DisposeCombatReports();
    }

    [Fact]
    public void FirstNewSessionSnapshotKeepsNewReports()
    {
        var buffer = new SnapshotBuffer(); buffer.Update(World(1, 12, Report(11), Report(12)));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor()); screen.PrepareCombatReports();
        Assert.Equal(2, screen.ReportStore.Reports.Count); screen.DisposeCombatReports();
    }

    [Fact]
    public async Task OldConnectionCannotPolluteReplacement()
    {
        var oldConnection = new LateConnection(); var old = new GameSessionHandle(oldConnection);
        old.Buffer.Update(World(1, 11, Report(11)));
        var screen = new GameSessionScreen(old.Buffer, new LinearMotionPredictor(), old); screen.PrepareCombatReports();
        var closing = old.DisposeAsync().AsTask();
        Assert.True(screen.CombatReportResourcesDisposed); Assert.Empty(screen.ReportStore.Reports);
        oldConnection.Snapshots.Writer.TryWrite(World(2, 12, Report(12))); oldConnection.Snapshots.Writer.TryComplete();
        await closing;
        Assert.Equal(1ul, old.Buffer.Latest!.Snapshot.SnapshotSequence);
        var replacement = new SnapshotBuffer(); replacement.Update(World(1, 11) with { CombatReports = new(11, 11, []) });
        Assert.Empty(replacement.DrainCombatReports());
    }

    [Fact]
    public void ModalResumeKeepsHistory()
    {
        var buffer = new SnapshotBuffer(); buffer.Update(World(1, 11, Report(11)));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720)); Frame(screen, surface.Canvas);
        var bounds = screen.CombatReportsBounds; screen.OnMouseDown(bounds.MidX, bounds.Top + 10);
        Assert.True(screen.ReportPanel.HasFocus);
        screen.OnDeactivated(); Assert.False(screen.ReportPanel.HasFocus);
        screen.OnActivated(); Frame(screen, surface.Canvas); Assert.Single(screen.ReportStore.Reports);
        screen.DisposeCombatReports();
    }

    [Fact]
    public void DefeatTransitionIsImmediate()
    {
        var buffer = new SnapshotBuffer(); buffer.Update(World(1, 11, Report(11)) with
        { Outcome = new(SessionOutcomeState.Defeated, "ship", "wreck", 0, 1) });
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor()); screen.PrepareCombatReports();
        Assert.Single(screen.ReportStore.Reports);
        screen.OnDeactivated(); Assert.True(screen.CombatReportResourcesDisposed);
        screen.PrepareCombatReports(); Assert.Empty(screen.ReportStore.Reports);
    }

    private sealed class LateConnection : IGameSessionConnection
    {
        internal readonly Channel<AuthoritativeSnapshot> Snapshots = Channel.CreateUnbounded<AuthoritativeSnapshot>();
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await foreach (var snapshot in Snapshots.Reader.ReadAllAsync()) yield return snapshot; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
