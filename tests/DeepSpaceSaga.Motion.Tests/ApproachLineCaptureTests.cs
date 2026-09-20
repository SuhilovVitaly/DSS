using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion.Tests;

public class ApproachLineCaptureTests
{

    [Theory]
    [InlineData(2.999999)]
    [InlineData(2.9999999)]
    [InlineData(2.99999999)]
    public void Near_equal_speed_roundoff_does_not_replace_a_rendezvous_with_the_fallback(double targetSpeed)
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 3, 60);
        var route = ApproachLineCaptureMath.Plan(ship, 10, 0, 90, targetSpeed, 10, 4)!;
        Assert.True(ApproachLineCaptureMath.IsRendezvous(route), $"Expected rendezvous, got {route}");
        var end = ApproachLineCaptureMath.PredictPose(route, route.DurationMs);
        double targetX = 10 + targetSpeed * 10 * route.DurationMs / 1000;
        Assert.InRange(targetX - end.X, 9.999, 10.001);
        Assert.InRange(Math.Abs(end.Y), 0, .001);
        Assert.Equal(90, end.Direction);
    }

    [Fact]
    public void Planner_version_is_incremented_for_new_routes()
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 3, 270);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 1, 10, 4)!;
        Assert.Equal(3, ApproachLineCaptureMath.PlannerVersion);
        Assert.Equal(3, route.PlannerVersion);
        // Published old routes remain executable; version-based replanning is an Engine decision.
        foreach (int version in new[] { 0, 1, 2 })
        {
            var saved = route with { PlannerVersion = version };
            Assert.Equal(ApproachLineCaptureMath.PredictPose(route, route.DurationMs / 2),
                ApproachLineCaptureMath.PredictPose(saved, saved.DurationMs / 2));
        }
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(2.9)]
    [InlineData(2.9999)]
    public void Plan_selects_the_shortest_catchable_trailing_rendezvous(double targetSpeed)
    {
        var ship = new ObjectMotionSnapshot("ship", -650, 330, 3, 247);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, targetSpeed, 35, 4)!;
        var roots = ApproachReference.Intercepts(ship, -35, 0, 90, targetSpeed, 4);
        Assert.NotEmpty(roots);
        double reference = roots.Min(p => p.Plan.RemainingUnits);
        Assert.True(route.Length <= reference + ApproachReference.Tolerance,
            $"Selected {route.Length:R}; reference {reference:R}");
        Assert.True(ApproachLineCaptureMath.IsRendezvous(route));
        Assert.Equal(route, ApproachLineCaptureMath.Plan(ship, 0, 0, 90, targetSpeed, 35, 4));
        Assert.Equal(-650, ship.X);
        Assert.Null(ship.ApproachRoute);
    }

    [Fact]
    public void Plan_is_not_longer_than_the_exhaustive_reference_corpus()
    {
        // Reference uses 30,000 logarithmic time samples per family plus independent
        // bisection, never the production breakpoints or candidate-selection logic.
        var random = new Random(20260920);
        for (int i = 0; i < 48; i++)
        {
            double speed = 0.5 + random.NextDouble() * 4;
            double targetSpeed = speed * (i % 3 == 0 ? .9999 : .05 + .9 * random.NextDouble());
            double heading = random.NextDouble() * 360;
            var ship = new ObjectMotionSnapshot("ship",
                random.NextDouble() * 4000 - 2000, random.NextDouble() * 4000 - 2000,
                speed, random.NextDouble() * 360);
            const double trail = 20;
            double rad = heading * Math.PI / 180;
            var roots = ApproachReference.Intercepts(
                ship, -trail * Math.Sin(rad), trail * Math.Cos(rad), heading, targetSpeed, 4);
            Assert.NotEmpty(roots);
            var route = ApproachLineCaptureMath.Plan(ship, 0, 0, heading, targetSpeed, trail, 4)!;
            double reference = roots.Min(p => p.Plan.RemainingUnits);
            Assert.True(route.Length <= reference + ApproachReference.Tolerance,
                $"Case {i}: {route.Type} length {route.Length:R} > reference {reference:R}");
            Assert.True(ApproachLineCaptureMath.IsRendezvous(route), $"Case {i}: not a rendezvous");
            var end = ApproachLineCaptureMath.PredictPose(route, route.DurationMs);
            Assert.InRange(Math.Abs(end.X * Math.Cos(rad) + end.Y * Math.Sin(rad)), 0, .001);
        }
    }

    [Fact]
    public void Unreachable_target_uses_the_shortest_curve_to_the_captured_trailing_point()
    {
        // Targets start ahead and cannot be intercepted. Enumerate all six families
        // independently at the fixed destination, not along an arbitrary aft ray.
        var random = new Random(902026);
        for (int i = 0; i < 60; i++)
        {
            var ship = new ObjectMotionSnapshot("ship",
                -20 - random.NextDouble() * 4000, random.NextDouble() * 2000 - 1000,
                1, random.NextDouble() * 360);
            double targetSpeed = i % 3 == 0 ? 1 : i % 3 == 1 ? 1.000001 : 1 + random.NextDouble() * 2;
            var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, targetSpeed, 10, 4)!;
            var end = ApproachLineCaptureMath.PredictPose(route, route.DurationMs);
            double reference = ApproachReference.Families.Select(type =>
                ApproachReference.Curve(ship, -10, 0, 90, 4, type, out var plan)
                    ? plan.RemainingUnits : double.PositiveInfinity).Min();
            Assert.InRange(Math.Abs(route.Length - reference), 0, ApproachReference.Tolerance);
            Assert.InRange(Math.Abs(end.X + 10), 0, .001);
            Assert.InRange(Math.Abs(end.Y), 0, .001);
            Assert.False(ApproachLineCaptureMath.IsRendezvous(route));
            Assert.Equal(route.Length / 10 * 1000, route.DurationMs, 6);
            Assert.Equal(route, ApproachLineCaptureMath.Plan(ship, 0, 0, 90, targetSpeed, 10, 4));
        }
    }

    [Fact]
    public void Symmetric_incoming_intercept_ties_use_the_canonical_family_order()
    {
        var ship = new ObjectMotionSnapshot("ship", 1000, 0, 1, 90);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 2, 10, 4)!;
        var mirrored = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 2, 10, 4)!;
        Assert.Equal(route, mirrored);
        ApproachReference.Curve(ship, 2010, 0, 90, 4, "LSL", out var straight);
        Assert.True(route.Type == "LSL", $"Route: {route}; straight LSL: {straight}");
        Assert.InRange(Math.Abs(route.Length - 1010), 0, 1e-5);
    }

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
                        if (!ApproachLineCaptureMath.IsRendezvous(route))
                        {
                            Assert.InRange(Math.Abs(route.Length - fallback.RemainingUnits), 0, .001);
                            Assert.InRange(Math.Abs(result.X + 10), 0, .05);
                        }
                    }
                    else
                        Assert.True(Math.Abs(result.X - targetX + 10) <= .05,
                            $"Gap {result.X - targetX:R}: bearing={bearing}, heading={heading}, d={distance}; {route}");
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
        Assert.InRange(Math.Abs(alongEnd + 10), 0, .001);
        Assert.InRange(Math.Abs(end.X + 10 * fx), 0, .001);
        Assert.InRange(Math.Abs(end.Y + 10 * fy), 0, .001);
        // The long middle leg points diagonally at the destination, as drawn in red,
        // rather than following the old ~178.6-degree lag-minimizing course.
        Assert.Equal('S', route.Type[1]);
        var straight = ApproachLineCaptureMath.PredictPose(route,
            (route.First + route.Second / 2) / (.7 * 10) * 1000);
        double bearing = Math.Atan2(end.X - straight.X, straight.Y - end.Y) * 180 / Math.PI;
        Assert.InRange(Math.Abs(ApproachLineCaptureMath.Delta(straight.Direction, bearing)), 0, 3);
        if (heading == 110)
            Assert.InRange(straight.Direction, 140, 155);
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
    public void Equal_and_near_equal_speeds_use_a_finite_captured_destination(double targetSpeed)
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 1, 90);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, targetSpeed, 10, 4)!;
        Assert.InRange(route.DurationMs, 1, 1_000_000);
        var end = ApproachLineCaptureMath.PredictPose(route, route.DurationMs);
        Assert.InRange(Math.Abs(end.Y), 0, .001);
        Assert.InRange(Math.Abs(end.X + 10), 0, .001);
        Assert.False(ApproachLineCaptureMath.IsRendezvous(route));
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
    public void Already_trailing_ship_flies_straight_to_the_destination_instead_of_finishing_early()
    {
        var ship = new ObjectMotionSnapshot("ship", -500, 0, 1, 90);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 2, 10, 4)!;
        Assert.Equal(490, route.Length, 6);
        Assert.Equal(0, route.First, 6);
        Assert.Equal(0, route.Third, 6);
        Assert.False(ApproachLineCaptureMath.IsRendezvous(route));
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
    public void Faster_target_capture_reaches_its_captured_position_not_a_nearby_line_entry()
    {
        var ship = new ObjectMotionSnapshot("ship", -100000, 300, 1, 90);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, 2, 10, 4)!;
        Assert.InRange(route.DurationMs, 9_999_000, 10_001_000);
        var end = ApproachLineCaptureMath.PredictPose(route, route.DurationMs);
        Assert.InRange(Math.Abs(end.X + 10), 0, .001);
        Assert.InRange(Math.Abs(end.Y), 0, .001);
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
