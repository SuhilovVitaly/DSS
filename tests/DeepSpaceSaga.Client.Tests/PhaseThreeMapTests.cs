using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests;

public class PhaseThreeMapTests
{
    private static InstalledModuleSnapshot Defense(string id, int position, double auto, double manual) => new(id,
        "module.countermeasure.launcher.basic", id, position, [DefenseCommandTypes.Fire], "On", "Ready", 60,
        Commands: [new(DefenseCommandTypes.Fire, "Fire", "object")], Defense: new(false, null, DefenseState.Ready, RangeKm: auto, ManualRangeKm: manual));

    [Theory]
    [InlineData(1f)]
    [InlineData(1.2f)]
    [InlineData(1.5f)]
    public void Only_hovered_panel_shows_its_world_space_ranges(float scale)
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            [new("player", 0, 0, 0, 0), new("enemy", 200, 200, 0, 0, RenderObjectType: SpaceObjectType.NpcShip,
                Defense: new(true, null, DefenseState.Ready))], "player", SelectedObjectId: "enemy",
            InstalledModules: [new("torp", "module.torpedo.launcher.basic", "Launcher", 1, [CombatCommandTypes.Fire],
                LauncherCombat: new(3, 90, 150, RangeKm: 200)), Defense("one", 2, 37, 81), Defense("two", 3, 49, 123)]));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale, timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        void Render() => screen.Render(canvas, 1920, 1080);
        Render();
        Assert.Empty(screen.WeaponRangeRings); // Selection alone never shows the old NPC defense circle.
        foreach (var (id, auto, manual) in new[] { ("one", 37d, 81d), ("two", 49d, 123d) })
        {
            var row = screen.CommandsPanel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == id);
            screen.OnMouseMove(row.CaptionRect.MidX * scale, row.CaptionRect.MidY * scale);
            Render();
            Assert.Equal(2, screen.WeaponRangeRings.Count);
            Assert.All(screen.WeaponRangeRings, r => Assert.Equal(id, r.ModuleId));
            Assert.Equal(auto, screen.WeaponRangeRings.Single(r => r.Kind == "auto").RangeKm);
            Assert.Equal(manual, screen.WeaponRangeRings.Single(r => r.Kind == "manual").RangeKm);
            Assert.All(screen.WeaponRangeRings, r => Assert.Equal(r.RangeKm * 10 * screen.CameraPixelsPerWorldUnit, r.RadiusPx, 3));
        }
        var launcher = screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Torpedo Launcher");
        screen.OnMouseMove(launcher.CaptionRect.MidX * scale, launcher.CaptionRect.MidY * scale);
        Render();
        Assert.Equal("torpedo", Assert.Single(screen.WeaponRangeRings).Kind);
        float beforeZoom = screen.WeaponRangeRings[0].RadiusPx;
        screen.OnKeyDown(Key.Minus);
        Render();
        Assert.NotEqual(beforeZoom, screen.WeaponRangeRings[0].RadiusPx);
        Assert.Equal(200 * 10 * screen.CameraPixelsPerWorldUnit, screen.WeaponRangeRings[0].RadiusPx, 3);
        var hide = screen.CommandsPanel.HideShowButtonRect;
        screen.OnMouseDown(hide.MidX * scale, hide.MidY * scale);
        Render();
        Assert.Empty(screen.WeaponRangeRings);
        screen.OnMouseMove(900, 600);
        Render();
        Assert.Empty(screen.WeaponRangeRings);
        screen.OnDeactivated();
    }

    private static ObjectMotionSnapshot Projectile(string id, string module, double x = 0, double y = 0) => new(id, x, y, 12, 90,
        RenderObjectType: SpaceObjectType.Countermeasure,
        Countermeasure: new("player", module, "missing", CountermeasurePhase.Guiding,
            new(0, 1, TorpedoRoutePhase.Straight, false, [new(x, y, 90, 12, 0, 60000)]), 0, 500,
            new(new("crew", "Crew", WeaponSkillType.CountermeasureDefense, 50, 55, 55), 5),
            MaxFlightTimeMs: 60000, ExpiresAtMotionTimeMs: 60000));

    [Fact]
    public void Calculation_labels_stay_hidden_through_pause_and_resume()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        buffer.Update(new(1, 9000000, SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0), Projectile("pr", "one")],
            "player", SimulationTimeMs: 0));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        void Render() => screen.Render(canvas, 1280, 720);
        Render();
        Assert.Empty(screen.DefenseFlightLabels);
        now = Stopwatch.Frequency * 100;
        Render();
        Assert.Empty(screen.DefenseFlightLabels);
        buffer.Update(buffer.Latest!.Snapshot with { SnapshotSequence = 2, CurrentSpeed = SimulationSpeed.Speed2 });
        now += Stopwatch.Frequency;
        Render();
        Assert.Empty(screen.DefenseFlightLabels);
        buffer.Update(buffer.Latest!.Snapshot with { SnapshotSequence = 3, CurrentSpeed = SimulationSpeed.Speed0, SimulationTimeMs = 5000 });
        now += Stopwatch.Frequency * 100;
        Render();
        Assert.Empty(screen.DefenseFlightLabels);
        buffer.Update(buffer.Latest!.Snapshot with { SnapshotSequence = 4, Objects = [buffer.Latest.Snapshot.Objects[0]] });
        Render();
        Assert.Empty(screen.DefenseFlightLabels);
        screen.OnDeactivated();
    }

    [Theory]
    [InlineData(1280, 720)]
    [InlineData(640, 360)]
    public void Coincident_countermeasures_do_not_display_calculation_labels(int width, int height)
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0),
            Projectile("pr-one", "launcher-one-" + new string('W', 75), width / 2 - 1, height / 2 - 1),
            Projectile("pr-two", "launcher-two-" + new string('W', 75), width / 2 - 1, height / 2 - 1)], "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, width, height);
        Assert.Empty(screen.DefenseFlightLabels);
        Assert.Empty(screen.DefenseChanceLabels);
        Assert.Equal(2, screen.RenderStates.Count(r => r.Source.Countermeasure is not null));
        screen.OnDeactivated();
    }

    [Fact]
    public void Offscreen_projectiles_do_not_move_their_labels_to_viewport_edges()
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0), Projectile("pr", "one", 10000, 10000)], "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        TorpedoCommandPanelTests.Render(screen);
        Assert.Empty(screen.DefenseFlightLabels);
        screen.OnDeactivated();
    }
}
