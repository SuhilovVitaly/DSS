using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public sealed class ModulePanelTimeTests
{
    [Theory]
    [InlineData(SimulationSpeed.Speed1, 40)]
    [InlineData(SimulationSpeed.SpeedDouble, 20)]
    [InlineData(SimulationSpeed.Speed2, 8)]
    [InlineData(SimulationSpeed.SpeedTen, 4)]
    public void Durations_use_real_seconds(SimulationSpeed speed, double expected) =>
        Assert.Equal(expected, PanelTime.RealSeconds(10, speed));

    [Fact]
    public void Pause_preserves_the_last_active_speed()
    {
        var speed = PanelTime.EffectiveSpeed(SimulationSpeed.Speed0, SimulationSpeed.SpeedDouble);
        Assert.Equal(20, PanelTime.RealSeconds(10, speed));
        Assert.Equal(SimulationSpeed.Speed1, PanelTime.EffectiveSpeed(SimulationSpeed.Speed0, SimulationSpeed.Speed0));
        Assert.Equal(0, PanelTime.RealSeconds(-1, speed));
    }
}
