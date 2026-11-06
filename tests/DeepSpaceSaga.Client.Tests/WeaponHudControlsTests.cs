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
        new("torp", "module.torpedo.launcher.basic", "Torpedo", 1, [CombatCommandTypes.Launch],
            LauncherCombat: new(150, RangeKm: 150, ReadyAtMotionTimeMs: 11000, ReloadDurationMs: 10000,Profiles:[new("slow",6,9)])),
        new("one", "module.countermeasure.launcher.basic", "Defense 1", 2, [DefenseCommandTypes.Launch],
            Defense: new(null, DefenseState.Reloading, ReloadDueMotionTimeMs: 11000, RangeKm: 50,
                ReadyAtMotionTimeMs: 11000, ReloadDurationMs: 10000,Profiles:[new("wide",15,1)])),
        new("two", "module.countermeasure.launcher.basic", "Defense 2", 3, [DefenseCommandTypes.Launch],
            Defense: new(null, DefenseState.Ready, RangeKm: 70, ReloadDurationMs: 10000,Profiles:[new("wide",15,1)]))
    ];

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
