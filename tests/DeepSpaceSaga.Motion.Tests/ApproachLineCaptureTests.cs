using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion.Tests;

public class ApproachLineCaptureTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Every_route_finishes_on_the_live_aft_ray_at_unchanged_speed(double targetSpeed)
    {
        foreach (double distance in new[] { 5.0, 100, 2000 })
        for (int bearing = 0; bearing < 360; bearing += 45)
        for (int heading = 0; heading < 360; heading += 45)
        {
            double rad = bearing * Math.PI / 180;
            var ship = new ObjectMotionSnapshot("ship", distance * Math.Sin(rad), distance * Math.Cos(rad), 3, heading);
            var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, targetSpeed, 10, 4)!;
            var result = ApproachLineCaptureMath.Predict(ship with { ApproachRoute = route }, (long)Math.Ceiling(route.DurationMs));
            double targetX = targetSpeed * 10 * Math.Ceiling(route.DurationMs) / 1000;
            Assert.True(Math.Abs(result.Y) < 0.0001, $"Lateral miss {result.Y}: bearing={bearing}, heading={heading}, d={distance}");
            Assert.True(result.X - targetX <= -9.96, $"Not behind: {result.X - targetX}");
            Assert.InRange(Math.Abs(ApproachLineCaptureMath.Delta(result.Direction, 90)), 0, 1e-7);
            Assert.Equal(ship.SpeedKmS, result.SpeedKmS);
            Assert.Null(result.ApproachRoute);
            if (targetSpeed >= ship.SpeedKmS)
            {
                var fallback = ApproachPursuitMath.CreateFlyThroughPlan(ship.X, ship.Y, ship.Direction, ship.SpeedKmS, -10, 0, 90, 4);
                double routeGap = targetSpeed / ship.SpeedKmS * route.Length - result.X;
                double fallbackGap = targetSpeed / ship.SpeedKmS * fallback.RemainingUnits + 10;
                // Small finite-flight tradeoff is allowed; a long detour away from the target is not.
                Assert.True(routeGap <= fallbackGap + .01 * fallback.RemainingUnits + .05);
            }
            else
                Assert.InRange(result.X - targetX, -10.05, -9.95);
        }
    }

    [Theory]
    [InlineData(1.069, 57)]
    [InlineData(1.911, 110)]
    [InlineData(1.65, 91)]
    public void Faster_target_is_approached_obliquely_instead_of_rushing_perpendicularly_to_its_line(
        double targetSpeed, double heading)
    {
        double angle = heading * Math.PI / 180;
        double fx = Math.Sin(angle), fy = -Math.Cos(angle);
        // Screenshot geometry in the target frame: ship well behind and to one side.
        var ship = new ObjectMotionSnapshot("ship", -5000 * fx + 4000 * fy,
            -5000 * fy - 4000 * fx, .7, 0);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, heading, targetSpeed, 10, 4)!;
        var end = ApproachLineCaptureMath.PredictPose(route, route.DurationMs);
        double alongEnd = end.X * fx + end.Y * fy;
        Assert.True(alongEnd > -4000, $"Insufficient progress towards target: {alongEnd}");
        double gap = targetSpeed / .7 * route.Length - alongEnd;
        var perpendicular = ApproachPursuitMath.CreateFlyThroughPlan(ship.X, ship.Y, ship.Direction,
            .7, -5000 * fx, -5000 * fy, heading, 4);
        double oldGap = targetSpeed / .7 * perpendicular.RemainingUnits + 5000;
        Assert.True(gap < oldGap * .96, $"New gap {gap} vs perpendicular gap {oldGap}");
        Assert.False(ApproachLineCaptureMath.IsRendezvous(route));
        Assert.Equal(heading, end.Direction);
        // The end-heading must be reached by the arc, never by snapping at completion.
        var before = ApproachLineCaptureMath.PredictPose(route, route.DurationMs - 1);
        Assert.InRange(Math.Abs(ApproachLineCaptureMath.Delta(before.Direction, end.Direction)), 0, .004001);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1.000001)]
    [InlineData(1.001)]
    public void Equal_and_near_equal_speeds_have_a_finite_useful_merge(double targetSpeed)
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 1, 90);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, targetSpeed, 10, 4)!;
        Assert.InRange(route.DurationMs, 1, 1_000_000);
        var end = ApproachLineCaptureMath.PredictPose(route, route.DurationMs);
        Assert.InRange(Math.Abs(end.Y), 0, .001);
        double gap = targetSpeed * route.Length - end.X;
        Assert.InRange(gap, 1000, 1150);
    }
    [Theory]
    [InlineData(2.9)]
    [InlineData(2.999)]
    [InlineData(2.9999)]
    public void Slight_speed_advantage_still_finds_a_real_trailing_rendezvous(double targetSpeed)
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 3, 270);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, targetSpeed, 10, 4)!;
        var end = ApproachLineCaptureMath.PredictPose(route, route.DurationMs);
        Assert.True(ApproachLineCaptureMath.IsRendezvous(route));
        Assert.InRange(targetSpeed * 10 * route.DurationMs / 1000 - end.X, 9.999, 10.001);
        Assert.InRange(Math.Abs(end.Y), 0, .001);
    }
    [Fact]
    public void Faster_incoming_target_can_still_reach_a_trailing_rendezvous()
    {
        var ship = new ObjectMotionSnapshot("ship", 1000, 200, .7, 90);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 1.069, 10, 4)!;
        Assert.True(ApproachLineCaptureMath.IsRendezvous(route));
        var end = ApproachLineCaptureMath.PredictPose(route, route.DurationMs);
        Assert.InRange(1.069 * 10 * route.DurationMs / 1000 - end.X, 9.99, 10.01);
        Assert.InRange(Math.Abs(end.Y), 0, .001);
    }
    [Fact]
    public void Long_closing_horizon_does_not_skip_an_early_rendezvous_with_target_behind()
    {
        var ship = new ObjectMotionSnapshot("ship", 0, 0, 3, 90);
        // Construct a known feasible rendezvous; the moving slot reaches this
        // endpoint at exactly the time required to fly the reference curve.
        var reference = ApproachPursuitMath.CreateFlyThroughPlan(0, 0, 90, 3, 1000, -500, 90, 4);
        double feasibleMs = reference.RemainingUnits / 30 * 1000;
        double targetX = 1010 - 2.9999 * 10 * feasibleMs / 1000;
        var route = ApproachLineCaptureMath.Plan(ship, targetX, -500, 90, 2.9999, 10, 4)!;
        Assert.True(ApproachLineCaptureMath.IsRendezvous(route));
        Assert.InRange(route.DurationMs, 1, feasibleMs + .1);
    }
    [Fact]
    public void Already_trailing_ship_does_not_start_a_loop()
    {
        var ship = new ObjectMotionSnapshot("ship", -500, 0, 1, 90);
        Assert.Equal(0, ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 2, 10, 4)!.Length);
    }

    [Fact]
    public void Coincident_trailing_slot_with_opposite_heading_still_gets_a_rendezvous()
    {
        var ship = new ObjectMotionSnapshot("ship", -10, 0, 3, 270);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 1, 10, 4)!;
        var result = ApproachLineCaptureMath.Predict(ship with { ApproachRoute = route }, route.DurationMs);
        Assert.InRange(result.X - route.DurationMs / 100, -10.01, -9.99);
        Assert.InRange(Math.Abs(result.Y), 0, .001);
        Assert.Equal(90, result.Direction);
    }

    [Fact]
    public void Faster_target_capture_uses_a_nearby_line_entry_instead_of_its_distant_position()
    {
        var ship = new ObjectMotionSnapshot("ship", -100000, 300, 1, 90);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 2, 10, 4)!;
        Assert.InRange(route.DurationMs, 30000, 90000);
    }

    [Fact]
    public void Partial_arc_prediction_is_composable_and_respects_turn_and_speed_limits()
    {
        var ship = new ObjectMotionSnapshot("ship", -200, 300, 3, 270);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 1, 10, 4)!;
        var origin = ship with { ApproachRoute = route };
        var previous = origin;
        for (long t = 37; t < route.DurationMs; t += 37)
        {
            var direct = ApproachLineCaptureMath.Predict(origin, t);
            var incremental = ApproachLineCaptureMath.Predict(previous, 37);
            Assert.Equal(direct.X, incremental.X, 8);
            Assert.Equal(direct.Y, incremental.Y, 8);
            Assert.InRange(Math.Abs(ApproachLineCaptureMath.Delta(previous.Direction, direct.Direction)), 0, 4 * .037 + 1e-7);
            Assert.Equal(3, direct.SpeedKmS);
            Assert.InRange(Math.Sqrt(Math.Pow(direct.X - previous.X, 2) + Math.Pow(direct.Y - previous.Y, 2)), 0, 30 * .037 + 1e-7);
            previous = direct;
        }
    }
}
