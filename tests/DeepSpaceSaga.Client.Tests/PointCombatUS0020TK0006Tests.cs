using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public class PointCombatUS0020TK0006Tests
{

    [Fact]
    public void Peace_levels_and_loaded_new_speed_are_available()
    {
        foreach (var speed in new[] { SimulationSpeed.Speed1, SimulationSpeed.SpeedDouble, SimulationSpeed.SpeedTen })
        {
            var buffer = new SnapshotBuffer(() => 0);
            buffer.Update(new(1, 0, speed, [new("player", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip)], "player"));
            var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
            Assert.Contains(speed, screen.SpeedOptions);
            Assert.Contains(SimulationSpeed.Speed3, screen.SpeedOptions); Assert.Contains(SimulationSpeed.Speed4, screen.SpeedOptions);
            screen.OnDeactivated();
        }
    }
}
