using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Motion.Tests;

public class SmokeTests
{
    [Fact]
    public void MotionPredictor_type_exists()
    {
        var type = typeof(MotionPredictor);
        Assert.NotNull(type);
        Assert.True(type.IsAbstract); // static class → abstract sealed
    }
}
