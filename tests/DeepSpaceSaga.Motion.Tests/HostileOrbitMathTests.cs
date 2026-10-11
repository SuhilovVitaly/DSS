using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion.Tests;

public class HostileOrbitMathTests
{
    [Theory]
    [InlineData(40, 0, 0, 0)]
    [InlineData(40, 0.4, 90, 359.9)]
    [InlineData(2, 0.4, 270, 180)]
    [InlineData(0, 0, 0, 90)]
    [InlineData(0, 0.4, 0, 359.9)]
    [InlineData(40, 0.7, 30, 170)]
    [InlineData(40, 1.9, 0, 270)]
    public void Reachable_target_holds_band_for_three_complete_orbits(double initialDistanceKm,
        double targetSpeed, double targetHeading, double shipHeading)
    {
        const double radius = 10, maxSpeed = 2, acceleration = 0.2, turn = 30;
        var target = new ObjectMotionSnapshot("player", 0, 0, targetSpeed, targetHeading);
        var ship = new ObjectMotionSnapshot("enemy", initialDistanceKm * 10, 0, 0, shipHeading);
        bool direction = HostileOrbitMath.ChooseClockwise(ship, target);
        // Conservative feasible speed from reserve, centripetal acceleration and turn radius.
        double relativeSpeed = Math.Min((maxSpeed - targetSpeed) * 0.25,
            Math.Min(Math.Sqrt(acceleration * radius * 0.1), turn * Math.PI / 180 * radius * 0.1));
        // Independent budget: four approach-plus-circumference travel times, one heading
        // revolution and a full acceleration ramp. No waiting for an unbounded convergence.
        double settlingSeconds = 4 * (initialDistanceKm + 2 * Math.PI * radius) / relativeSpeed +
            360 / turn + maxSpeed / acceleration;
        int settlingSteps = (int)Math.Ceiling(settlingSeconds * 10);
        double revolutions = 0, oldBearing = 0;
        int verificationSteps = (int)Math.Ceiling(4 * 2 * Math.PI * radius / relativeSpeed * 10);
        for (int i = 0; i < settlingSteps + verificationSteps; i++)
        {
            var planned = HostileOrbitMath.Plan(ship, target, i * 100L, "engine", "weapon", radius,
                maxSpeed, acceleration, turn, ship.HostileCombat);
            Assert.Equal(direction, planned.Clockwise);
            Assert.InRange(Math.Abs(planned.Segment.AccelerationKmS2), 0, acceleration);
            Assert.InRange(Math.Abs(planned.Segment.TurnRateDegPerSec), 0, turn);
            var next = new LinearMotionPredictor().Predict(ship with { HostileCombat = planned }, 100);
            Assert.InRange(next.SpeedKmS, 0, maxSpeed);
            Assert.InRange(Math.Sqrt(Math.Pow(next.X - ship.X, 2) + Math.Pow(next.Y - ship.Y, 2)), 0, maxSpeed + 1e-10);
            ship = next;
            target = new LinearMotionPredictor().Predict(target, 100);
            double dx = (ship.X - target.X) / 10, dy = (ship.Y - target.Y) / 10;
            double bearing = Math.Atan2(dx, -dy);
            if (i >= settlingSteps)
            {
                Assert.InRange(Math.Sqrt(dx * dx + dy * dy), radius * 0.95, radius * 1.05);
                Assert.Equal(HostileCombatPhase.OrbitHold, ship.HostileCombat!.Phase);
                double delta = Math.Atan2(Math.Sin(bearing - oldBearing), Math.Cos(bearing - oldBearing));
                Assert.True(direction ? delta > 0 : delta < 0);
                revolutions += Math.Abs(delta) / (2 * Math.PI);
            }
            oldBearing = bearing;
        }
        Assert.True(revolutions >= 3, $"Only {revolutions} complete turns after settling");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Equal_or_faster_target_uses_maximum_pursuit_without_teleport(double targetSpeed)
    {
        var target = new ObjectMotionSnapshot("player", 1000, 0, targetSpeed, 90);
        var ship = new ObjectMotionSnapshot("enemy", 0, 0, 0.5, 90);
        for (int i = 0; i < 100; i++)
        {
            var plan = HostileOrbitMath.Plan(ship, target, i * 100, "e", "w", 10, 2, 0.2, 30, ship.HostileCombat);
            Assert.Equal(HostileCombatPhase.Pursuit, plan.Phase);
            double x = ship.X;
            ship = HostileOrbitMath.Predict(ship with { HostileCombat = plan }, 100);
            Assert.InRange(ship.X - x, 0.05, 2.000000001);
            target = new LinearMotionPredictor().Predict(target, 100);
        }
        Assert.Equal(2, ship.SpeedKmS, 10);
    }

    [Fact]
    public void Direction_uses_shortest_tangent_turn_and_clockwise_tie()
    {
        var target = new ObjectMotionSnapshot("p", 0, 0, 0, 0);
        var ship = new ObjectMotionSnapshot("e", 100, 0, 0, 90);
        Assert.True(HostileOrbitMath.ChooseClockwise(ship, target));
        Assert.True(HostileOrbitMath.ChooseClockwise(ship with { Direction = 179 }, target));
        Assert.False(HostileOrbitMath.ChooseClockwise(ship with { Direction = 359 }, target));
        Assert.True(HostileOrbitMath.ChooseClockwise(ship with { X = 0 }, target));
    }

    [Fact]
    public void Split_prediction_matches_single_step_and_never_plans_future_segment()
    {
        var target = new ObjectMotionSnapshot("p", 0, 0, 0.5, 90);
        var ship = new ObjectMotionSnapshot("e", 1000, 0, 0.4, 359);
        ship = ship with { HostileCombat = HostileOrbitMath.Plan(ship, target, 700, "e", "w", 10, 2, 0.2, 30) };
        var predictor = new LinearMotionPredictor();
        Assert.False(LinearMotionPredictor.IsLinear(ship));
        var whole = predictor.Predict(ship, 100);
        var split = predictor.Predict(predictor.Predict(predictor.Predict(ship, 13), 41), 46);
        Assert.Equal(whole, split);
        Assert.Equal(whole, predictor.Predict(ship, long.MaxValue));
        Assert.Equal(ship, predictor.Predict(whole, -100));
        Assert.Equal(700, whole.HostileCombat!.Segment.EpochMs);
    }

    [Fact]
    public void Analytic_segment_matches_independent_numerical_integration()
    {
        var segment = new HostileMotionSegment(0, 100, 0, 4, -3, 1.2, 359, 0.7, 72);
        double x = 4, y = -3;
        const int samples = 10_000;
        for (int i = 0; i < samples; i++)
        {
            double t = (i + 0.5) * 0.1 / samples;
            double heading = (359 + 72 * t) * Math.PI / 180;
            double distance = (1.2 + 0.7 * t) / samples;
            x += distance * Math.Sin(heading);
            y -= distance * Math.Cos(heading);
        }
        var result = HostileOrbitMath.Sample(segment, 100);
        Assert.Equal(x, result.X, 9);
        Assert.Equal(y, result.Y, 9);
        Assert.Equal(6.2, result.Direction, 10);
        Assert.Equal(1.27, result.SpeedKmS, 10);
    }

    [Fact]
    public void Hold_phase_uses_ten_percent_exit_hysteresis()
    {
        var target = new ObjectMotionSnapshot("p", 0, 0, 0, 0);
        var ship = new ObjectMotionSnapshot("e", 100, 0, 1, 180);
        var held = HostileOrbitMath.Plan(ship, target, 0, "e", "w", 10, 2, 0.2, 30);
        Assert.Equal(HostileCombatPhase.OrbitHold, held.Phase);
        Assert.Equal(HostileCombatPhase.OrbitHold, HostileOrbitMath.Plan(ship with { X = 109 }, target, 100, "e", "w", 10, 2, 0.2, 30, held).Phase);
        Assert.Equal(HostileCombatPhase.OrbitEntry, HostileOrbitMath.Plan(ship with { X = 111 }, target, 200, "e", "w", 10, 2, 0.2, 30, held).Phase);
        Assert.Equal(HostileCombatPhase.OrbitEntry, HostileOrbitMath.Plan(ship with { X = 106 }, target, 300, "e", "w", 10, 2, 0.2, 30).Phase);
    }

    [Fact]
    public void Unavailable_turn_authority_falls_back_to_bounded_pursuit()
    {
        var ship = new ObjectMotionSnapshot("e", 0, 0, 1, 359);
        var target = new ObjectMotionSnapshot("p", 100, 100, 0, 0);
        var plan = HostileOrbitMath.Plan(ship, target, 0, "e", "w", 10, 2, 0.2, 0);
        Assert.Equal(HostileCombatPhase.Pursuit, plan.Phase);
        Assert.Equal(0, plan.Segment.TurnRateDegPerSec);
        Assert.Equal(0.2, plan.Segment.AccelerationKmS2);
    }
}
