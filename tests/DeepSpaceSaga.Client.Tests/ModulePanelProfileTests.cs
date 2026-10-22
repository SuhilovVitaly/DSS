using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public sealed class ModulePanelProfileTests
{
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
