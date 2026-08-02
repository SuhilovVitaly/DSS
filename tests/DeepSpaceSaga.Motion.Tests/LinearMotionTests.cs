using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Motion.Tests;

public class LinearMotionTests
{
    [Fact]
    public void Predict_moves_right_at_90_degrees()
    {
        var predictor = new LinearMotionPredictor();
        // 90° = right, 5 km/s * 1 sec = 50 world units
        var state = new ObjectMotionSnapshot("o1", X: 100, Y: 0, SpeedKmS: 5, Direction: 90);

        var predicted = predictor.Predict(state, elapsedMs: 1000);

        Assert.Equal(150.0, predicted.X, precision: 6); // 100 + 50
        Assert.Equal(0.0, predicted.Y, precision: 6);
    }

    [Fact]
    public void Predict_moves_up_at_0_degrees()
    {
        var predictor = new LinearMotionPredictor();
        // 0° = up (negative Y), 10 km/s * 1 sec = 100 world units
        var state = new ObjectMotionSnapshot("o1", X: 0, Y: 200, SpeedKmS: 10, Direction: 0);

        var predicted = predictor.Predict(state, elapsedMs: 1000);

        Assert.Equal(0.0, predicted.X, precision: 6);
        Assert.Equal(100.0, predicted.Y, precision: 6); // 200 - 100
    }

    [Fact]
    public void Predict_moves_down_at_180_degrees()
    {
        var predictor = new LinearMotionPredictor();
        var state = new ObjectMotionSnapshot("o1", X: 0, Y: 100, SpeedKmS: 10, Direction: 180);

        var predicted = predictor.Predict(state, elapsedMs: 1000);

        Assert.Equal(0.0, predicted.X, precision: 6);
        Assert.Equal(200.0, predicted.Y, precision: 6); // 100 + 100
    }

    [Fact]
    public void Predict_with_zero_elapsed_returns_same_position()
    {
        var predictor = new LinearMotionPredictor();
        var state = new ObjectMotionSnapshot("o1", X: 42, Y: 73, SpeedKmS: 100, Direction: 45);

        var predicted = predictor.Predict(state, elapsedMs: 0);

        Assert.Equal(42.0, predicted.X, precision: 6);
        Assert.Equal(73.0, predicted.Y, precision: 6);
    }

    [Fact]
    public void Predict_half_second_produces_half_distance()
    {
        var predictor = new LinearMotionPredictor();
        // 90° = right, 10 km/s * 0.5 sec = 50 world units
        var state = new ObjectMotionSnapshot("o1", X: 0, Y: 0, SpeedKmS: 10, Direction: 90);

        var predicted = predictor.Predict(state, elapsedMs: 500);

        Assert.Equal(50.0, predicted.X, precision: 6);
        Assert.Equal(0.0, predicted.Y, precision: 6);
    }
}
