using DeepSpaceSaga.Engine;

namespace DeepSpaceSaga.Engine.Tests;

public class SmokeTests
{
    [Fact]
    public void SimulationEngine_is_instantiable()
    {
        var engine = new SimulationEngine();
        Assert.NotNull(engine);
    }
}
