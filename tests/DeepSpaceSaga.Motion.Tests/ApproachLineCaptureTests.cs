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
                Assert.True(route.Length <= fallback.RemainingUnits + 1e-7);
            }
            else
                Assert.InRange(result.X - targetX, -10.05, -9.95);
        }
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
