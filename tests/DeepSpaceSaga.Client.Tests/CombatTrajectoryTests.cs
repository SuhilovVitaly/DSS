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

}
