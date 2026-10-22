using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public sealed class ModulePanelProfileTests
{
    [Fact]
    public async Task Reload_ring_number_and_status_remain_visible_on_button_hover()
    {
        await using var f = await PointCombatUiFixture.Create();
        await f.Launch(x: 300);
        await f.AdvanceTo(3000);
        var panel = f.Screen.CommandsPanel;
        var row = Assert.Single(panel.CommandPanelRows, r => r.Weapon?.ModuleId == PointCombatUiFixture.Torpedo);
        Assert.Equal(28, row.RemainingReloadRealSeconds, 3);
        Assert.Equal("28 " + Localization.Combat("Panel.Seconds"), row.ReloadTimerText);
        var button = row.Buttons[0];
        f.Screen.OnMouseMove(button.Rect.MidX, button.Rect.MidY); f.Render();
        row = Assert.Single(panel.CommandPanelRows, r => r.Weapon?.ModuleId == PointCombatUiFixture.Torpedo);
        Assert.Contains(Localization.Combat("Reloading"), row.StatusBarText);
        Assert.Contains("28", row.StatusBarText);
        Assert.NotNull(row.HoverText);
        Assert.Equal("28 " + Localization.Combat("Panel.Seconds"), row.ReloadTimerText);
        await f.AdvanceTo(8950);
        row = Assert.Single(panel.CommandPanelRows, r => r.Weapon?.ModuleId == PointCombatUiFixture.Torpedo);
        Assert.Equal("4.2 " + Localization.Combat("Panel.Seconds"), row.ReloadTimerText!.Replace(',', '.'));
        await f.AdvanceTo(10000);
        row = Assert.Single(panel.CommandPanelRows, r => r.Weapon?.ModuleId == PointCombatUiFixture.Torpedo);
        Assert.Null(row.ReloadTimerText);
    }

    [Fact]
    public async Task Snapshot_statistics_and_reference_sigma_follow_game_speed()
    {
        await using var f = await PointCombatUiFixture.Create(accuracySkill: 20);
        var row = Assert.Single(f.Screen.CommandsPanel.CommandPanelRows, r => r.Weapon?.ModuleId == PointCombatUiFixture.Torpedo);
        Assert.Equal(70, row.RangeKm);
        Assert.Contains("0.42", row.OperatorText!.Replace(',', '.'));
        Assert.Contains(row.DetailLines, l => l.Contains("200") && l.Contains("20") && l.Contains("40"));
        await f.Connection.SetSimulationSpeedAsync(SimulationSpeed.SpeedDouble);
        await f.Wait(() => f.Presented.CurrentSpeed == SimulationSpeed.SpeedDouble);
        row = Assert.Single(f.Screen.CommandsPanel.CommandPanelRows, r => r.Weapon?.ModuleId == PointCombatUiFixture.Torpedo);
        Assert.Contains(row.DetailLines, l => l.Contains(Localization.Combat("Panel.Reload") + " 20 "));
        Assert.Contains("x2", row.TimeTooltip);
        Assert.Contains("10", row.TimeTooltip);
    }

    [Fact]
    public async Task Render_keeps_fractional_penetration_and_does_not_invent_an_operator_sigma()
    {
        await using var f = await PointCombatUiFixture.Create();
        var snapshot = f.Presented with
        {
            InstalledModules = [.. f.Presented.InstalledModules.Select(m => m.LauncherCombat is not { } launcher ? m : m with
            { Operator = null, LauncherCombat = launcher with { Operator = null, EffectiveScatterPercent = null, ArmorPiercingPercent = 20.125m } })]
        };
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new DeepSpaceSaga.Motion.LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SkiaSharp.SKBitmap(1920, 1080);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        var row = Assert.Single(screen.CommandsPanel.CommandPanelRows, r => r.Weapon?.ModuleId == PointCombatUiFixture.Torpedo);
        Assert.Contains(row.DetailLines, l => l.Replace(',', '.').Contains("20.125"));
        Assert.Equal(Localization.Combat("Panel.NoOperator"), row.OperatorText);
        screen.OnDeactivated();
    }

    [Fact]
    public async Task Profile_rows_use_current_binding_order_outside_aim()
    {
        await using var f = await PointCombatUiFixture.Create(inputs: CombatInputSettings.Default with
        { Profiles = [Key.Number3, Key.Number1, Key.Number2] });
        var row = Assert.Single(f.Screen.CommandsPanel.CommandPanelRows, r => r.Weapon?.ModuleId == PointCombatUiFixture.Torpedo);
        var keys = row.DetailLines.Where(l => l.Contains('\t')).Select(l => l.Split('\t')[0]).ToArray();
        Assert.Equal("3", keys[0]);
        Assert.Equal("1" + Localization.Combat("Panel.Selected"), keys[1]);
        Assert.Equal("2", keys[2]);
    }

    [Fact]
    public async Task Actual_profile_rows_show_keys_core_cursor_times_and_sigma()
    {
        await using var f = await PointCombatUiFixture.Create(accuracySkill: 20);
        f.Expose(250);
        f.Screen.OnKeyDown(Key.Q);
        var point = f.Pixel(250);
        f.Screen.OnMouseMove(point.X, point.Y);
        f.Render();
        var row = Assert.Single(f.Screen.CommandsPanel.CommandPanelRows, r => r.Weapon?.ModuleId == PointCombatUiFixture.Torpedo);
        var columns = row.DetailLines.Where(l => l.Contains('\t')).Select(l => l.Split('\t')).ToArray();
        Assert.Equal(3, columns.Length);
        Assert.Equal("1", columns[0][0]);
        Assert.Contains(Localization.Combat("Panel.Selected"), columns[1][0]);
        Assert.Contains("2.7", columns[0][4].Replace(',', '.'));
        Assert.Contains("1.5", columns[1][4].Replace(',', '.'));
        Assert.Contains("0.75", columns[2][4].Replace(',', '.'));
        Assert.Contains("16.7", columns[0][5].Replace(',', '.'));
        Assert.Contains("10.0", columns[1][5].Replace(',', '.'));
        Assert.Contains("6.3", columns[2][5].Replace(',', '.'));
        Assert.Contains("0.42", columns[1][6].Replace(',', '.'));
        f.Expose(800);
        f.Screen.OnKeyDown(Key.Q);
        point = f.Pixel(800); f.Screen.OnMouseMove(point.X, point.Y); f.Render();
        row = Assert.Single(f.Screen.CommandsPanel.CommandPanelRows, r => r.Weapon?.ModuleId == PointCombatUiFixture.Torpedo);
        Assert.All(row.DetailLines.Where(l => l.Contains('\t')), l => Assert.Contains(Localization.Combat("Panel.OutOfRange"), l));
    }

    [Fact]
    public async Task Actual_entry_uses_medium_and_remembers_fast_per_session()
    {
        await using (var f = await PointCombatUiFixture.Create())
        {
            f.Screen.OnKeyDown(Key.Q);
            Assert.Equal("medium", f.Screen.PointAim!.ProfileId);
            f.Screen.OnKeyDown(Key.Number3);
            f.Screen.OnKeyDown(Key.Escape);
            f.Screen.OnKeyDown(Key.Q);
            Assert.Equal("fast", f.Screen.PointAim!.ProfileId);
            f.Screen.OnKeyDown(Key.E);
            Assert.Equal("wide", f.Screen.PointAim!.ProfileId);
            f.Screen.OnKeyDown(Key.Q);
            Assert.Equal("fast", f.Screen.PointAim!.ProfileId);
        }
        await using var fresh = await PointCombatUiFixture.Create();
        fresh.Screen.OnKeyDown(Key.Q);
        Assert.Equal("medium", fresh.Screen.PointAim!.ProfileId);
    }

    [Fact]
    public void Removed_remembered_profile_and_missing_default_fall_back_safely()
    {
        System.Collections.Immutable.ImmutableArray<LaunchProfileSnapshot> profiles = [new("custom", 6, 9), new("fast", 16, 2.5)];
        Assert.Equal("fast", GameSessionScreen.ResolvePanelProfile(profiles, "fast", "missing"));
        Assert.Equal("custom", GameSessionScreen.ResolvePanelProfile(profiles, "removed", "missing"));
        Assert.Equal("fast", GameSessionScreen.ResolvePanelProfile(profiles, "removed", "fast"));
    }

    [Fact]
    public void Shipped_visual_configuration_declares_profile_defaults()
    {
        var settings = CombatVisualSettings.Load(Path.Combine(PointCombatUiFixture.Root, CombatVisualSettings.RelativePath));
        Assert.Equal("medium", settings.DefaultTorpedoProfile);
        Assert.Equal("wide", settings.DefaultCountermissileProfile);
    }

    [Fact]
    public async Task Two_installed_counter_launchers_keep_independent_profile_memory()
    {
        await using var f = await PointCombatUiFixture.Create();
        void Open(string module)
        {
            var panel = f.Screen.CommandsPanel;
            var row = Assert.Single(panel.CommandPanelRows, r => r.Weapon?.ModuleId == module);
            if (!row.Opened) { panel.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY); f.Render(); }
            row = Assert.Single(panel.CommandPanelRows, r => r.Weapon?.ModuleId == module);
            var button = Assert.Single(row.Buttons, b => b.CommandTypeId == DefenseCommandTypes.Launch);
            panel.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
            f.Render();
            Assert.Equal(module, f.Screen.PointAim!.ModuleId);
        }
        Open("MOD-PLAYER-COUNTERMEASURE-01");
        f.Screen.OnKeyDown(Key.Number2);
        Open("MOD-PLAYER-COUNTERMEASURE-02");
        Assert.Equal("wide", f.Screen.PointAim!.ProfileId);
        Open("MOD-PLAYER-COUNTERMEASURE-01");
        Assert.Equal("fast", f.Screen.PointAim!.ProfileId);
    }
}
