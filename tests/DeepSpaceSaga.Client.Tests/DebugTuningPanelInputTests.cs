using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class DebugTuningPanelInputTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public async Task BindingRapidEditsConfirmationAndMouseCapture(float scale)
    {
        string directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/ep0013/panel-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string client = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
            using var engine = SimulationEngine.CreateFromScenarioFile(Path.Combine(client, "Settings.json"), Path.Combine(client, "Scenarios/PlayerShipOnly/scenario.json"));
            var snapshot = engine.CaptureSnapshotForTests();
            var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
            var connection = new ProbeConnection();
            await using var handle = new GameSessionHandle(connection, buffer, new CombatMetricsWriter(Path.Combine(directory, "metrics")), new DebugTuningFileWriter(directory));
            var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), handle, uiScale: scale,
                combatInputs: CombatInputSettings.Default with { DebugPanel = Key.F11 });
            using var surface = SKSurface.Create(new SKImageInfo(1920, 1200));
            screen.Render(surface.Canvas, 1920, 1200);
            screen.OnKeyDown(Key.F12); Assert.False(screen.DebugPanel.Visible);
            screen.OnKeyDown(Key.F11); Assert.True(screen.DebugPanel.Visible);
            screen.Render(surface.Canvas, 1920, 1200);
            var row = screen.DebugPanel.RowBounds(0);
            float x = (row.Right - 45) * scale, y = row.MidY * scale;
            double zoom = screen.CameraPixelsPerWorldUnit;
            var selected = screen.SelectedObjectId;
            screen.OnMouseDown(x, y);
            Assert.Single(connection.Commands);
            Assert.Equal(5.5, connection.Commands[0].DebugTuning!.Values[0].Value);
            Assert.Equal(5, buffer.Latest!.Snapshot.DebugTuning!.Values[0].Value);
            Assert.Equal(5.5, screen.DebugPanel.GetRow(0).PendingValue);
            screen.OnKeyDown(Key.ControlLeft); screen.OnMouseDown(x, y); screen.OnKeyUp(Key.ControlLeft);
            Assert.Equal(5.6, connection.Commands[1].DebugTuning!.Values[0].Value, 8);
            // Wheel over the numeric cell edits, wheel elsewhere scrolls; neither reaches map.
            screen.OnMouseWheel((row.Right - 120) * scale, y, 1);
            Assert.Equal(6.1, connection.Commands[2].DebugTuning!.Values[0].Value, 8);
            screen.OnMouseDown(x, y, MouseButton.Right);
            Assert.True(screen.OnMouseMove(x, y));
            screen.OnMouseWheel(screen.DebugPanel.Bounds.MidX * scale, screen.DebugPanel.Bounds.MidY * scale, -1);
            Assert.Equal(zoom, screen.CameraPixelsPerWorldUnit); Assert.Equal(selected, screen.SelectedObjectId); Assert.Null(screen.ActiveObjectId);
            Assert.Equal(3, connection.Commands.Count); Assert.Equal(0, connection.SpeedChanges);
            var values = snapshot.DebugTuning!.Values.SetItem(0, snapshot.DebugTuning.Values[0] with { Value = 6.1 });
            buffer.Update(snapshot with
            {
                SnapshotSequence = snapshot.SnapshotSequence + 1,
                DebugTuning = snapshot.DebugTuning with { Revision = 3, Values = values },
                CommandResults = [.. connection.Commands.Select(c => new CommandResult(c.CommandId, "", "", c.CommandType, CommandResultStatus.Executed, 0))]
            });
            screen.PrepareDebugTuning();
            Assert.Null(screen.DebugPanel.GetRow(0).PendingValue); Assert.Equal(6.1, screen.DebugPanel.GetRow(0).Value.Value);
            screen.OnKeyDown(Key.Escape); Assert.False(screen.DebugPanel.Visible);
            screen.OnKeyDown(Key.F11); screen.OnKeyDown(Key.F11); Assert.False(screen.DebugPanel.Visible);
            await handle.DisposeAsync(); Assert.True(screen.DebugPanel.IsDisposed);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void BoundsFineStepsAndPendingValueUseDisplayedTarget()
    {
        using var panel = new UI.Screens.GameSession.Controls.DebugTuningPanel(k => k) { Visible = true };
        var entry = new DebugTuningValue("module.engine.tetrarch", "maxSpeedKmS", 49.9);
        panel.Prepare(800, 600, new(0, [entry], [entry with { Value = 5 }]));
        var row = panel.RowBounds(0);
        Assert.True(panel.TryEdit(row.Right - 45, row.MidY, 0, false, out int index, out double value));
        Assert.Equal(50, value); panel.SetPending(index, value);
        Assert.False(panel.TryEdit(row.Right - 45, row.MidY, 0, false, out _, out _));
        Assert.True(panel.TryEdit(row.Right - 75, row.MidY, 0, true, out _, out value)); Assert.Equal(49.9, value);
        Assert.Equal(50, panel.GetRow(0).DisplayValue);
    }

    private sealed class ProbeConnection : IGameSessionConnection
    {
        private readonly Channel<AuthoritativeSnapshot> _snapshots = Channel.CreateUnbounded<AuthoritativeSnapshot>();
        internal readonly List<PlayerCommand> Commands = [];
        internal int SpeedChanges;
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) { Commands.Add(command); return ValueTask.CompletedTask; }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) { SpeedChanges++; return ValueTask.CompletedTask; }
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await foreach (var snapshot in _snapshots.Reader.ReadAllAsync(cancellationToken)) yield return snapshot; }
        public ValueTask DisposeAsync() { _snapshots.Writer.TryComplete(); return ValueTask.CompletedTask; }
    }
}
