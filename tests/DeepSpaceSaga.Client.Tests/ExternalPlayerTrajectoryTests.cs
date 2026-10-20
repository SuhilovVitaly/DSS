using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class ExternalPlayerTrajectoryTests
{
    [Theory]
    [InlineData(0, 0, -1)]
    [InlineData(90, 1, 0)]
    [InlineData(180, 0, 1)]
    [InlineData(270, -1, 0)]
    public void External_line_uses_current_course_while_own_turn_forecast_remains_curved(double heading, int dx, int dy)
    {
        var player = new ObjectMotionSnapshot("player", 100, 50, 1, heading,
            TurnStepDegrees: 10, TurnStepRemainingMs: 500, TurnStepIntervalMs: 1000,
            RenderObjectType: SpaceObjectType.PlayerShip);
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [player], "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        Assert.Equal(2, screen.ExternalPlayerTrajectory.Count);
        var start = screen.ExternalPlayerTrajectory[0];
        var end = screen.ExternalPlayerTrajectory[1];
        Assert.Equal(new FutureTrajectoryPoint(100, 50), start);
        double distance = (dx == 0 ? 360 : 640) / screen.CameraPixelsPerWorldUnit;
        Assert.Equal(start.X + dx * distance, end.X, 6);
        Assert.Equal(start.Y + dy * distance, end.Y, 6);
        Assert.NotNull(screen.DisplayedPlayerTrajectoryEnd);
        Assert.NotEqual(end, screen.DisplayedPlayerTrajectoryEnd.Value);
        Assert.Equal(player, Assert.Single(buffer.Latest!.Snapshot.Objects));
        screen.OnDeactivated();
    }

    [Fact]
    public void Line_is_removed_for_stationary_destroyed_or_missing_player()
    {
        var player = new ObjectMotionSnapshot("player", 0, 0, 1, 90, RenderObjectType: SpaceObjectType.PlayerShip);
        var buffer = new SnapshotBuffer(() => 0);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [player], "player"));
        screen.Render(canvas, 1280, 720);
        Assert.Equal(2, screen.ExternalPlayerTrajectory.Count);
        foreach (var state in new[] { player with { SpeedKmS = 0 }, player with { IsDestroyed = true } })
        {
            buffer.Update(new(2, 0, SimulationSpeed.Speed0, [state], "player"));
            screen.Render(canvas, 1280, 720);
            Assert.Empty(screen.ExternalPlayerTrajectory);
        }
        buffer.Update(new(3, 0, SimulationSpeed.Speed0, [], "player"));
        screen.Render(canvas, 1280, 720);
        Assert.Empty(screen.ExternalPlayerTrajectory);
        screen.OnDeactivated();
    }

    [Fact]
    public void Forecast_toggle_hides_external_line()
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            [new("player", 0, 0, 1, 90, RenderObjectType: SpaceObjectType.PlayerShip)], "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), showTrajectoryPrediction: false);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        Assert.Empty(screen.ExternalPlayerTrajectory);
        screen.OnDeactivated();
    }

    [Fact]
    public void External_line_is_dashed_and_dimmer_than_the_main_forecast()
    {
        using var bitmap = new SKBitmap(200, 80);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        new TacticalMapDepthRenderer().DrawTargetTrajectory(canvas,
            [new(-80, 0), new(80, 0)], new CameraState(0, 0, 1), 200, 80,
            SpaceMapColorResolver.PlayerShipColor, opacity: 77);
        var pixels = Enumerable.Range(30, 140).Select(x => bitmap.GetPixel(x, 40)).ToArray();
        Assert.Contains(pixels, p => p.Alpha > 0 && p.Blue > p.Green && p.Green > p.Red);
        Assert.Contains(pixels, p => p.Alpha == 0);
        Assert.All(pixels, p => Assert.InRange(p.Alpha, 0, 77));
    }
}
