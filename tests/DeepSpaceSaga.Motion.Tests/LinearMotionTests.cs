using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Motion.Tests;

public class LinearMotionTests
{
    [Fact]
    public void Predict_moves_object_along_direction()
    {
        var predictor = new LinearMotionPredictor();
        var state = new ObjectMotionSnapshot("o1", X: 100, Y: 0, Speed: 50, Direction: 0); // moving right

        var predicted = predictor.Predict(state, elapsedMs: 500);

        // speed 50 units/sec * 0.5 sec = 25 units to the right
        Assert.Equal(125.0, predicted.X, precision: 6);
        Assert.Equal(0.0, predicted.Y, precision: 6);
    }

    [Fact]
    public void Predict_moves_up()
    {
        var predictor = new LinearMotionPredictor();
        var state = new ObjectMotionSnapshot("o1", X: 0, Y: 100, Speed: 100, Direction: Math.PI / 2); // moving up

        var predicted = predictor.Predict(state, elapsedMs: 1000);

        Assert.Equal(0.0, predicted.X, precision: 6);
        Assert.Equal(200.0, predicted.Y, precision: 6); // 100 units/sec * 1 sec
    }

    [Fact]
    public void Predict_with_zero_elapsed_returns_same_position()
    {
        var predictor = new LinearMotionPredictor();
        var state = new ObjectMotionSnapshot("o1", X: 42, Y: 73, Speed: 100, Direction: 1.0);

        var predicted = predictor.Predict(state, elapsedMs: 0);

        Assert.Equal(42.0, predicted.X, precision: 6);
        Assert.Equal(73.0, predicted.Y, precision: 6);
    }
}
