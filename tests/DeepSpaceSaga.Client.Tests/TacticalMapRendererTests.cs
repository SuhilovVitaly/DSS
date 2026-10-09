using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapRendererTests
{
    private sealed class CountingPredictor : IMotionPredictor
    {
        internal int Calls;
        private readonly LinearMotionPredictor _inner = new();
        public ObjectMotionSnapshot Predict(ObjectMotionSnapshot state, long elapsedMs)
        {
            Calls++;
            return _inner.Predict(state, elapsedMs);
        }
    }

    private static GameSessionScreen Create(IMotionPredictor predictor)
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed1, [new("player", 0, 0, 1, 90)], PlayerShipObjectId: "player"));
        return new(buffer, predictor, timestampProvider: () => 0);
    }

    [Fact]
    public void Drawing_same_scene_does_not_mutate_state()
    {
        using var screen = Create(new LinearMotionPredictor());
        using var first = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(first);
        screen.Render(canvas, 1280, 720);
        var scene = screen.PreparedScene!;
        var markers = scene.Markers.ToArray();
        using var painter = new TacticalMapRenderer();
        using var second = new SKBitmap(1280, 720);
        using var replay = new SKCanvas(second);
        painter.Draw(replay, scene, 0);
        Assert.Equal(first.Pixels, second.Pixels);
        replay.Clear();
        painter.Draw(replay, scene, 0);
        Assert.Equal(first.Pixels, second.Pixels);
        Assert.Equal(markers, scene.Markers.ToArray());
        var status = scene.View.Labels[0].Geometry.StatusRect;
        replay.Clear();
        painter.Draw(replay, scene, .75);
        Assert.NotEqual(first.GetPixel((int)status.MidX, (int)status.MidY), second.GetPixel((int)status.MidX, (int)status.MidY));
        Assert.Same(scene, screen.PreparedScene);
    }

    [Fact]
    public void Draw_does_not_run_geometry_or_io()
    {
        var predictor = new CountingPredictor();
        using var screen = Create(predictor);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        screen.Render(surface.Canvas, 1280, 720);
        Assert.True(predictor.Calls > 0);
        var before = (predictor.Calls, screen.LabelGeometryBuilds, screen.FutureGeometryBuilds,
            screen.NavigationGeometryBuilds, screen.TrailGeometryBuilds, screen.ContactMembershipBuilds);
        var writer = screen.SnapshotSaveTask;
        int writes = 0;
        screen.SnapshotWriter = (_, _) => { writes++; return "unexpected"; };
        using var painter = new TacticalMapRenderer();
        for (int i = 0; i < 10; i++) painter.Draw(surface.Canvas, screen.PreparedScene!, i / 80.0);
        Assert.Equal(before, (predictor.Calls, screen.LabelGeometryBuilds, screen.FutureGeometryBuilds,
            screen.NavigationGeometryBuilds, screen.TrailGeometryBuilds, screen.ContactMembershipBuilds));
        Assert.Same(writer, screen.SnapshotSaveTask);
        Assert.Equal(0, writes);
    }

    [Fact]
    public void Painter_preserves_layer_order_and_clipping()
    {
        using var screen = Create(new LinearMotionPredictor());
        using var bitmap = new SKBitmap(200, 200);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 200, 200);
        using var recorder = new TacticalMapPaintRecorder(100, 100);
        using var paint = new SKPaint { Color = SKColors.Red };
        recorder.Canvas.DrawRect(0, 0, 200, 200, paint);
        var recording = recorder.Stage("background");
        recording.Save();
        recording.Scale(2);
        paint.Color = SKColors.Blue;
        recording.DrawRect(10, 10, 20, 20, paint);
        recording = recorder.Stage("ui");
        paint.Color = SKColors.Lime;
        recording.DrawRect(20, 20, 10, 10, paint);
        recording.Restore();
        using var commands = recorder.Finish();
        var scene = screen.PreparedScene! with { View = screen.PreparedScene!.View with { Width = 100, Height = 100 }, PaintCommands = commands };
        canvas.Clear(SKColors.Black);
        var order = new List<string>();
        using var painter = new TacticalMapRenderer();
        painter.Draw(canvas, scene, 0, order.Add);
        Assert.Equal(new[] { "background", "ui" }, order);
        Assert.Equal(SKColors.Red, bitmap.GetPixel(5, 5));
        Assert.Equal(SKColors.Blue, bitmap.GetPixel(25, 25));
        Assert.Equal(SKColors.Lime, bitmap.GetPixel(45, 45));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(150, 150));
        var handle = commands.Commands[0].Picture!;
        commands.Dispose();
        Assert.Equal(IntPtr.Zero, handle.Handle);
        Assert.Throws<ObjectDisposedException>(() => painter.Draw(canvas, scene, 0));
    }
}
