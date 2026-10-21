using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class PointCombatUS0020TK0006Tests
{
    [Theory]
    [InlineData(SimulationSpeed.SpeedDouble, .5)]
    [InlineData(SimulationSpeed.SpeedTen, 2.5)]
    public void New_speeds_preserve_ids_multipliers_and_roundtrip(SimulationSpeed speed, double multiplier)
    {
        Assert.Equal(multiplier, speed.MotionTimeMultiplier());
        Assert.True(speed.IsAllowedInCombat());
        Assert.Equal(speed, JsonSerializer.Deserialize<SimulationSpeed>(JsonSerializer.Serialize(speed)));
        Assert.Equal(5, (int)SimulationSpeed.Speed2);
        Assert.Equal(20, (int)SimulationSpeed.Speed3);
        Assert.Equal(100, (int)SimulationSpeed.Speed4);
    }
}
