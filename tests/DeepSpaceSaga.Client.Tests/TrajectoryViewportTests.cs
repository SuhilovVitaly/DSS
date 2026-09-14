using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using DeepSpaceSaga.Client;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public class TrajectoryViewportTests
{
    private const int Width = 1920, Height = 1080;

    [Theory]
    [InlineData(1)] [InlineData(.1)] [InlineData(.001)]
    [InlineData(.0001)] [InlineData(.00001)] [InlineData(1e-12)]
    public void Slow_straight_player_prediction_reaches_edge_at_every_scale_with_two_points(double ppu)
    {
        var player = new ObjectMotionSnapshot("player", 10000, 10000, .001, 90);
        var camera = new CameraState(player.X, player.Y, ppu);
        List<FutureTrajectoryPoint> points = new();
        new FutureTrajectoryProjector(new LinearMotionPredictor()).ProjectPlayerInto(player, points, camera, Width, Height);
        Assert.Equal(2, points.Count);
        var end = camera.WorldToScreen(points[^1].X, points[^1].Y, Width, Height);
        Assert.Equal(Width, end.X, 3); Assert.Equal(Height / 2f, end.Y, 3);
        Assert.Equal(new FutureTrajectoryPoint(player.X, player.Y), points[0]);
    }

    [Theory]
    [InlineData(0)] [InlineData(45)] [InlineData(90)] [InlineData(135)]
    [InlineData(180)] [InlineData(225)] [InlineData(270)] [InlineData(315)]
    public void Ray_hits_actual_viewport_edge_after_pan_in_every_direction(double heading)
    {
        var camera = new CameraState(-8000, 3000, .01);
        var player = new ObjectMotionSnapshot("player", 500, -600, 2, heading);
        List<FutureTrajectoryPoint> points = new();
        new FutureTrajectoryProjector(new LinearMotionPredictor()).ProjectPlayerInto(player, points, camera, Width, Height);
        AssertEdge(points[^1], camera);
        var a = points[0]; var b = points[^1]; double angle = heading * Math.PI / 180;
        Assert.True((b.X - a.X) * Math.Sin(angle) - (b.Y - a.Y) * Math.Cos(angle) > 0);
    }

    [Fact]
    public void Offscreen_ship_aimed_into_viewport_continues_to_opposite_edge()
    {
        var camera = new CameraState(0, 0, 1);
        List<FutureTrajectoryPoint> points = [new(-2000, 0)];
        TrajectoryViewportGeometry.ExtendToEdge(points, 90, camera, Width, Height);
        Assert.Equal(960, points[^1].X, 7);
        AssertEdge(points[^1], camera);
    }

    [Fact]
    public void Offscreen_ship_aimed_away_does_not_create_a_backward_line()
    {
        var camera = new CameraState(0, 0, 1);
        List<FutureTrajectoryPoint> points = [new(-2000, 0)];
        TrajectoryViewportGeometry.ExtendToEdge(points, 270, camera, Width, Height);
        Assert.Single(points);
    }

    [Theory]
    [InlineData(1)] [InlineData(3)] [InlineData(5)]
    public void Approach_continues_through_future_target_and_to_edge_without_changing_physical_route(double targetSpeed)
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 3, 270,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach,
            NavigationPhase: ApproachLineCaptureMath.Phase);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, targetSpeed, 10, 4)!;
        ship = ship with { ApproachRoute = route };
        var camera = new CameraState(0, 0, .01);
        var projector = new NavigationTrajectoryProjector();
        var physical = projector.Project(ship, out _, out var completion);
        var points = projector.ProjectPlayerInto(ship, new(), camera, Width, Height, out bool confirmed, out var marker);
        Assert.True(confirmed); Assert.Equal(completion, marker);
        Assert.Equal(physical, points.Take(physical.Count));
        double targetXAtCompletion = route.TargetX + route.TargetSpeedKmS * 10 * route.DurationMs / 1000;
        Assert.Contains(points, p => Math.Abs(p.X - targetXAtCompletion) < .00001 && Math.Abs(p.Y) < .00001);
        AssertEdge(points[^1], camera);
        Assert.Equal(route, ship.ApproachRoute);
        Assert.Equal(3, ship.SpeedKmS);
    }

    [Theory]
    [InlineData(1)] [InlineData(.1)] [InlineData(.001)] [InlineData(.00001)]
    public void Approach_presentation_keeps_curves_and_completion_fixed_when_zoom_changes(double ppu)
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 3, 270,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 5, 10, 4)!;
        ship = ship with { ApproachRoute = route };
        var camera = new CameraState(0, 0, ppu);
        var projector = new NavigationTrajectoryProjector();
        var original = projector.Project(ship, out _, out var completion);
        var shown = projector.ProjectPlayerInto(ship, new(), camera, Width, Height, out _, out var sameCompletion);
        Assert.Equal(completion, sameCompletion);
        Assert.Equal(original, shown.Take(original.Count));
        Assert.True(shown[^1].X >= 960 / ppu);
        Assert.InRange(shown.Count, original.Count, original.Count + 2);
    }

    [Fact]
    public void After_partial_approach_continuation_uses_absolute_route_time()
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 3, 270,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 5, 10, 4)!;
        ship = ship with { ApproachRoute = route };
        var partial = ApproachLineCaptureMath.Predict(ship, route.DurationMs / 2);
        var projector = new NavigationTrajectoryProjector();
        var camera = new CameraState(0, 0, .01);
        var initial = projector.ProjectPlayerInto(ship, new(), camera, Width, Height, out _, out _);
        var later = projector.ProjectPlayerInto(partial, new(), camera, Width, Height, out _, out _);
        Assert.Equal(initial[^2], later[^2]);
        Assert.Equal(initial[^1], later[^1]);
        Assert.Equal(new FutureTrajectoryPoint(partial.X, partial.Y), later[0]);
    }

    [Theory]
    [InlineData(500)] [InlineData(1e9)]
    public void Locked_navigation_course_reaches_edge_before_or_after_its_waypoint(double targetX)
    {
        var ship = new ObjectMotionSnapshot("ship", 0, 0, 1, 90,
            ActiveEngineCommandType: ShipEngineCommandTypes.Orbit, TurnStepDegrees: 1,
            TurnStepRemainingMs: 250, TurnStepIntervalMs: 250, NavigationTargetX: targetX,
            NavigationTargetY: 0, NavigationAngularInertiaDegPerSec: 4, NavigationLockedCourseDegrees: 90);
        var camera = new CameraState(0, 0, .00001);
        var points = new NavigationTrajectoryProjector().ProjectPlayerInto(ship, new(), camera, Width, Height, out _, out _);
        AssertEdge(points[^1], camera);
        Assert.True(points[^1].X > 0);
        Assert.Equal(0, points[^1].Y, 4);
    }

    [Fact]
    public void Zero_speed_does_not_draw_an_infinite_future_path()
    {
        List<FutureTrajectoryPoint> points = new();
        new FutureTrajectoryProjector(new LinearMotionPredictor()).ProjectPlayerInto(
            new("ship", 0, 0, 0, 90), points, new(0, 0, 1), Width, Height);
        Assert.Single(points);
    }

    [Fact]
    public void Actual_screen_keeps_player_path_at_edge_through_animated_zoom_and_resize()
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            [new("player", 10000, 10000, .001, 90, RenderObjectType: SpaceObjectType.PlayerShip)], "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock,
            mapSettings: new() { ZoomAnimationMs = 120 });
        using var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, Width, Height);
        var scale = screen.ScaleButtonRects[4]; screen.OnMouseDown(scale.MidX, scale.MidY);
        for (int i = 0; i < 8; i++)
        {
            clock += System.Diagnostics.Stopwatch.Frequency / 50;
            int w = i == 7 ? 1280 : Width, h = i == 7 ? 720 : Height;
            screen.Render(canvas, w, h);
            var end = Assert.IsType<FutureTrajectoryPoint>(screen.DisplayedPlayerTrajectoryEnd);
            double x = w / 2.0 + (end.X - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit;
            Assert.Equal(w, x, 5);
        }
    }

    [Fact]
    public void Continuous_turn_keeps_its_real_closed_path_instead_of_a_fake_straight_extension()
    {
        var ship = new ObjectMotionSnapshot("ship", 0, 0, 1, 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.TurnRightUntilCancel,
            TurnStepDegrees: 90, TurnStepRemainingMs: 1000, TurnStepIntervalMs: 1000);
        var projector = new FutureTrajectoryProjector(new LinearMotionPredictor());
        List<FutureTrajectoryPoint> shown = new();
        projector.ProjectPlayerInto(ship, shown, new(0, 0, .001), Width, Height);
        Assert.Equal(projector.Project(ship), shown);
    }

    private static void AssertEdge(FutureTrajectoryPoint point, CameraState camera)
    {
        var screen = camera.WorldToScreen(point.X, point.Y, Width, Height);
        Assert.InRange(screen.X, -.001f, Width + .001f); Assert.InRange(screen.Y, -.001f, Height + .001f);
        Assert.True(Math.Abs(screen.X) < .001 || Math.Abs(screen.X - Width) < .001 ||
            Math.Abs(screen.Y) < .001 || Math.Abs(screen.Y - Height) < .001);
    }
}
