using DeepSpaceSaga.Contracts;
namespace DeepSpaceSaga.Motion.Tests;

public class CountermeasureGuidanceTests
{
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
        var op = new WeaponOperatorSnapshot("c", "Crew", WeaponSkillType.CountermeasureDefense, 50, 30, 30);
        var state = new ObjectMotionSnapshot("pr", 0, 0, 0, 12, Countermeasure: new("ship", "module", "torpedo", CountermeasurePhase.Guiding, route, 0, 500, new(op, 30)));
        var predictor = new LinearMotionPredictor();
        var a = predictor.Predict(state, 1500);
        var b = predictor.Predict(predictor.Predict(state, 600), 900);
        Assert.Equal(a, b);
        Assert.False(LinearMotionPredictor.IsLinear(state));
    }
}
