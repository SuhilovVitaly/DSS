using DeepSpaceSaga.Client.UI;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatTrajectoryTests
{
    private static readonly ObjectMotionSnapshot Player = new("player", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip);
    private static readonly ObjectMotionSnapshot Torpedo = new("torpedo", 100, 100, 6, 0, RenderObjectType: SpaceObjectType.Missile,
        OwnProjectile: new(ProjectileKind.Torpedo, "player", "launcher", 100, 100, 100, 100, 100, -200, 0, 5000, 6, "slow", 9, 200, 20));
    private static AuthoritativeSnapshot Snapshot => new(1, 9000000000, SimulationSpeed.Speed0, [Player, Torpedo], "player");
    [Fact]
    public void Zoom_changes_screen_geometry_not_flight_state()
    {
        string before = JsonSerializer.Serialize(Torpedo);
        var forecast = CombatForecast.PointProjectile(Torpedo, null, 0, new(5000));
        var geometry = CombatTrajectoryProjector.FromPointForecast(forecast, Torpedo, 0);
        var camera = new CameraState(0, 0, 1); using var path = new SKPath();
        CombatTrajectoryProjector.BuildPath(path, geometry.Prediction, camera, 1920, 1080); float x = path.LastPoint.X;
        camera.SetZoom(2); camera.SetFocus(10, 0);
        CombatTrajectoryProjector.BuildPath(path, geometry.Prediction, camera, 1920, 1080);
        Assert.NotEqual(x, path.LastPoint.X); Assert.Equal(1140, path.LastPoint.X);
        Assert.Equal(before, JsonSerializer.Serialize(Torpedo));
        CombatTrajectoryProjector.BuildPath(path, [new(-1e12, 0), new(1e12, 0)], camera, 1920, 1080);
        Assert.InRange(path.Bounds.Left, -2.1f, 0); Assert.InRange(path.Bounds.Right, 1920, 1922.1f);
    }
    [Fact]
    public void Current_pose_pause_and_explicit_removal_control_the_visible_path()
    {
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(Snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080); var path = screen.CombatTrajectories["torpedo"];
        Assert.Equal(new FutureTrajectoryPoint(100, 100), path.Prediction[0]);
        Assert.Equal(new FutureTrajectoryPoint(100, -200), path.Detonation);
        Assert.Null(path.Intercept); Assert.Empty(path.Target);
        screen.Render(canvas, 1920, 1080); Assert.Equal(new FutureTrajectoryPoint(100, 100), screen.CombatTrajectories["torpedo"].Prediction[0]);
        buffer.Update(Snapshot with { SnapshotSequence = 2, Objects = [Player] });
        screen.Render(canvas, 1920, 1080); Assert.Empty(screen.CombatTrajectories); Assert.Empty(screen.PointProjectileVisuals);
        screen.OnDeactivated();
    }
    [Theory]
    [InlineData(0, 0, 4)]
    [InlineData(90, -4, 0)]
    public void Torpedo_draws_ship_engine_flame_behind_its_current_heading(double heading, int offsetX, int offsetY)
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(Snapshot with { Objects = [Player, Torpedo with { Direction = heading, OwnProjectile = Torpedo.OwnProjectile }] });
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        bool observed = false;
        screen.RenderStageCompleted = stage =>
        {
            if (stage == "combat_trajectories") canvas.Clear(SKColors.Transparent);
            if (stage != "marker_geometry") return;
            observed = true;
            var behind = bitmap.GetPixel(1060 + offsetX, 640 + offsetY);
            var ahead = bitmap.GetPixel(1060 - offsetX, 640 - offsetY);
            Assert.True(behind.Blue > ahead.Blue && behind.Alpha > 0, $"Expected fading blue exhaust, got {behind}");
            Assert.True(behind.Alpha > ahead.Alpha, $"Exhaust must point behind the torpedo: {behind}, {ahead}");
        };
        screen.Render(canvas, 1920, 1080);
        Assert.True(observed);
        screen.OnDeactivated();
    }

}
