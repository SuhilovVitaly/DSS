using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public sealed class ModulePanelEngineTests
{
    [Theory]
    [InlineData(3000, "13")]
    [InlineData(3200, "12")]
    public async Task Actual_engine_panel_shows_current_speed_limits_and_real_durations(int speed, string seconds)
    {
        await using var f = await PointCombatUiFixture.Create(configure: o => o.ObjectId != PointCombatUiFixture.Player ? o : o with
        { SpeedMps = speed, MovementType = "Linear" });
        var row = Assert.Single(f.Screen.CommandsPanel.CommandPanelRows, r => r.Name == "Engine");
        Assert.Contains(row.DetailLines, l => l.Contains("5.0") || l.Contains("5,0"));
        Assert.Contains(row.DetailLines, l => l.Contains(Localization.Combat("Panel.AccelerationTime") + ": " + seconds + " "));
        Assert.Contains(row.DetailLines, l => l.Contains(Localization.Combat("Panel.TurnTime") + ": 18 "));
        Assert.False(row.DetailsMuted);
        Assert.Contains("x1", row.TimeTooltip);
        if (speed == 3000)
        {
            await f.Connection.SetSimulationSpeedAsync(SimulationSpeed.SpeedDouble);
            await f.Wait(() => f.Presented.CurrentSpeed == SimulationSpeed.SpeedDouble);
            await f.Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed0);
            await f.Wait(() => f.Presented.CurrentSpeed == SimulationSpeed.Speed0);
            row = Assert.Single(f.Screen.CommandsPanel.CommandPanelRows, r => r.Name == "Engine");
            Assert.Contains(row.DetailLines, l => l.Replace(',', '.').Contains(Localization.Combat("Panel.AccelerationTime") + ": 6.7 "));
            Assert.Contains(row.DetailLines, l => l.Replace(',', '.').Contains(Localization.Combat("Panel.TurnTime") + ": 9.0 "));
            Assert.Contains("x2", row.TimeTooltip);
        }
    }

    [Fact]
    public async Task New_snapshot_updates_limits_and_zero_rates_never_invent_a_duration()
    {
        await using var f = await PointCombatUiFixture.Create();
        var snapshot = f.Presented;
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        snapshot = snapshot with
        {
            SnapshotSequence = snapshot.SnapshotSequence + 1,
            InstalledModules = [.. snapshot.InstalledModules.Select(m => m.EngineMotion is null ? m : m with
            { EngineMotion = new(7, 0, 0), DisabledUntilMotionTimeMs = 5000 })]
        };
        buffer.Update(snapshot); screen.Render(canvas, 1920, 1080);
        var row = Assert.Single(screen.CommandsPanel.CommandPanelRows, r => r.Name == "Engine");
        Assert.True(row.DetailsMuted);
        Assert.Contains(row.DetailLines, l => l.Contains("7.0") || l.Contains("7,0"));
        Assert.Contains(row.DetailLines, l => l.Contains(Localization.Combat("Panel.AccelerationTime") + ": " + Localization.Combat("Panel.UnknownValue")));
        Assert.Contains(row.DetailLines, l => l.Contains(Localization.Combat("Panel.Disabled") + " 20 "));
        Assert.DoesNotContain(row.DetailLines, l => l.Contains("NaN") || l.Contains("Infinity"));
        screen.OnDeactivated();
    }
}
