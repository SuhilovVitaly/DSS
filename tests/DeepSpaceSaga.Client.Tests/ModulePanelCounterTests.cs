using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public sealed class ModulePanelCounterTests
{
    [Fact]
    public async Task Counter_panel_has_reference_sigma_reload_and_no_core()
    {
        await using var f = await PointCombatUiFixture.Create(accuracySkill: 20);
        f.Screen.OnKeyDown(Key.E); f.Render();
        var row = Assert.Single(f.Screen.CommandsPanel.CommandPanelRows, r => r.Weapon?.ModuleId == "MOD-PLAYER-COUNTERMEASURE-01");
        Assert.Equal("wide", f.Screen.PointAim!.ProfileId);
        Assert.Equal(30, row.RangeKm);
        Assert.Contains("0.67", row.OperatorText!.Replace(',', '.'));
        Assert.Contains("10", row.OperatorText);
        var profiles = row.DetailLines.Where(l => l.Contains('\t')).Select(l => l.Split('\t')).ToArray();
        Assert.Equal(2, profiles.Length);
        Assert.All(profiles, p => Assert.Empty(p[4]));
        Assert.Contains(row.DetailLines, l => l.Contains(Localization.Combat("Panel.CounterStats")) && l.Contains("50"));
        await f.Connection.SetSimulationSpeedAsync(SimulationSpeed.SpeedDouble);
        await f.Wait(() => f.Presented.CurrentSpeed == SimulationSpeed.SpeedDouble);
        row = Assert.Single(f.Screen.CommandsPanel.CommandPanelRows, r => r.Weapon?.ModuleId == "MOD-PLAYER-COUNTERMEASURE-01");
        Assert.Contains(row.DetailLines, l => l.Contains(Localization.Combat("Panel.Reload") + " 25 "));
    }

    [Fact]
    public async Task Profiles_share_live_observed_intercept_point_and_mark_missed_arrival()
    {
        await using var f = await PointCombatUiFixture.Create(hostileShots: 1);
        var foreign = Assert.Single(f.Presented.Objects, o => o.ObservedProjectile is { Kind: ProjectileKind.Torpedo });
        var observed = foreign.ObservedProjectile!;
        double targetX = foreign.X + observed.DirectionX * 80, targetY = foreign.Y + observed.DirectionY * 80;
        f.Expose(targetX, targetY);
        f.Screen.OnKeyDown(Key.E);
        var cursor = f.Pixel(targetX, targetY); f.Screen.OnMouseMove(cursor.X, cursor.Y); f.Render();
        var preview = Assert.IsType<DeepSpaceSaga.Client.UI.Screens.GameSession.GameSessionScreen.CounterAimPreview>(f.Screen.CurrentCounterAimPreview);
        Assert.NotNull(preview.SnappedObjectId);
        Assert.Equal(targetX, preview.X, 3); Assert.Equal(targetY, preview.Y, 3);
        var row = Assert.Single(f.Screen.CommandsPanel.CommandPanelRows, r => r.Weapon?.ModuleId == "MOD-PLAYER-COUNTERMEASURE-01");
        var columns = row.DetailLines.Where(l => l.Contains('\t')).Select(l => l.Split('\t')).ToArray();
        Assert.Contains("5.9", columns[0][5].Replace(',', '.'));
        Assert.Equal(Localization.Combat("Panel.Unreachable"), columns[1][5]);
    }

    [Fact]
    public async Task Missing_observed_track_does_not_invent_unreachable_forecast()
    {
        await using var f = await PointCombatUiFixture.Create();
        f.Expose(150); f.Screen.OnKeyDown(Key.E);
        var cursor = f.Pixel(150); f.Screen.OnMouseMove(cursor.X, cursor.Y); f.Render();
        Assert.Null(f.Screen.CurrentCounterAimPreview!.SnappedObjectId);
        var row = Assert.Single(f.Screen.CommandsPanel.CommandPanelRows, r => r.Weapon?.ModuleId == "MOD-PLAYER-COUNTERMEASURE-01");
        Assert.DoesNotContain(row.DetailLines, l => l.Contains(Localization.Combat("Panel.Unreachable")));
    }
}
