using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Motion.Tests;

public class TurnPositionSamplingTests
{
    [Theory]
    [InlineData(1, 250, 250)]
    [InlineData(-7, 333, 137)]
    [InlineData(90, 1000, 0)]
    [InlineData(3, 1000, 1200)]
    [InlineData(1, 17, 3)]
    public void Batch_matches_independent_predictions_at_every_sample(int degrees, long interval, long phase)
    {
        var state = new ObjectMotionSnapshot("ship", 12345, -9876, 4, 359,
            ShipEngineCommandTypes.TurnRightUntilCancel, degrees, phase, interval);
        var points = new (double X, double Y)[801];
        Assert.True(LinearMotionPredictor.TryPredictTurnPositions(state, 250, points, out var terminal));
        var predictor = new LinearMotionPredictor();
        for (int i = 0; i < points.Length; i++)
        {
            var expected = predictor.Predict(state, i * 250L);
            Assert.InRange(Math.Abs(points[i].X - expected.X), 0, 1e-7);
            Assert.InRange(Math.Abs(points[i].Y - expected.Y), 0, 1e-7);
        }
        var end = predictor.Predict(state, 200000);
        Assert.Equal(end.Direction, terminal.Direction);
        Assert.Equal(end.TurnStepRemainingMs, terminal.TurnStepRemainingMs);
        Assert.Equal(12345, state.X);
    }

    [Theory]
    [InlineData(NavigationComputerCommandTypes.Approach)]
    [InlineData(ShipEngineCommandTypes.Orbit)]
    public void Batch_does_not_replace_navigation_prediction(string command)
    {
        var state = new ObjectMotionSnapshot("ship", 0, 0, 4, 0, command, 1, 250, 250);
        var points = new (double X, double Y)[] { (42, 43) };
        Assert.False(LinearMotionPredictor.TryPredictTurnPositions(state, 250, points, out var terminal));
        Assert.Same(state, terminal);
        Assert.Equal((42d, 43d), points[0]);
    }

    [Fact]
    public void Batch_rejects_invalid_interval_and_handles_empty_output()
    {
        var state = new ObjectMotionSnapshot("ship", 0, 0, 4, 0, null, 1, 250, 250);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LinearMotionPredictor.TryPredictTurnPositions(state, 0, new (double, double)[1], out _));
        Assert.False(LinearMotionPredictor.TryPredictTurnPositions(state, 250, Span<(double, double)>.Empty, out _));
    }
}
