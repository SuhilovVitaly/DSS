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

    [Fact]
    public void Priority_uses_command_identity_even_when_caption_changes()
    {
        var weapon = new WeaponPanelInstance("ship", "launcher", "Localized title");
        Assert.Equal(0, CommandsPanel.ExpansionPriority(new("Localized title", [CombatCommandTypes.Launch], weapon)));
        Assert.Equal(1, CommandsPanel.ExpansionPriority(new("Anything", [DefenseCommandTypes.Launch], weapon)));
    }

    [Fact]
    public async Task Actual_render_opens_a_weapon_before_navigation_in_small_viewport()
    {
        await using var f = await PointCombatUiFixture.Create(1280, 720, 1.5f);
        var rows = f.Screen.CommandsPanel.CommandPanelRows;
        Assert.Contains(rows, r => r.Weapon?.ModuleId == PointCombatUiFixture.Torpedo && r.Opened);
        Assert.DoesNotContain(rows, r => r.Name == "Navigation" && r.Opened);
    }

    [Fact]
    public async Task Reload_tooltip_and_countdown_keep_double_speed_while_paused()
    {
        await using var f = await PointCombatUiFixture.Create();
        Assert.Equal(CommandResultStatus.Executed, (await f.Launch(x: 300)).Status);
        await f.Connection.SetSimulationSpeedAsync(SimulationSpeed.SpeedDouble);
        await f.Wait(() => f.Presented.CurrentSpeed == SimulationSpeed.SpeedDouble);
        await f.Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed0);
        await f.Wait(() => f.Presented.CurrentSpeed == SimulationSpeed.Speed0);
        var row = Assert.Single(f.Screen.CommandsPanel.CommandPanelRows, r => r.Weapon?.ModuleId == PointCombatUiFixture.Torpedo);
        Assert.Equal(20, row.RemainingReloadRealSeconds, 3);
        f.Screen.OnMouseMove(row.ReloadRingRect.MidX, row.ReloadRingRect.MidY);
        f.Render();
        Assert.Contains("x2", f.Screen.CommandsPanel.CurrentTimeTooltip);
        Assert.Contains("10", f.Screen.CommandsPanel.CurrentTimeTooltip);
    }
}
