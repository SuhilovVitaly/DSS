using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion.Tests;

public class ManualCountermeasureGuidanceTests
{
    private static readonly ObjectMotionSnapshot Defender = new("ship", 0, 0, 99, 0);
    private static TorpedoRoute Target(double distance = 1000, double speed = 3, double turn = 0) =>
        new(0, 1, TorpedoRoutePhase.Turning, false, [new(0, -distance, 0, speed, turn, 1000)]);

    [Fact]
    public void Manual_unreachable_target_still_has_bounded_flight_and_unknown_eta()
    {
        var plan = CountermeasureGuidanceMath.PlanManual(Defender, Target(speed: 20), 0, 60000);
        Assert.Null(plan.PredictedEncounterMotionTimeMs);
        Assert.False(plan.Route.HasIntercept);
        var segment = Assert.Single(plan.Route.Segments);
        Assert.Equal(12, segment.SpeedKmS);
        Assert.InRange(Math.Abs(segment.AngularVelocityDegPerSec), 0, 90);
        Assert.Equal(100, segment.DurationMs);
        var pose = TorpedoGuidanceMath.PredictPose(plan.Route, 100);
        Assert.Equal(-12, pose.Y, 8);
    }

    [Fact]
    public void Manual_horizon_uses_captured_remaining_lifetime_and_not_hull_deadline()
    {
        var target = Target();
        var shortFlight = CountermeasureGuidanceMath.PlanManual(Defender, target, 0, 1000);
        var longFlight = CountermeasureGuidanceMath.PlanManual(Defender, target, 0, 60000);
        Assert.Null(shortFlight.PredictedEncounterMotionTimeMs);
        Assert.NotNull(longFlight.PredictedEncounterMotionTimeMs);
        Assert.InRange(longFlight.PredictedEncounterMotionTimeMs.Value, 10000, 12000);
        Assert.Null(CountermeasureGuidanceMath.Plan(Defender, target, 0, 1000));
        Assert.Null(CountermeasureGuidanceMath.Plan(Defender, target, 0, 60000, expiresAtMotionTimeMs: 1000));
        Assert.NotNull(CountermeasureGuidanceMath.Plan(Defender, target, 0, 60000, expiresAtMotionTimeMs: 20000));
        var remaining = CountermeasureGuidanceMath.PlanManual(Defender, target, 59000, 1000);
        Assert.Null(remaining.PredictedEncounterMotionTimeMs);
    }

    [Theory]
    [InlineData(-45)]
    [InlineData(45)]
    public void Manual_plan_uses_curved_target_and_actual_contact(double turn)
    {
        var target = Target(turn: turn);
        var plan = CountermeasureGuidanceMath.PlanManual(Defender, target, 0, 60000, 12, 90);
        Assert.NotNull(plan.PredictedEncounterMotionTimeMs);
        double time = plan.PredictedEncounterMotionTimeMs.Value;
        var a = TorpedoGuidanceMath.PredictPose(plan.Route, time);
        var b = TorpedoGuidanceMath.PredictPose(target, time);
        Assert.InRange(Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)), 4.99999, 5.00001);
        Assert.All(plan.Route.Segments, s => Assert.InRange(Math.Abs(s.AngularVelocityDegPerSec), 0, 90));
    }

    [Fact]
    public void Earlier_obstacle_contact_suppresses_false_encounter_cross()
    {
        var plan = CountermeasureGuidanceMath.PlanManual(Defender, Target(), 0, 60000, firstTargetContactMotionTimeMs: 1000);
        Assert.Null(plan.PredictedEncounterMotionTimeMs);
        Assert.False(plan.Route.HasIntercept);
        Assert.NotEmpty(plan.Route.Segments);
    }

    [Fact]
    public void Short_remaining_fallback_segment_does_not_extend_lifetime()
    {
        var plan = CountermeasureGuidanceMath.PlanManual(Defender with { Direction = 359 }, Target(speed: 20), 0, 7, 8, 45);
        var segment = Assert.Single(plan.Route.Segments);
        Assert.Equal(7, segment.DurationMs);
        Assert.Equal(8, segment.SpeedKmS);
        Assert.InRange(Math.Abs(segment.AngularVelocityDegPerSec), 0, 45);
    }
}
