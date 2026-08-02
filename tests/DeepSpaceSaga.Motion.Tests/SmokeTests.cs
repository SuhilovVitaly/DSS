using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Motion.Tests;

public class SmokeTests
{
    [Fact]
    public void LinearMotionPredictor_is_instantiable()
    {
        var predictor = new LinearMotionPredictor();
        Assert.NotNull(predictor);
    }
}
