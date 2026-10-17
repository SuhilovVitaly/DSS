using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;
using static DeepSpaceSaga.Client.Tests.CombatReportMailboxTests;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatReportScreenFlowTests
{
    internal static AuthoritativeSnapshot World(ulong tick, long latest, params BattleReportSnapshot[] reports) =>
        Snapshot(tick, latest, reports) with
        {
            Objects = [new("ship", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip),
                new("enemy", 150, 0, 0, 0, RenderObjectType: SpaceObjectType.NpcShip, DisplayName: "Enemy")],
            PlayerShipObjectId = "ship",
            SelectedObjectId = "enemy"
        };

    internal static void Frame(GameSessionScreen screen, SKCanvas canvas, int width = 1280, int height = 720)
    { screen.PrepareCombatReports(); screen.Render(canvas, width, height); }

    [Fact]
    public void FirstFramePreservesBatchAndLatestExpansion()
    {
        var buffer = new SnapshotBuffer(); buffer.Update(World(1, 13, Report(11), Report(12), Report(13)));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720)); Frame(screen, surface.Canvas);
        Assert.Equal(3, screen.ReportStore.Reports.Count); Assert.Equal(13, screen.ReportStore.ExpandedSequence);
        Assert.False(screen.ReportPanel.HasFocus); Assert.True(screen.CombatReportsBounds.Height >= 80);
    }

    [Fact]
    public void NewEventDoesNotStealFocus()
    {
        var buffer = new SnapshotBuffer(); buffer.Update(World(1, 11, Report(11)));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720)); Frame(screen, surface.Canvas);
        buffer.Update(World(2, 12, Report(12))); Frame(screen, surface.Canvas);
        Assert.Equal(12, screen.ReportStore.ExpandedSequence); Assert.False(screen.ReportPanel.HasFocus);
    }

    [Fact]
    public void KeyboardCanBrowseReportsAndReturnToMapWithoutMouse()
    {
        var buffer = new SnapshotBuffer(); buffer.Update(World(1, 12, Report(11), Report(12)));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720)); Frame(screen, surface.Canvas);
        var focus = (screen.CameraFocusX, screen.CameraFocusY);
        var selected = screen.SelectedObjectId;
        var tracker = new KeyboardEdgeTracker();
        var pressed = new Key[32]; var released = new Key[32];
        void Press(Key key)
        {
            var edges = tracker.PollBoth(k => k == key, pressed, released);
            foreach (var edge in pressed.Take(edges.pressedCount)) screen.OnKeyDown(edge);
            Assert.Equal(0, tracker.PollBoth(k => k == key, pressed, released).pressedCount);
            edges = tracker.PollBoth(_ => false, pressed, released);
            foreach (var edge in released.Take(edges.releasedCount)) screen.OnKeyUp(edge);
        }

        Press(Key.Tab); Assert.True(screen.ReportPanel.HasFocus);
        Press(Key.Down); Assert.Equal(11, screen.ReportStore.KeyboardSelection);
        Assert.Equal(12, screen.ReportStore.ExpandedSequence);
        Press(Key.Enter); Assert.Equal(11, screen.ReportStore.ExpandedSequence);
        Assert.Equal(focus, (screen.CameraFocusX, screen.CameraFocusY));
        Assert.Equal(selected, screen.SelectedObjectId);
        Press(Key.Tab); Assert.False(screen.ReportPanel.HasFocus);
        Press(Key.Tab); Assert.True(screen.ReportPanel.HasFocus);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape));
        Assert.False(screen.ReportPanel.HasFocus);
        Assert.Equal(ScreenEvent.OpenGameMenu, screen.OnKeyDown(Key.Escape));
        screen.DisposeCombatReports();
    }

    [Fact]
    public async Task PanelInputLeavesCameraClockAndSelection()
    {
        var connection = new ProbeConnection(); await using var handle = new GameSessionHandle(connection);
        handle.Buffer.Update(World(1, 12, Report(11), Report(12)));
        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720)); Frame(screen, surface.Canvas);
        var camera = (screen.CameraFocusX, screen.CameraFocusY, screen.CameraPixelsPerWorldUnit);
        var selection = screen.SelectedObjectId; var speed = handle.Buffer.CurrentSpeed;
        var bounds = screen.CombatReportsBounds; float x = bounds.MidX, y = bounds.Top + 10;
        screen.OnMouseDown(x, y, MouseButton.Left); screen.OnMouseUp(x, y);
        screen.OnMouseWheel(x, y, -1); screen.OnMouseMove(x, y);
        screen.OnKeyDown(Key.Down); screen.OnKeyDown(Key.Enter);
        screen.OnMouseDown(x, y, MouseButton.Right);
        Assert.Equal(camera, (screen.CameraFocusX, screen.CameraFocusY, screen.CameraPixelsPerWorldUnit));
        Assert.Equal(selection, screen.SelectedObjectId); Assert.Equal(speed, handle.Buffer.CurrentSpeed);
        Assert.Equal(0, connection.Commands); Assert.Equal(0, connection.SpeedChanges);
    }

    [Fact]
    public void ManualBrowseDoesNotTrackMapSelection()
    {
        var buffer = new SnapshotBuffer(); buffer.Update(World(1, 12, Report(11), Report(12)));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720)); Frame(screen, surface.Canvas);
        screen.ReportStore.Expand(11); Frame(screen, surface.Canvas);
        buffer.Update(World(2, 12) with { SelectedObjectId = null }); Frame(screen, surface.Canvas);
        Assert.Equal(11, screen.ReportStore.ExpandedSequence); Assert.Equal(2, screen.ReportStore.Reports.Count);
    }

    [Fact]
    public void RenderDoesNotDrainReceiveQueue()
    {
        var buffer = new SnapshotBuffer(); buffer.Update(World(1, 11, Report(11)));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        screen.Render(surface.Canvas, 1280, 720); Assert.Empty(screen.ReportStore.Reports);
        Frame(screen, surface.Canvas); Assert.Single(screen.ReportStore.Reports);
    }

    private sealed class ProbeConnection : IGameSessionConnection
    {
        private readonly Channel<AuthoritativeSnapshot> _snapshots = Channel.CreateUnbounded<AuthoritativeSnapshot>();
        public int Commands, SpeedChanges;
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) { Commands++; return ValueTask.CompletedTask; }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) { SpeedChanges++; return ValueTask.CompletedTask; }
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await foreach (var snapshot in _snapshots.Reader.ReadAllAsync(cancellationToken)) yield return snapshot; }
        public ValueTask DisposeAsync() { _snapshots.Writer.TryComplete(); return ValueTask.CompletedTask; }
    }
}
