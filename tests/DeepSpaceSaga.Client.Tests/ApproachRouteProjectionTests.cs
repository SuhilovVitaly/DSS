using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.Tests;

public class ApproachRouteProjectionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2.9999)]
    [InlineData(5)]
    public void Preview_uses_the_exact_route_and_ends_on_the_target_line(double targetSpeed)
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 3, 270,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach,
            NavigationPhase: ApproachLineCaptureMath.Phase);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, targetSpeed, 10, 4)!;
        ship = ship with { ApproachRoute = route };
        var projector = new NavigationTrajectoryProjector();
        var points = projector.Project(ship, out bool confirmed, out var endpoint);
        var expected = ApproachLineCaptureMath.Predict(ship, route.DurationMs);
        Assert.Equal(ApproachLineCaptureMath.IsRendezvous(route), confirmed);
        Assert.Equal(ship.X, points[0].X);
        Assert.Equal(ship.Y, points[0].Y);
        Assert.Equal(expected.X, endpoint.X, 7);
        Assert.Equal(expected.Y, endpoint.Y, 7);
        Assert.Equal(endpoint, points[^1]);
        Assert.InRange(Math.Abs(endpoint.Y), 0, .001);
        Assert.InRange(points.Count, 2, 801);

        var mid = new LinearMotionPredictor().Predict(ship, 1234);
        var remaining = projector.Project(mid, out _, out var sameEndpoint);
        Assert.Equal(mid.X, remaining[0].X);
        Assert.Equal(mid.Y, remaining[0].Y);
        Assert.Equal(endpoint.X, sameEndpoint.X, 7);
        Assert.Equal(endpoint.Y, sameEndpoint.Y, 7);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1)]
    [InlineData(1.1)]
    public void Preview_uses_the_exact_route_endpoint_even_when_sampling_is_coarse(double elapsedFraction)
    {
        // Each short arc receives only one sample; the straight receives one as well.
        var route = new ApproachRoute(0, 0, 90, 3, 4, "LSR", 0.1, 1200.123, 0.2,
            2000, 0, 90, 5, 10, PlannerVersion: 3);
        route = route with { ElapsedMs = route.DurationMs * elapsedFraction };
        var ship = SnapshotAt(route);
        // Other snapshot coordinates must not displace either analytical endpoint.
        ship = ship with { X = ship.X + 1, Y = ship.Y - 1 };
        var buffer = new List<FutureTrajectoryPoint> { new(-123, -456) };

        var points = new NavigationTrajectoryProjector().ProjectInto(ship, buffer, out _, out var endpoint);

        var expectedStart = ApproachLineCaptureMath.PredictPose(route, Math.Min(route.ElapsedMs, route.DurationMs));
        var expectedEnd = ApproachLineCaptureMath.PredictPose(route, route.DurationMs);
        Assert.Same(buffer, points);
        AssertPoint(expectedStart.X, expectedStart.Y, points[0]);
        Assert.Equal(new FutureTrajectoryPoint(expectedEnd.X, expectedEnd.Y), endpoint);
        Assert.Equal(endpoint, points[^1]);
        Assert.InRange(points.Count, 1, 4);
        if (elapsedFraction >= 1) Assert.Single(points);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Preview_resumes_each_route_segment_and_preserves_remaining_arcs(int segment)
    {
        // Two quarter circles joined by a straight, with an independently known end pose.
        double radius = 10 / (4 * Math.PI / 180);
        double arc = Math.PI * radius / 2;
        var route = new ApproachRoute(0, 0, 0, 1, 4, "RSL", arc, 1000, arc,
            1000 + 2 * radius, -2 * radius - 10, 0, 0, 10, PlannerVersion: 3);
        double elapsedLength = segment switch
        {
            0 => arc / 2,
            1 => arc + 500,
            _ => arc + 1000 + arc / 2
        };
        route = route with { ElapsedMs = elapsedLength / 10 * 1000 };
        var ship = SnapshotAt(route);

        var points = new NavigationTrajectoryProjector().Project(ship, out _, out var endpoint);

        AssertPoint(ship.X, ship.Y, points[0]);
        AssertPoint(1000 + 2 * radius, -2 * radius, endpoint);
        Assert.Equal(endpoint, points[^1]);
        Assert.True(points.Count > 20, "The remaining quarter/half-quarter circle must stay visible.");
        if (segment == 0)
            Assert.Contains(points, p => Math.Abs(p.X - radius) < .001 && Math.Abs(p.Y + radius) < .001);
        if (segment <= 1)
            Assert.Contains(points, p => Math.Abs(p.X - radius - 1000) < .001 && Math.Abs(p.Y + radius) < .001);
        Assert.DoesNotContain(new FutureTrajectoryPoint(0, 0), points);
    }

    [Theory]
    [InlineData(0, 90, true)]
    [InlineData(1, 90, true)]
    [InlineData(2.9999, 90, true)]
    [InlineData(3, 270, true)]
    [InlineData(5, 270, true)]
    [InlineData(3, 90, false)]
    [InlineData(5, 90, false)]
    public void Preview_marks_only_a_route_rendezvous_as_confirmed(
        double targetSpeed, double targetDirection, bool expectedConfirmed)
    {
        var ship = new ObjectMotionSnapshot("ship", 0, 0, 3, 90,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach);
        var route = ApproachLineCaptureMath.Plan(ship, 30000, 0, targetDirection, targetSpeed, 10, 4)!;

        var points = new NavigationTrajectoryProjector().Project(
            ship with { ApproachRoute = route }, out bool confirmed, out var endpoint);

        Assert.Equal(expectedConfirmed, ApproachLineCaptureMath.IsRendezvous(route));
        Assert.Equal(expectedConfirmed, confirmed);
        var expected = ApproachLineCaptureMath.PredictPose(route, route.DurationMs);
        AssertPoint(expected.X, expected.Y, endpoint);
        Assert.Equal(endpoint, points[^1]);
    }

    [Fact]
    public void Captured_point_preview_keeps_its_endpoint_as_the_target_moves()
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 3, 270,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 5, 10, 4)!;
        Assert.False(ApproachLineCaptureMath.IsRendezvous(route));
        var projector = new NavigationTrajectoryProjector();
        int previousCount = int.MaxValue;

        foreach (double fraction in new[] { 0, .25, .75, 1 })
        {
            double elapsed = fraction * route.DurationMs;
            var current = SnapshotAt(route with { ElapsedMs = elapsed }) with
            {
                NavigationTargetX = route.TargetX + route.TargetSpeedKmS * 10 * elapsed / 1000,
                NavigationTargetY = route.TargetY,
                NavigationTargetSpeedKmS = route.TargetSpeedKmS,
                NavigationTargetDirectionDegrees = route.TargetDirection,
                NavigationPhase = ApproachPursuitMath.FlyThroughPendingPhase
            };

            var points = projector.Project(current, out bool confirmed, out var endpoint);

            Assert.False(confirmed);
            AssertPoint(-route.TrailDistance, 0, endpoint);
            Assert.Equal(endpoint, points[^1]);
            AssertPoint(current.X, current.Y, points[0]);
            Assert.True(points.Count <= previousCount);
            previousCount = points.Count;
            if (fraction == 1) Assert.Single(points);
        }
    }

    [Fact]
    public void Repeated_route_projection_reuses_geometry_without_steady_state_allocations()
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 3, 270,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 5, 10, 4)! with
        {
            ElapsedMs = 0
        };
        var current = SnapshotAt(route with { ElapsedMs = route.DurationMs / 3 });
        var projector = new NavigationTrajectoryProjector();
        var points = new List<FutureTrajectoryPoint>(1024);

        projector.ProjectInto(current, points, out _, out _);
        projector.ProjectInto(current, points, out _, out _);
        projector.ProjectInto(current, points, out _, out _);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
            projector.ProjectInto(current, points, out _, out _);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void Route_signature_change_rebuilds_cached_geometry()
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 3, 270,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 1, 10, 4)!;
        var changed = route with { Direction = route.Direction + 15, PlannerVersion = route.PlannerVersion + 1 };
        var projector = new NavigationTrajectoryProjector();

        var before = projector.Project(SnapshotAt(route), out _, out _);
        var after = projector.Project(SnapshotAt(changed), out _, out _);

        Assert.NotEqual(before[1], after[1]);
    }

    [Theory]
    [InlineData(1, 0.25)]
    [InlineData(5, 0.5)]
    public void Cached_projection_matches_the_exact_uncached_route(double targetSpeed, double elapsedFraction)
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 3, 270,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, targetSpeed, 10, 4)!;
        var current = SnapshotAt(route with { ElapsedMs = route.DurationMs * elapsedFraction });

        var expectedProjector = new NavigationTrajectoryProjector();
        var expected = expectedProjector.Project(current, out bool expectedConfirmed, out var expectedEndpoint);
        var projector = new NavigationTrajectoryProjector();
        projector.Project(SnapshotAt(route), out _, out _); // warm the route cache at another elapsed time
        var actual = projector.Project(current, out bool actualConfirmed, out var actualEndpoint);

        Assert.Equal(expected, actual);
        Assert.Equal(expectedConfirmed, actualConfirmed);
        Assert.Equal(expectedEndpoint, actualEndpoint);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1)]
    public void ProjectPlayerInto_continues_from_the_route_endpoint_to_a_stationary_target(double fraction)
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 3, 270,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 0, 10, 4)!;
        ship = SnapshotAt(route with { ElapsedMs = route.DurationMs * fraction });
        if (fraction == 1) ship = ship with { SpeedKmS = 0 };
        var projector = new NavigationTrajectoryProjector();
        var maneuver = projector.Project(ship, out bool expectedConfirmed, out var expectedEndpoint);

        var points = projector.ProjectPlayerInto(ship, [], new CameraState(0, 0, 1), 1280, 720,
            out bool confirmed, out var endpoint, out int maneuverPointCount);

        Assert.Equal(expectedConfirmed, confirmed);
        Assert.Equal(expectedEndpoint, endpoint);
        Assert.Equal(maneuver.Count, maneuverPointCount);
        Assert.Equal(maneuver, points.Take(maneuverPointCount));
        Assert.Equal(endpoint, points[maneuverPointCount - 1]);
        Assert.Equal(maneuverPointCount + 1, points.Count);
        AssertPoint(route.TargetX, route.TargetY, points[^1]);
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(1, 90, 0.5)]
    [InlineData(1, 225, 0.5)]
    [InlineData(5, 90, 0)]
    [InlineData(5, 90, 0.5)]
    [InlineData(5, 90, 1)]
    public void ProjectPlayerInto_extends_a_moving_target_course_to_the_viewport_edge(
        double targetSpeed, double heading, double fraction)
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 3, 270,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, heading, targetSpeed, 10, 4)!;
        ship = SnapshotAt(route with { ElapsedMs = route.DurationMs * fraction }) with
        {
            NavigationTargetX = -50000,
            NavigationTargetY = 50000,
            NavigationTargetDirectionDegrees = heading + 90,
            NavigationTargetSpeedKmS = 0
        };
        double dx = Math.Sin(heading * Math.PI / 180), dy = -Math.Cos(heading * Math.PI / 180);
        double distance = targetSpeed * 10 * route.DurationMs / 1000;
        double targetX = route.TargetX + distance * dx, targetY = route.TargetY + distance * dy;
        var camera = new CameraState(targetX, targetY, .01);
        var projector = new NavigationTrajectoryProjector();
        var maneuver = projector.Project(ship, out bool expectedConfirmed, out var expectedEndpoint);

        var points = projector.ProjectPlayerInto(ship, [], camera, 1280, 720,
            out bool confirmed, out var endpoint, out int maneuverPointCount);

        Assert.Equal(expectedConfirmed, confirmed);
        Assert.Equal(expectedEndpoint, endpoint);
        Assert.Equal(maneuver.Count, maneuverPointCount);
        Assert.Equal(maneuver, points.Take(maneuverPointCount));
        Assert.Equal(maneuverPointCount + 2, points.Count);
        AssertPoint(targetX, targetY, points[maneuverPointCount]);
        double edgeDistance = Math.Min(64000 / Math.Abs(dx), 36000 / Math.Abs(dy));
        AssertPoint(targetX + edgeDistance * dx, targetY + edgeDistance * dy, points[^1]);
        for (int i = maneuverPointCount; i < points.Count; i++)
        {
            double x = points[i].X - endpoint.X, y = points[i].Y - endpoint.Y;
            Assert.InRange(Math.Abs(x * dy - y * dx), 0, .001);
            Assert.True(x * dx + y * dy > 0);
        }
        if (targetSpeed > ship.SpeedKmS) Assert.False(confirmed);
    }

    private static ObjectMotionSnapshot SnapshotAt(ApproachRoute route)
    {
        var pose = ApproachLineCaptureMath.PredictPose(route, route.ElapsedMs);
        return new ObjectMotionSnapshot("ship", pose.X, pose.Y, route.SpeedKmS, pose.Direction,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach,
            NavigationPhase: ApproachLineCaptureMath.Phase,
            ApproachRoute: route);
    }

    private static void AssertPoint(double x, double y, FutureTrajectoryPoint actual)
    {
        Assert.InRange(Math.Sqrt(Math.Pow(actual.X - x, 2) + Math.Pow(actual.Y - y, 2)), 0, .001);
    }
}
