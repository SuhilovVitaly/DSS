using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapPipelineIntegrationTests
{
    [Fact]
    public async Task Input_and_capture_use_presented_frame()
    {
        var buffer = new SnapshotBuffer(() => 0);
        var player = new ObjectMotionSnapshot("player", 0, 0, 1, 90);
        var contact = new ObjectMotionSnapshot("contact", 0, -150, 0, 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [player, contact], PlayerShipObjectId: "player"));
        using var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        screen.Render(surface.Canvas, 1280, 720);
        long shown = screen.PresentedFrameId;
        var original = screen.PreparedScene!;
        TacticalMapSnapshotDocument? captured = null;
        screen.SnapshotWriter = (document, _) => { captured = document; return "test"; };
        buffer.Update(new(2, 1000, SimulationSpeed.Speed0, [player with { X = 200 }, contact with { X = 500 }], PlayerShipObjectId: "player"));
        screen.RenderStageCompleted = stage =>
        {
            if (stage != "marker_geometry") return;
            screen.RequestTacticalMapSnapshot();
            screen.OnMouseDown(640, 210);
            Assert.Equal(shown, screen.LastInputFrameId);
            Assert.Equal("contact", screen.SelectedObjectId);
        };
        screen.Render(surface.Canvas, 1280, 720);
        await screen.SnapshotSaveTask;
        Assert.NotNull(captured);
        Assert.Equal(shown, captured.FrameId);
        Assert.Equal(1UL, captured.State.AuthoritativeSnapshot!.SnapshotSequence);
        Assert.Equal(0, captured.State.Objects.Single(o => o.Authoritative.ObjectId == "player").Rendered.X);
        Assert.Equal(original.Frame.Reconciliation!.Sequence, captured.State.Reconciliation.LastSnapshotBaselineSequence);
        Assert.True(screen.PresentedFrameId > shown);
    }

    [Fact]
    public void Frozen_trail_history_survives_ring_growth_replacement_and_pruning()
    {
        var trail = new ObjectTrailBuffer();
        for (int i = 0; i < 16; i++) trail.Add(new(i, -i, i));
        var first = trail.Freeze();
        for (int i = 16; i < 600; i++) trail.Add(new(i, -i, i));
        var full = trail.Freeze();
        var saved = full.ToArray();
        trail[0] = new(1000, 1000, 1000);
        trail.RemoveFirst(10);
        trail.Add(new(2000, 2000, 2000));
        Assert.Equal(Enumerable.Range(0, 16).Select(i => (double)i), first.Select(p => p.X));
        Assert.Equal(saved, full.ToArray());
        Assert.Equal(512, full.Count);
        Assert.Equal(16, first.Capacity);
    }

    [Fact]
    public void Pipeline_handles_resize_modal_pause_and_disposal()
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0)], PlayerShipObjectId: "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));
        screen.Render(surface.Canvas, 1280, 720);
        var old = screen.PreparedScene!.PaintCommands!;
        screen.OnDeactivated(); screen.OnActivated(); screen.SetUiScale(1.5f);
        screen.Render(surface.Canvas, 1920, 1080);
        Assert.True(old.IsDisposed);
        Assert.Equal(1920, screen.PreparedScene!.View.Width);
        Assert.Equal(1.5f, screen.PreparedScene.View.UiScale);
        var current = screen.PreparedScene.PaintCommands!;
        screen.Dispose();
        Assert.True(current.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => screen.Render(surface.Canvas, 1920, 1080));
    }

    [Fact]
    public void Stage_instrumentation_has_bounded_overhead()
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0)], PlayerShipObjectId: "player"));
        using var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        screen.Render(surface.Canvas, 1280, 720);
        Assert.Null(screen.LastPipelineMetrics);
        Assert.Equal(0, screen.PipelineTimestampReads);
        screen.PipelineMetricsEnabled = true;
        screen.Render(surface.Canvas, 1280, 720);
        var metrics = screen.LastPipelineMetrics!.Value;
        Assert.Equal(1, metrics.PoseObjects);
        Assert.Equal(4, screen.PipelineTimestampReads);
        Assert.Equal(metrics, screen.CaptureFrameProfile().Frames[^1].Pipeline);
        screen.PipelineMetricsEnabled = false;
        screen.Render(surface.Canvas, 1280, 720);
        Assert.Null(screen.LastPipelineMetrics);
        Assert.Equal(4, screen.PipelineTimestampReads);
    }
}
