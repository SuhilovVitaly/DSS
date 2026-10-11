using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class DualDefensePanelLayoutTests
{
    [Fact]
    public void Owner_without_defense_preserves_template_groups()
    {
        var panel = new CommandsPanel();
        using var bitmap = new SKBitmap(400, 2000);
        using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, ImmutableArray<InstalledModuleSnapshot>.Empty, 2000, "player");
        Assert.Equal(CommandsPanel.Panels.Select(p => p.Name), panel.CommandPanelRows.Select(r => r.Name));
    }

    private static ImmutableArray<InstalledModuleSnapshot> Modules => Enumerable.Range(1, 2).Select(i =>
        new InstalledModuleSnapshot("defense-" + i, "module.countermeasure.launcher.basic", "Defense", i,
            [DefenseCommandTypes.Fire, DefenseCommandTypes.Enable, DefenseCommandTypes.Disable],
            Commands: [new(DefenseCommandTypes.Fire, "Fire " + i, "object")],
            Defense: new(false, null, DefenseState.Ready))).ToImmutableArray();

    private static void Render(CommandsPanel panel, float height = 1000, string owner = "player", bool reversed = false)
    {
        using var bitmap = new SKBitmap(400, (int)height);
        using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, reversed ? Modules.Reverse().ToArray() : Modules, height, owner);
    }

    [Fact]
    public void Two_defense_panels_have_distinct_owner_module_keys_and_command_hooks()
    {
        var clicked = new List<(string Owner, string Module, string Command)>();
        var panel = new CommandsPanel(weaponCommandEnabled: (_, _) => true,
            weaponCommandClicked: (w, c) => clicked.Add((w.OwnerObjectId, w.ModuleId, c)),
            weaponStatus: w => "status-" + w.ModuleId, weaponOperatorText: w => "operator-" + w.ModuleId);
        Render(panel, 2000);
        var rows = panel.CommandPanelRows.Where(r => r.Weapon is not null).ToArray();
        Assert.Equal(2, rows.Length);
        Assert.NotEqual(rows[0].Key, rows[1].Key);
        Assert.Equal(new[] { "defense-1", "defense-2" }, rows.Select(r => r.Weapon!.ModuleId));
        foreach (var row in rows)
        {
            Assert.Equal("status-" + row.Weapon!.ModuleId, row.StatusBarText);
            var fire = Assert.Single(row.Buttons, b => b.CommandTypeId == DefenseCommandTypes.Fire);
            Assert.Equal(row.Weapon, fire.Weapon);
            panel.OnMouseDown(fire.Rect.MidX, fire.Rect.MidY);
        }
        Assert.Equal(new[] { ("player", "defense-1", DefenseCommandTypes.Fire), ("player", "defense-2", DefenseCommandTypes.Fire) }, clicked);
    }

    [Fact]
    public void Collapsing_one_preserves_other_and_identity_survives_module_enumeration_change()
    {
        var panel = new CommandsPanel(weaponCommandEnabled: (_, _) => true);
        Render(panel, 2000);
        var first = panel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == "defense-1");
        panel.OnMouseDown(first.CaptionRect.MidX, first.CaptionRect.MidY);
        Render(panel, 2000, reversed: true);
        Assert.False(panel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == "defense-1").Opened);
        Assert.True(panel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == "defense-2").Opened);
        Render(panel, 2000, "another-player");
        Assert.All(panel.CommandPanelRows.Where(r => r.Weapon is not null), r => Assert.True(r.Opened));
    }

    [Theory]
    [InlineData(320)]
    [InlineData(480)]
    [InlineData(720)]
    public void Small_viewport_keeps_both_panels_reachable(int height)
    {
        var panel = new CommandsPanel(weaponCommandEnabled: (_, _) => true);
        Render(panel, height);
        foreach (string id in new[] { "defense-1", "defense-2" })
        {
            var row = panel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == id);
            Assert.InRange(row.CaptionRect.MidY, 40, height);
            if (!row.Opened) panel.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY);
            Render(panel, height);
            row = panel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == id);
            Assert.True(row.Opened);
            Assert.All(row.Buttons, b => Assert.InRange(b.Rect.Bottom, 40, height));
            Assert.InRange(row.StatusBarRect.Bottom, 40, height);
        }
    }

    [Fact]
    public void Very_short_viewport_scrolls_opened_panel_without_clicking_clipped_buttons()
    {
        var clicked = new List<string>();
        var panel = new CommandsPanel(weaponCommandEnabled: (_, _) => true,
            weaponCommandClicked: (w, _) => clicked.Add(w.ModuleId));
        Render(panel, 200);
        var row = panel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == "defense-2");
        panel.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY);
        Render(panel, 200);
        row = panel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == "defense-2");
        var fire = row.Buttons[0];
        Assert.True(fire.Rect.MidY > 200);
        Assert.False(panel.OnMouseDown(fire.Rect.MidX, fire.Rect.MidY));
        Assert.Empty(clicked);
        Assert.True(panel.OnMouseWheel(100, 100, -10));
        Render(panel, 200);
        fire = panel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == "defense-2").Buttons[0];
        Assert.InRange(fire.Rect.MidY, 40, 200);
        panel.OnMouseDown(fire.Rect.MidX, fire.Rect.MidY);
        Assert.Equal(new[] { "defense-2" }, clicked);
    }

    [Fact]
    public void Click_rechecks_the_specific_module_without_reusing_cached_enablement()
    {
        bool firstReady = true;
        var clicked = new List<string>();
        var panel = new CommandsPanel(weaponCommandEnabled: (w, _) => w.ModuleId != "defense-1" || firstReady,
            weaponCommandClicked: (w, _) => clicked.Add(w.ModuleId));
        Render(panel, 2000);
        firstReady = false;
        foreach (var button in panel.AllCommandButtons.Where(b => b.Weapon is not null && b.CommandTypeId == DefenseCommandTypes.Fire))
            panel.OnMouseDown(button.Rect.MidX, button.Rect.MidY);
        Assert.Equal(new[] { "defense-2" }, clicked);
    }
}
