using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public sealed class TacticalMapSnapshotTests
{
    [Fact]
    public async Task Snapshot_button_writes_authoritative_and_render_state_with_camera_selection_and_speed()
    {
        string directory = Path.Combine(Path.GetTempPath(), "DSS-TacticalMapSnapshotTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            long clock = 0;
            var buffer = new SnapshotBuffer(() => clock);
            buffer.Update(new AuthoritativeSnapshot(
                17,
                12_345,
                SimulationSpeed.Speed2,
                ImmutableArray.Create(
                    new ObjectMotionSnapshot("PLAYER", 10_000, 10_000, 1.5, 90,
                        ActiveEngineCommandType: ShipEngineCommandTypes.Accelerate,
                        RenderObjectType: SpaceObjectType.PlayerShip),
                    new ObjectMotionSnapshot("TARGET", 10_050, 10_000, .5, 270,
                        RenderObjectType: SpaceObjectType.Station)),
                "PLAYER"));

            var screen = new GameSessionScreen(
                buffer,
                new LinearMotionPredictor(),
                timestampProvider: () => clock,
                tacticalMapSnapshotDirectory: directory);

            using var bitmap = new SKBitmap(1920, 1080);
            using var canvas = new SKCanvas(bitmap);
            clock += Stopwatch.Frequency * 50 / 1000;
            screen.Render(canvas, 1920, 1080);

            // Select the target first so the capture proves that the click-state is
            // written independently from the authoritative snapshot's object list.
            screen.OnMouseDown(1010, 540);
            screen.OnMouseDown(screen.MapViewButtonRects[4].MidX, screen.MapViewButtonRects[4].MidY);

            screen.Render(canvas, 1920, 1080);
            string? path = await screen.SnapshotSaveTask;
            Assert.NotNull(path);
            using var document = JsonDocument.Parse(File.ReadAllText(path!));
            JsonElement state = document.RootElement.GetProperty("state");

            Assert.Equal(2, document.RootElement.GetProperty("schemaVersion").GetInt32());
            Assert.Equal(17UL, state.GetProperty("authoritativeSnapshot").GetProperty("snapshotSequence").GetUInt64());
            Assert.Equal("PLAYER", state.GetProperty("playerShipObjectId").GetString());
            Assert.Equal("TARGET", state.GetProperty("selectedObjectId").GetString());
            Assert.Equal("Speed2", state.GetProperty("clientPredictionSpeed").GetString());
            Assert.Equal(2, state.GetProperty("objects").GetArrayLength());
            Assert.Contains(state.GetProperty("objects").EnumerateArray(), item =>
                item.GetProperty("authoritative").GetProperty("objectId").GetString() == "TARGET" &&
                item.GetProperty("isSelected").GetBoolean());
            Assert.True(state.GetProperty("camera").GetProperty("pixelsPerWorldUnit").GetDouble() > 0);
            Assert.Equal(1920, state.GetProperty("viewport").GetProperty("width").GetInt32());
            Assert.Equal(1080, state.GetProperty("viewport").GetProperty("height").GetInt32());
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Writer_does_not_overwrite_two_captures_with_the_same_timestamp_and_sequence()
    {
        string directory = Path.Combine(Path.GetTempPath(), "DSS-TacticalMapSnapshotTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var document = new TacticalMapSnapshotDocument(
                TacticalMapSnapshotWriter.CurrentSchemaVersion,
                new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero),
                new TacticalMapSnapshotState(
                    new AuthoritativeSnapshot(4, 0, SimulationSpeed.Speed0, ImmutableArray<ObjectMotionSnapshot>.Empty),
                    0,
                    null,
                    0,
                    0,
                    SimulationSpeed.Speed0,
                    0,
                    0,
                    null,
                    null,
                    null,
                    null,
                    new(0, 0, 1, true, false, 1),
                    new(0, 0, 1, 1, 1, 1, 1, false),
                    new(true, false, false, 0, 0),
                    new(),
                    [],
                    [],
                    [],
                    new(false, 0, 0, 0, SimulationSpeed.Speed0, [], [], [])));

            string first = TacticalMapSnapshotWriter.Write(document, directory);
            string second = TacticalMapSnapshotWriter.Write(document, directory);

            Assert.NotEqual(first, second);
            Assert.True(File.Exists(first));
            Assert.True(File.Exists(second));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Capture_keeps_one_rendered_frame_when_snapshot_arrives_during_render(bool showForecast)
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        var ship = new ObjectMotionSnapshot("player", 10000, 10000, 1, 90);
        buffer.Update(new(17, 0, SimulationSpeed.Speed1, [ship], PlayerShipObjectId: "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock,
            showTrajectoryPrediction: showForecast);
        TacticalMapSnapshotDocument? saved = null;
        screen.SnapshotWriter = (document, _) => { saved = document; return "test-capture"; };
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        screen.Render(surface.Canvas, 1280, 720);
        var button = screen.MapViewButtonRects[4];
        screen.OnMouseDown(button.MidX, button.MidY);
        clock = Stopwatch.Frequency / 10;
        screen.RenderStageCompleted = stage =>
        {
            if (stage != "coordinates_and_hit_test") return;
            clock += Stopwatch.Frequency / 10;
            buffer.Update(new(18, 5000, SimulationSpeed.Speed1, [ship with { X = 12000 }], PlayerShipObjectId: "player"));
        };
        screen.Render(surface.Canvas, 1280, 720);
        await screen.SnapshotSaveTask;

        Assert.NotNull(saved);
        Assert.Equal(18UL, buffer.Latest!.Snapshot.SnapshotSequence);
        Assert.Equal(17UL, saved.State.AuthoritativeSnapshot!.SnapshotSequence);
        Assert.Equal(100L, saved.State.EffectivePredictionDeltaMs);
        Assert.Equal(Stopwatch.Frequency / 10, saved.State.CaptureTimestampTicks);
        Assert.Equal(10001, Assert.Single(saved.State.Objects).Rendered.X);
        var frame = saved.Profile!.Frames[^1];
        Assert.Equal(saved.FrameId, frame.FrameId);
        Assert.Equal(saved.State.PredictedGameTimeMs, frame.PredictedGameTimeMs);
        Assert.Equal(saved.State.Camera.FocusX, frame.Player!.Value.RenderedX);
        Assert.Equal(showForecast, saved.ShowTrajectoryPrediction);
        if (showForecast)
        {
            var trajectory = Assert.Single(saved.State.Trajectories);
            Assert.Equal(screen.DisplayedPlayerTrajectoryEnd!.Value.X, trajectory.Points[^1].X);
            Assert.Equal(screen.DisplayedPlayerTrajectoryEnd.Value.Y, trajectory.Points[^1].Y);
        }
        else Assert.Empty(saved.State.Trajectories);
    }

    [Fact]
    public async Task Blocked_writer_does_not_block_render_or_queue_repeated_captures()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        buffer.Update(new(1, 0, SimulationSpeed.Speed1,
            [new("player", 10000, 10000, 1, 90)], PlayerShipObjectId: "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock);
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        TacticalMapSnapshotDocument? saved = null;
        int calls = 0;
        screen.SnapshotWriter = (document, _) =>
        {
            Interlocked.Increment(ref calls);
            entered.SetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Test writer was not released.");
            saved = document;
            return "test-capture";
        };
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        screen.Render(surface.Canvas, 1280, 720);
        var button = screen.MapViewButtonRects[4];
        screen.OnMouseDown(button.MidX, button.MidY);
        screen.Render(surface.Canvas, 1280, 720);
        var pending = screen.SnapshotSaveTask;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            for (int i = 0; i < 3; i++)
            {
                clock += Stopwatch.Frequency / 10;
                screen.OnMouseDown(button.MidX, button.MidY);
                screen.Render(surface.Canvas, 1280, 720);
            }
            Assert.False(pending.IsCompleted);
            Assert.Same(pending, screen.SnapshotSaveTask);
            Assert.Equal(1, Volatile.Read(ref calls));
            Assert.Equal(10003, screen.RenderStates[0].Predicted.X);
        }
        finally
        {
            release.Set();
            await pending.WaitAsync(TimeSpan.FromSeconds(5));
        }
        Assert.NotNull(saved);
        // Subsequent renders did not mutate the worker's owned frame or history.
        Assert.Equal(10000, saved.State.Objects[0].Rendered.X);
        Assert.Equal(2, saved.Profile!.Frames.Length);
        Assert.Equal(10000, saved.Profile.Frames[^1].Player!.Value.RenderedX);
    }

    [Fact]
    public async Task Profile_preserves_unclamped_frame_gap_window_timing_and_snapshot_transition()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        var ship = new ObjectMotionSnapshot("player", 10000, 10000, 1, 90);
        buffer.Update(new(1, 0, SimulationSpeed.Speed1, [ship], PlayerShipObjectId: "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock);
        TacticalMapSnapshotDocument? saved = null;
        screen.SnapshotWriter = (document, _) => { saved = document; return "test-capture"; };
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        screen.Render(surface.Canvas, 1280, 720);
        screen.CompleteWindowProfile(new(16, 3, 2, 6, true, true, 1, 2560, 1440, 125, "test GPU", "test GL", 20, 1024, 4096));
        clock = Stopwatch.Frequency * 8 / 10;
        buffer.Update(new(2, 900, SimulationSpeed.Speed1, [ship with { X = 10009 }], PlayerShipObjectId: "player"));
        var button = screen.MapViewButtonRects[4];
        screen.OnMouseDown(button.MidX, button.MidY);
        screen.Render(surface.Canvas, 1280, 720);
        await screen.SnapshotSaveTask;

        Assert.NotNull(saved);
        var frames = saved.Profile!.Frames;
        Assert.Equal(2, frames.Length);
        Assert.Equal(800, frames[1].FrameIntervalMs);
        Assert.Equal(2, frames[0].Window!.Value.CpuFlushMs);
        Assert.Equal(125, frames[0].Window!.Value.PresentWaitMs);
        Assert.Equal("test GPU", frames[0].Window!.Value.GraphicsRenderer);
        Assert.Equal("test GL", frames[0].Window!.Value.GraphicsVersion);
        Assert.Equal(1024, frames[0].Window!.Value.GpuCacheBytes);
        Assert.Equal(4096, frames[0].Window!.Value.GpuCacheLimitBytes);
        Assert.Null(frames[1].Window); // Current callback has not completed yet.
        Assert.True(frames[1].NewSnapshot);
        Assert.Equal(100L, frames[1].ForwardJumpMs);
        Assert.Equal(100L, frames[1].TotalForwardJumpMs);
        Assert.True(frames[1].CaptureRequested);
        Assert.Equal(10008, frames[1].Player!.Value.RenderedX);
        Assert.Equal(10009, frames[1].Player!.Value.RawPredictedX);
        Assert.True(frames[1].RenderAllocatedBytes >= 0);
        Assert.True(frames[1].Stages.CoordinatesAndHitTestMs >= 0);
        Assert.Equal(800, saved.Profile.WithSummary().Summary!.MaximumFrameIntervalMs);
    }

    [Fact]
    public void Profile_ring_bounds_memory_and_time_and_preserves_chronological_order()
    {
        var ring = new TacticalMapFrameRecorder();
        for (int i = 1; i <= 5000; i++)
            ring.Add(default(TacticalMapFrameProfile) with { FrameId = i, TimestampTicks = i * Stopwatch.Frequency / 1000 });
        var first = ring.Capture();
        Assert.Equal(TacticalMapFrameRecorder.Capacity, first.Frames.Length);
        Assert.Equal(905, first.Frames[0].FrameId);
        Assert.Equal(5000, first.Frames[^1].FrameId);
        ring.Add(default(TacticalMapFrameProfile) with { FrameId = 5001, TimestampTicks = 35 * Stopwatch.Frequency });
        var second = ring.Capture();
        Assert.Equal(2, second.Frames.Length);
        Assert.Equal(5000, second.Frames[0].FrameId);
        Assert.Equal(5001, second.Frames[1].FrameId);
        Assert.Equal(5000, first.Frames[^1].FrameId);
        Assert.Equal(first.SessionId, second.SessionId);
    }
}
