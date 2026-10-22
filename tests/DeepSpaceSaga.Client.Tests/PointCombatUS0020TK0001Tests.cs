using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

public class PointCombatUS0020TK0001Tests
{
    private static ObjectMotionSnapshot Ship(double targetX = 5000) => new("player", 0, 0, 5, 0,
        ActiveEngineCommandType: ShipEngineCommandTypes.Orbit, TurnStepDegrees: 2,
        TurnStepRemainingMs: 100, TurnStepIntervalMs: 100,
        NavigationTargetX: targetX, NavigationTargetY: -1000, NavigationAngularInertiaDegPerSec: 20);

    [Fact]
    public void Maneuver_reaches_target_beyond_five_seconds()
    {
        List<FutureTrajectoryPoint> points = [];
        CombatTrajectoryProjector.BuildOwnViewportPath(Ship(), points, new(0, 0, .1), 1920, 1080);
        Assert.Contains(points, p => double.Hypot(p.X - 5000, p.Y + 1000) < 10);
        Assert.True(points.Count > 3);
        Assert.NotEqual(0, points[1].Y);
    }

    [Fact]
    public void Maneuver_extends_straight_outside_viewport()
    {
        List<FutureTrajectoryPoint> points = [];
        var camera = new CameraState(0, 0, .1);
        CombatTrajectoryProjector.BuildOwnViewportPath(Ship(), points, camera, 1920, 1080);
        var end = camera.WorldToScreen(points[^1].X, points[^1].Y, 1920, 1080);
        Assert.True(end.X >= 1919 || end.Y <= 1 || end.Y >= 1079 || end.X <= 1);
        Assert.True(points[^1].X > 5000);
    }

    [Fact]
    public void Maneuver_pause_zoom_pan_retarget()
    {
        List<FutureTrajectoryPoint> a = [], b = [];
        CombatTrajectoryProjector.BuildOwnViewportPath(Ship(), a, new(0, 0, .1), 1920, 1080);
        CombatTrajectoryProjector.BuildOwnViewportPath(Ship(), b, new(100, 200, .01), 1200, 800);
        Assert.Equal(a.Take(a.Count - 1), b.Take(b.Count - 1));
        CombatTrajectoryProjector.BuildOwnViewportPath(Ship(-5000), b, new(0, 0, .1), 1920, 1080);
        Assert.Contains(b, p => double.Hypot(p.X + 5000, p.Y + 1000) < 10);
        Assert.DoesNotContain(b, p => p.X >= 5000);
    }
}
