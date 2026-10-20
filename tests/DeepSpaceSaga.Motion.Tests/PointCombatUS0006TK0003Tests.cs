using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Motion.Tests;

public sealed class PointCombatUS0006TK0003Tests
{
    [Theory]
    [InlineData(90, 20, -20, 90)]
    [InlineData(-90, -20, -20, 270)]
    public void Held_turn_follows_analytic_quarter_circle(double rate, double x, double y, double direction)
    {
        var state = new ObjectMotionSnapshot("ship", 0, 0, Math.PI, 0, TurnHoldRateDegPerSec: rate);
        var predictor = new LinearMotionPredictor();
        var result = predictor.Predict(state, 1000);
        Assert.Equal(x, result.X, 10);
        Assert.Equal(y, result.Y, 10);
        Assert.Equal(direction, result.Direction, 10);
        var split = CurrentManeuverForecast.Sample(CurrentManeuverForecast.Sample(state, 313.5), 686.5);
        Assert.Equal(result.X, split.X, 10);
        Assert.Equal(result.Y, split.Y, 10);
        Assert.Equal(result.Direction, split.Direction, 10);
    }

    [Fact]
    public void Invalid_held_turn_is_rejected()
    {
        var state = new ObjectMotionSnapshot("ship", 0, 0, 1, 0, TurnHoldRateDegPerSec: double.NaN);
        Assert.Throws<ArgumentException>(() => CurrentManeuverForecast.Sample(state, 1));
    }
}
