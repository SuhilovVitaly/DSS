using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class OrbitalClientPredictionTests
{
    private static AuthoritativeSnapshot Snapshot(ulong sequence, long time, SimulationSpeed speed)
    {
        var orbit = new OrbitalElements(720000, 720000, 1200000000, 17, 0.000001, 0, "clockwise");
        var station = OrbitalMotionMath.At(new("station", 0, 0, 0, 0, RenderObjectType: "Station"), orbit, time);
        var ship = OrbitalMotionMath.At(new("ship", 0, 0, 0, 0, RenderObjectType: "PlayerShip",
            IsDocked: true, DockedStationObjectId: "station", WorldOffsetX: 1, WorldOffsetY: 1), orbit, time);
        return new(sequence, time * 300, speed, [station, ship], "ship", SimulationTimeMs: time);
    }

    [Theory]
    [InlineData(SimulationSpeed.Speed0)]
    [InlineData(SimulationSpeed.Speed1)]
    [InlineData(SimulationSpeed.Speed2)]
    [InlineData(SimulationSpeed.Speed3)]
    [InlineData(SimulationSpeed.Speed4)]
    public void ClientAndEngineOrbitAgree(SimulationSpeed speed)
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        buffer.Update(Snapshot(1, 10000, speed));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        clock += Stopwatch.Frequency / 5;
        screen.Render(canvas, 1280, 720);
        AssertPose(screen, buffer);
        var ship = screen.RenderStates.Single(o => o.Pose.ObjectId == "ship").Predicted;
        var station = screen.RenderStates.Single(o => o.Pose.ObjectId == "station").Predicted;
        Assert.Equal(station.X + 1, ship.X); Assert.Equal(station.Y + 1, ship.Y);
        Assert.Equal(station.OrbitSampleSimulationTimeMs, ship.OrbitSampleSimulationTimeMs);
    }

    [Fact]
    public void NewGameOrbitDoesNotBootstrapBeforeEpoch()
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(Snapshot(1, 0, SimulationSpeed.Speed0));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        AssertPose(screen, buffer);
    }

    [Fact]
    public void PauseResumeNoDrift()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        buffer.Update(Snapshot(1, 10000, SimulationSpeed.Speed4));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        clock += Stopwatch.Frequency / 10;
        buffer.CurrentSpeed = SimulationSpeed.Speed0;
        buffer.Update(Snapshot(2, 20000, SimulationSpeed.Speed0));
        screen.Render(canvas, 1280, 720);
        AssertPose(screen, buffer);
        var paused = screen.RenderStates.Select(s => s.Predicted).ToArray();
        clock += Stopwatch.Frequency;
        screen.Render(canvas, 1280, 720);
        Assert.Equal(paused, screen.RenderStates.Select(s => s.Predicted).ToArray());
        buffer.CurrentSpeed = SimulationSpeed.Speed1;
        screen.Render(canvas, 1280, 720);
        AssertPose(screen, buffer);
        clock += Stopwatch.Frequency / 4;
        buffer.Update(Snapshot(3, 20250, SimulationSpeed.Speed1));
        screen.Render(canvas, 1280, 720);
        AssertPose(screen, buffer);
    }

    private static void AssertPose(GameSessionScreen screen, SnapshotBuffer buffer)
    {
        var prediction = buffer.LatestPrediction!;
        foreach (var actual in screen.RenderStates)
        {
            var expected = OrbitalMotionMath.At(actual.Source, actual.Source.Orbit!,
                prediction.BufferedSnapshot.Snapshot.MotionTimeMs + prediction.EffectivePredictionDeltaMs);
            Assert.Equal(expected.X, actual.Pose.X, 7); Assert.Equal(expected.Y, actual.Pose.Y, 7);
            Assert.Equal(expected.SpeedKmS, actual.Pose.SpeedKmS, 9);
            Assert.Equal(expected.Direction, actual.Pose.Direction, 9);
            Assert.Equal(expected.OrbitSampleSimulationTimeMs, actual.Predicted.OrbitSampleSimulationTimeMs);
        }
    }
}
