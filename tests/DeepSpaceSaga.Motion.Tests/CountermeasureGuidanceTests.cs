using DeepSpaceSaga.Contracts;
namespace DeepSpaceSaga.Motion.Tests;

public class CountermeasureGuidanceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(274.48)]
    public void Launcher_aims_at_future_encounter_independently_of_carrier_heading(double heading)
    {
        var target = new TorpedoRoute(0, 1, TorpedoRoutePhase.Turning, true,
            [new(200, -600, 210, 10, -90, 300), new(189.4, -572.2, 183, 10, 0, 10000)]);
        var carrier = new ObjectMotionSnapshot("ship", 0, 0, SpeedKmS: 0, Direction: heading);
        var route = Assert.IsType<TorpedoRoute>(CountermeasureGuidanceMath.PlanLaunch(carrier, target, 0, 10000, 20));
        double duration = route.Segments.Sum(s => s.DurationMs);
        Assert.InRange(route.Segments.Sum(s => Math.Abs(s.AngularVelocityDegPerSec) * s.DurationMs / 1000), 0, .01);
        var missile = TorpedoGuidanceMath.PredictPose(route, duration);
        var torpedo = TorpedoGuidanceMath.PredictPose(target, duration);
        Assert.InRange(Math.Abs(missile.X - torpedo.X) + Math.Abs(missile.Y - torpedo.Y), 0, 1e-5);
        var manual = CountermeasureGuidanceMath.PlanManualLaunch(carrier, target, 0, 10000, 20);
        Assert.NotNull(manual.PredictedEncounterMotionTimeMs);
        Assert.Equal(route.Segments[0].Direction, manual.Route.Segments[0].Direction, 6);
        Assert.Equal(heading, carrier.Direction);
    }

    [Fact]
    public void Recorded_loop_case_launches_without_a_circle_but_airborne_replan_keeps_heading()
    {
        var target = new TorpedoRoute(36300, 1, TorpedoRoutePhase.Turning, true,
            [new(-223.96278388134763, -733.0150671071071, 216.31652200185727, 10, -90, 1.8905249984358525),
             new(-224.07452303696462, -732.8625706070376, 216.146374751998, 10, 0, 4823.767837277274)]);
        var carrier = new ObjectMotionSnapshot("ship", -457.8908808105749, -362.08115542540037, SpeedKmS: 0, Direction: 222.56);
        var launch = CountermeasureGuidanceMath.PlanManualLaunch(carrier, target, 36544, 29784, 20);
        Assert.NotNull(launch.PredictedEncounterMotionTimeMs);
        Assert.InRange(launch.Route.Segments.Sum(s => Math.Abs(s.AngularVelocityDegPerSec) * s.DurationMs / 1000), 0, .01);
        Assert.InRange(launch.PredictedEncounterMotionTimeMs!.Value - 36544, 0, 3000);
        var airborne = CountermeasureGuidanceMath.PlanManual(carrier, target, 36544, 29784, 20);
        Assert.Equal(carrier.Direction, airborne.Route.Segments[0].Direction);
        Assert.All(airborne.Route.Segments, s => Assert.InRange(Math.Abs(s.AngularVelocityDegPerSec), 0, 90));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(45)]
    [InlineData(-45)]
    public void Curved_torpedo_path_has_consistent_intercept(double turn)
    {
        var target = new TorpedoRoute(0, 1, TorpedoRoutePhase.Turning, true, [new(0, -600, 180, 3, turn, 1000)]);
        var defender = new ObjectMotionSnapshot("ship", 0, 0, SpeedKmS: 50, Direction: 90);
        var route = CountermeasureGuidanceMath.Plan(defender, target, 0, 20000);
        Assert.NotNull(route);
        Assert.Equal(0, route.Segments[0].X);
        Assert.Equal(0, route.Segments[0].Y);
        Assert.Equal(90, route.Segments[0].Direction);
        Assert.All(route.Segments, s => { Assert.Equal(12, s.SpeedKmS); Assert.InRange(Math.Abs(s.AngularVelocityDegPerSec), 0, 90); });
        double duration = route.Segments.Sum(s => s.DurationMs);
        var a = TorpedoGuidanceMath.PredictPose(route, duration);
        var b = TorpedoGuidanceMath.PredictPose(target, duration);
        Assert.InRange(Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y), 0, 1e-5);
        Assert.NotNull(CountermeasureGuidanceMath.Plan(defender, target, 0, duration - .1));
        Assert.Null(CountermeasureGuidanceMath.Plan(defender, target, 0, 1));
    }
    [Fact]
    public void Prediction_is_partition_invariant()
    {
        var route = new TorpedoRoute(0, 1, TorpedoRoutePhase.Turning, true, [new(0, 0, 0, 12, 90, 1000)]);
        var op = new WeaponOperatorSnapshot("c", "Crew", WeaponSkillType.CountermeasureDefense, 50, 55, 55);
        var state = new ObjectMotionSnapshot("pr", 0, 0, 0, 12, Countermeasure: new("ship", "module", "torpedo", CountermeasurePhase.Guiding, route, 0, 500, new(op, 5)));
        var predictor = new LinearMotionPredictor();
        var a = predictor.Predict(state, 1500);
        var b = predictor.Predict(predictor.Predict(state, 600), 900);
        Assert.Equal(a, b);
        Assert.False(LinearMotionPredictor.IsLinear(state));
    }
}
