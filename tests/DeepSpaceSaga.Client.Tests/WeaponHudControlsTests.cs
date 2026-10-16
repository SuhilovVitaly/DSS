using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class WeaponHudControlsTests
{
    private static ImmutableArray<InstalledModuleSnapshot> Modules =>
    [
        new("torp", "module.torpedo.launcher.basic", "Torpedo", 1, [CombatCommandTypes.Fire],
            LauncherCombat: new(10, 90, 150, RangeKm: 150, ReadyAtMotionTimeMs: 11000, ReloadDurationMs: 10000)),
        new("one", "module.countermeasure.launcher.basic", "Defense 1", 2, [DefenseCommandTypes.Fire],
            Defense: new(true, null, DefenseState.Reloading, ReloadDueMotionTimeMs: 11000, RangeKm: 50, ManualRangeKm: 100,
                ReadyAtMotionTimeMs: 11000, ReloadDurationMs: 10000)),
        new("two", "module.countermeasure.launcher.basic", "Defense 2", 3, [DefenseCommandTypes.Fire],
            Defense: new(true, null, DefenseState.Ready, RangeKm: 70, ManualRangeKm: 110, ReloadDurationMs: 10000))
    ];

    [Theory]
    [InlineData(1f)]
    [InlineData(1.2f)]
    [InlineData(1.5f)]
    public void Range_checkboxes_pin_each_launcher_independently_even_when_panels_are_hidden(float scale)
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0)], "player", InstalledModules: Modules));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale, timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1200);
        using var canvas = new SKCanvas(bitmap);
        void Render() => screen.Render(canvas, 1920, 1200);
        Render();
        foreach (var id in new[] { "torp", "one", "two" })
        {
            var row = screen.CommandsPanel.CommandPanelRows.Single(r => r.RangeModuleId == id);
            Assert.False(row.RangeToggleRect.IsEmpty);
            Assert.InRange(row.RangeToggleRect.Bottom * scale, 0, 1200);
            screen.OnMouseDown(row.RangeToggleRect.MidX * scale, row.RangeToggleRect.MidY * scale);
            Render();
            Assert.Equal(row.Opened, screen.CommandsPanel.CommandPanelRows.Single(r => r.RangeModuleId == id).Opened);
            Assert.True(screen.CommandsPanel.IsWeaponRangeVisible(id));
        }
        screen.OnMouseMove(1000, 500);
        var hide = screen.CommandsPanel.HideShowButtonRect;
        screen.OnMouseDown(hide.MidX * scale, hide.MidY * scale);
        Render();
        Assert.Equal(CommandsPanelState.Closed, screen.CommandsPanel.State);
        Assert.Equal(new[] { "one", "torp", "two" }, screen.WeaponRangeRings.Select(r => r.ModuleId).Distinct().Order().ToArray());
        screen.CommandsPanel.OnMouseDown(hide.MidX, hide.MidY);
        Render();
        var first = screen.CommandsPanel.CommandPanelRows.Single(r => r.RangeModuleId == "one");
        screen.OnMouseDown(first.RangeToggleRect.MidX * scale, first.RangeToggleRect.MidY * scale);
        screen.OnMouseMove(1000, 500);
        Render();
        Assert.DoesNotContain(screen.WeaponRangeRings, r => r.ModuleId == "one");
        Assert.Contains(screen.WeaponRangeRings, r => r.ModuleId == "two");
        screen.OnDeactivated();
    }

    [Fact]
    public void Reload_bars_advance_between_snapshots_and_freeze_on_pause()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        buffer.Update(new(1, 300000, SimulationSpeed.Speed1, [new("player", 0, 0, 0, 0)], "player",
            InstalledModules: Modules, SimulationTimeMs: 1000));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now);
        using var bitmap = new SKBitmap(1920, 1200);
        using var canvas = new SKCanvas(bitmap);
        void Render() => screen.Render(canvas, 1920, 1200);
        Render();
        var torpedo = screen.CommandsPanel.CommandPanelRows.Single(r => r.RangeModuleId == "torp");
        Assert.False(torpedo.ReloadBarRect.IsEmpty);
        Assert.Equal(0, torpedo.ReloadFraction);
        now = Stopwatch.Frequency * 20; // 5000 physical ms at the new base pace.
        Render();
        Assert.Equal(.5, screen.CommandsPanel.CommandPanelRows.Single(r => r.RangeModuleId == "torp").ReloadFraction, 6);
        Assert.Equal(.5, screen.CommandsPanel.CommandPanelRows.Single(r => r.RangeModuleId == "one").ReloadFraction, 6);
        Assert.Equal(1, screen.CommandsPanel.CommandPanelRows.Single(r => r.RangeModuleId == "two").ReloadFraction);
        buffer.CurrentSpeed = SimulationSpeed.Speed0;
        now += Stopwatch.Frequency * 100;
        Render();
        Assert.Equal(.5, screen.CommandsPanel.CommandPanelRows.Single(r => r.RangeModuleId == "torp").ReloadFraction, 6);
        buffer.CurrentSpeed = SimulationSpeed.Speed1;
        now += Stopwatch.Frequency * 20;
        Render();
        Assert.Equal(1, screen.CommandsPanel.CommandPanelRows.Single(r => r.RangeModuleId == "torp").ReloadFraction);
        screen.OnDeactivated();
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void Visible_contacts_outside_largest_free_rectangle_never_get_edge_markers(float scale)
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            [new("player", 0, 0, 0, 0), new("enemy", -100, 0, 0, 0, RenderObjectType: SpaceObjectType.NpcShip)],
            "player", SelectedObjectId: "enemy"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale, timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        var free = screen.AvailableMapRect();
        var world = World(screen, Math.Max(10, free.Left - 5), 540);
        buffer.Update(buffer.Latest!.Snapshot with
        {
            SnapshotSequence = 2,
            Objects = [new("player", 0, 0, 0, 0), new("enemy", world.X, world.Y, 0, 0, RenderObjectType: SpaceObjectType.NpcShip)]
        });
        screen.OnDeactivated();
        screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale, timestampProvider: () => 0);
        screen.Render(canvas, 1920, 1080);
        Assert.Empty(screen.OffscreenMarkerIds);
        var outside = World(screen, -100, 540);
        buffer.Update(buffer.Latest!.Snapshot with
        {
            SnapshotSequence = 3,
            Objects = [new("player", 0, 0, 0, 0), new("enemy", outside.X, outside.Y, 0, 0, RenderObjectType: SpaceObjectType.NpcShip)]
        });
        screen.OnDeactivated();
        screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale, timestampProvider: () => 0);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal("enemy", Assert.Single(screen.OffscreenMarkerIds));
        screen.OnDeactivated();
    }
    private static (double X, double Y) World(GameSessionScreen screen, float x, float y) =>
        (screen.CameraFocusX + (x - 960) / screen.CameraPixelsPerWorldUnit,
         screen.CameraFocusY + (y - 540) / screen.CameraPixelsPerWorldUnit);

}
