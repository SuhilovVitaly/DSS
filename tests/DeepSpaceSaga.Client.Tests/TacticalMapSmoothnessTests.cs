using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapSmoothnessTests
{
    private static AuthoritativeSnapshot DetailWorld(ulong sequence, ulong seed, string station, double x, long time = 0) =>
        new(sequence, time * 300, SimulationSpeed.Speed0,
            [new("player", x, 0, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip),
                new(station, x + 100, 0, 0, 0, RenderObjectType: SpaceObjectType.Station)], "player",
            SimulationTimeMs: time, SolarSystemMap: new(2, seed, 1000000, [], [], []),
            ClusterMap: new(2, "cluster-" + seed, [new("cluster-" + seed, "Cluster " + seed, "belt", [station], "mixed")],
                [new(station, "cluster-" + seed, "mining")], [], [], new(4, 1, 1, 3)));

    [Fact]
    public void DetailThresholdDoesNotFlicker()
    {
        var snapshot = DetailWorld(1, 42, "station", 0);
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        screen.Render(surface.Canvas, 1280, 720);
        void Span(double days)
        {
            double ppu = 720 / (days * 4 * 86400 / 300 * 10);
            var free = screen.AvailableMapRect();
            screen.OnMouseWheel(free.MidX, free.MidY, (float)(Math.Log(ppu / screen.CameraPixelsPerWorldUnit) / Math.Log(1.25)));
            screen.Render(surface.Canvas, 1280, 720);
        }
        for (int i = 0; i < 60; i++)
        {
            buffer.Update(snapshot with { SnapshotSequence = (ulong)i + 2, CurrentSpeed = i % 2 == 0 ? SimulationSpeed.Speed0 : SimulationSpeed.Speed4 });
            Span(4d / 24 * (i % 2 == 0 ? 1.02 : 1.08));
            Assert.Equal(MapDetailLevel.Local, screen.MapDetail);
        }
        Span(4d / 24 * 1.12); Assert.Equal(MapDetailLevel.Region, screen.MapDetail);
        for (int i = 0; i < 60; i++)
        {
            Span(4d / 24 * (i % 2 == 0 ? 1.02 : 1.08));
            Assert.Equal(MapDetailLevel.Region, screen.MapDetail);
            Assert.InRange(screen.MapClusterCount, 0, 1);
        }
    }

    [Fact]
    public void NewSessionClearsOldClusterPresentation()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock); buffer.Update(DetailWorld(1, 1, "old", 1000));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock,
            mapSettings: new() { ZoomAnimationMs = 120 });
        using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));
        screen.Render(surface.Canvas, 1920, 1080);
        var overview = screen.ScaleButtonRects[4]; screen.OnMouseDown(overview.MidX, overview.MidY);
        clock += Stopwatch.Frequency; screen.Render(surface.Canvas, 1920, 1080);
        Assert.Equal(MapDetailLevel.System, screen.MapDetail); Assert.Equal(1, screen.MapClusterCount);
        var free = screen.AvailableMapRect(); screen.OnMouseWheel(free.MidX, free.MidY, 1);
        Assert.True(screen.IsZoomAnimating);
        buffer.Update(DetailWorld(2, 2, "new", 5000));
        screen.Render(surface.Canvas, 1920, 1080);
        Assert.False(screen.IsZoomAnimating); Assert.Equal(MapDetailLevel.Local, screen.MapDetail);
        Assert.Equal(screen.LocalMaximumPpu, screen.CameraPixelsPerWorldUnit);
        Assert.Equal(5000, screen.CameraFocusX);
        Assert.Equal(5000, screen.RenderStates.Single(s => s.Pose.ObjectId == "player").Pose.X);
        Assert.DoesNotContain("old", screen.MapLabels.Keys); Assert.DoesNotContain("old", screen.AggregatedMapObjectIds);
        Assert.Empty(screen.MapSummaryLabels);
        free = screen.AvailableMapRect(); screen.OnMouseWheel(free.MidX, free.MidY, -1);
        clock += Stopwatch.Frequency; screen.Render(surface.Canvas, 1920, 1080);
        double manual = screen.CameraPixelsPerWorldUnit;
        buffer.Update(DetailWorld(3, 2, "new", 5000, 100)); screen.Render(surface.Canvas, 1920, 1080);
        Assert.Equal(manual, screen.CameraPixelsPerWorldUnit);
        buffer.Update(DetailWorld(4, 2, "new", 5000)); screen.Render(surface.Canvas, 1920, 1080);
        Assert.Equal(screen.LocalMaximumPpu, screen.CameraPixelsPerWorldUnit);
    }

    [Fact]
    public void ResizeKeepsSelectionAndHitsAligned()
    {
        var buffer = new SnapshotBuffer(); buffer.Update(DetailWorld(1, 42, "station", 1000) with { SelectedObjectId = "station" });
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), mapSettings: new() { ZoomAnimationMs = 120 });
        using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));
        screen.Render(surface.Canvas, 1920, 1080);
        var free = screen.AvailableMapRect(); screen.OnMouseWheel(free.MidX, free.MidY, -1);
        Assert.True(screen.IsZoomAnimating);
        screen.SetUiScale(1.5f); screen.Render(surface.Canvas, 1280, 720);
        Assert.False(screen.IsZoomAnimating); Assert.Equal("station", screen.SelectedObjectId);
        var label = screen.MapLabels["station"].PlaqueRect;
        Assert.InRange(label.Right, 0, 1280); Assert.InRange(label.Bottom, 0, 720);
        screen.OnMouseMove(label.MidX, label.MidY); Assert.Equal("station", screen.ActiveObjectId);
        screen.OnMouseDown(label.MidX, label.MidY, Silk.NET.Input.MouseButton.Right);
        screen.OnMouseDown(label.MidX, label.MidY, Silk.NET.Input.MouseButton.Left);
        Assert.Equal("station", screen.SelectedObjectId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Snapshot_during_reconciliation_preserves_the_in_progress_visual_pose(bool baselineIncludesCorrection)
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        var controlBuffer = new SnapshotBuffer(() => clock);
        var predictor = new LinearMotionPredictor();
        var obj = new ObjectMotionSnapshot("player", 0, 0, 1, 90);
        var initial = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed4, [obj], PlayerShipObjectId: "player");
        buffer.Update(initial);
        controlBuffer.Update(initial);
        var screen = new GameSessionScreen(buffer, predictor, timestampProvider: () => clock);
        var control = new GameSessionScreen(controlBuffer, predictor, timestampProvider: () => clock);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        void Frame()
        {
            clock += Stopwatch.Frequency / 80;
            screen.Render(surface.Canvas, 1280, 720);
            control.Render(surface.Canvas, 1280, 720);
        }
        for (int i = 0; i < 80; i++) Frame();
        long aheadTime = buffer.EffectivePredictionDeltaMs + 6000;
        var ahead = new AuthoritativeSnapshot(2, aheadTime, SimulationSpeed.Speed4,
            [predictor.Predict(obj, aheadTime)], PlayerShipObjectId: "player");
        buffer.Update(ahead);
        controlBuffer.Update(ahead);
        Frame(); // Both begin smoothing a 60-world-unit offset.
        for (int i = 0; i < 8; i++) Frame();

        long time = ahead.MotionTimeMs + buffer.EffectivePredictionDeltaMs;
        double progress = (9.0 / 80) / .3; // Correction age on the next rendered frame.
        double remainingOffset = 60 * (1 - progress * progress * (3 - 2 * progress));
        if (!baselineIncludesCorrection) time += 4000;
        var next = predictor.Predict(obj, time);
        if (baselineIncludesCorrection) next = next with { X = next.X - remainingOffset };
        buffer.Update(new(3, time, SimulationSpeed.Speed4, [next], PlayerShipObjectId: "player"));
        Frame();
        // Quarter pace can quantize the two baselines one physical millisecond apart.
        Assert.InRange(Math.Abs(control.RenderStates[0].Predicted.X - screen.RenderStates[0].Predicted.X), 0, .010001);
        for (int i = 0; i < 40; i++) Frame();
        Assert.Equal(predictor.Predict(next, buffer.EffectivePredictionDeltaMs).X,
            screen.RenderStates[0].Predicted.X, 6);
    }

    [Fact]
    public void Speed100_prediction_preserves_submillisecond_real_time()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        buffer.Update(new(1, 0, SimulationSpeed.Speed4, []));
        clock = Stopwatch.Frequency / 80; // 12.5 real ms at 80 Hz
        Assert.Equal(312, buffer.EffectivePredictionDeltaMs);
        clock = Stopwatch.Frequency * 3 / 80;
        Assert.Equal(937, buffer.EffectivePredictionDeltaMs);
    }

    [Theory]
    [InlineData(200, false)]
    [InlineData(1500, false)]
    [InlineData(6000, false)]
    [InlineData(6000, true)]
    public void All_500_contacts_keep_continuous_motion_when_snapshots_advance_time(int skewMotionMs, bool skipSnapshotFrame)
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        var predictor = new LinearMotionPredictor();
        var objects = Enumerable.Range(0, 500)
            .Select(i => new ObjectMotionSnapshot("object-" + i, i * 100, i * 50, 1 + i % 20, 90))
            .ToImmutableArray();
        buffer.Update(new(1, 0, SimulationSpeed.Speed4, objects, PlayerShipObjectId: objects[0].ObjectId));
        var screen = new GameSessionScreen(buffer, predictor, timestampProvider: () => clock);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        void Frame()
        {
            clock += Stopwatch.Frequency / 80;
            screen.Render(surface.Canvas, 1280, 720);
        }
        for (int i = 0; i < 80; i++) Frame();
        var before = screen.RenderStates.Select(s => s.Predicted.X).ToArray();
        long previousTime = buffer.LatestPrediction!.BufferedSnapshot.Snapshot.MotionTimeMs + buffer.EffectivePredictionDeltaMs;
        void Publish(ulong sequence, long time) => buffer.Update(new(sequence, time, SimulationSpeed.Speed4,
            objects.Select(o => predictor.Predict(o, time)).ToImmutableArray(), PlayerShipObjectId: objects[0].ObjectId));
        if (skipSnapshotFrame) Publish(2, previousTime + skewMotionMs / 2);
        Publish(3, previousTime + skewMotionMs);
        Frame();
        for (int i = 0; i < objects.Length; i++)
        {
            double normalStep = objects[i].SpeedKmS * 10 * .3125;
            double actualStep = screen.RenderStates[i].Predicted.X - before[i];
            // A physical timestamp has integer-millisecond precision.
            double quantization = objects[i].SpeedKmS * .01;
            Assert.InRange(actualStep, normalStep - quantization, normalStep + quantization);
        }
        for (int i = 0; i < 40; i++) Frame();
        var prediction = buffer.LatestPrediction!;
        for (int i = 0; i < objects.Length; i++)
        {
            var expected = predictor.Predict(prediction.BufferedSnapshot.Snapshot.Objects[i], prediction.EffectivePredictionDeltaMs);
            Assert.Equal(expected.X, screen.RenderStates[i].Predicted.X, 6);
        }
    }
}
