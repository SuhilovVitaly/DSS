using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public class PointCombatUS0020TK0006Tests
{
    [Theory]
    [InlineData(SimulationSpeed.Speed1, 40)]
    [InlineData(SimulationSpeed.SpeedDouble, 20)]
    [InlineData(SimulationSpeed.Speed2, 8)]
    [InlineData(SimulationSpeed.SpeedTen, 4)]
    public async Task Combat_speed_ui_and_eta(SimulationSpeed speed, double expectedSeconds)
    {
        await using var world = new PointCombatSessionFixture();
        var scene = world.Engine.CaptureSnapshot();
        scene = scene with
        {
            CurrentSpeed = speed,
            Encounter = new(CombatEncounterState.Active, 0, null, new()),
            InstalledModules = [.. scene.InstalledModules.Select(m => m.LauncherCombat is not { } launcher ? m :
                m with { LauncherCombat = launcher with { ReadyAtMotionTimeMs = 10000, ReloadDurationMs = 10000 } })]
        };
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(scene);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal(new[] { SimulationSpeed.Speed0, SimulationSpeed.Speed1, SimulationSpeed.SpeedDouble, SimulationSpeed.Speed2, SimulationSpeed.SpeedTen }, screen.SpeedOptions);
        Assert.Equal(speed, buffer.CurrentSpeed);
        Assert.Equal(expectedSeconds, Assert.Single(screen.CommandsPanel.CommandPanelRows,
            r => r.Weapon?.ModuleId == PointCombatSessionFixture.Torpedo).RemainingReloadRealSeconds);
        screen.OnKeyDown(Key.Space); screen.OnKeyUp(Key.Space);
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);
        screen.OnKeyDown(Key.Space); Assert.Equal(speed, buffer.CurrentSpeed);
        screen.OnDeactivated();
    }

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
