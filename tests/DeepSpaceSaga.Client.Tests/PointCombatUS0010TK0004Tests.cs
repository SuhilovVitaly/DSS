using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0010TK0004Tests
{
    private static ObjectMotionSnapshot Player(double turn = 0) => new("player", 0, 0, 1, 0,
        RenderObjectType: SpaceObjectType.PlayerShip, MaxSpeedKmS: 5, TurnHoldRateDegPerSec: turn == 0 ? null : turn);
    private static AuthoritativeSnapshot Scene(params ObjectMotionSnapshot[] objects) => new(1, 0, SimulationSpeed.Speed1,
        objects.ToImmutableArray(), "player", CombatJournal: [], Encounter: new(CombatEncounterState.Active, 0, null, new()));

    private static void AssertOutsidePanels(GameSessionScreen screen, SKRect free, float scale)
    {
        foreach (var panel in new[] { screen.CommandsPanel.CaptionRect, screen.CommandsPanel.BodyRect,
            screen.ObjectInfoPanel.CaptionRect, screen.ObjectInfoPanel.BodyRect, screen.CombatReportsBounds, screen.MapToolbarRect })
        {
            if (panel.IsEmpty) continue;
            Assert.False(free.IntersectsWith(new SKRect(panel.Left * scale, panel.Top * scale, panel.Right * scale, panel.Bottom * scale)),
                $"Free map viewport {free} must avoid panel {panel} at scale{scale}");
        }
    }

    [Theory]
    [InlineData(-20d)]
    [InlineData(0d)]
    [InlineData(20d)]
    public void Own_path_uses_current_confirmed_turn_for_five_seconds(double rate)
    {
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(Scene(Player(rate)));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        var path = screen.GetFutureTrajectory("player"); Assert.Equal(51, path.Count);
        double radians = rate * Math.PI / 180;
        double expectedX = rate == 0 ? 0 : 10 * (1 - Math.Cos(radians * 5)) / radians;
        double expectedY = rate == 0 ? -50 : -10 * Math.Sin(radians * 5) / radians;
        Assert.Equal(expectedX, path[^1].X, 7); Assert.Equal(expectedY, path[^1].Y, 7);
        Assert.Equal(path[^1], screen.DisplayedPlayerTrajectoryEnd);
        Assert.Equal(2, screen.ExternalPlayerTrajectory.Count);
        Assert.Equal(0, screen.ExternalPlayerTrajectory[^1].X, 7); Assert.Equal(-10, screen.ExternalPlayerTrajectory[^1].Y, 7);
        screen.OnDeactivated();
    }

    [Theory]
    [InlineData(1920, 1080, 1f)]
    [InlineData(1280, 720, 1.5f)]
    public void Fit_battle_includes_relevant_ships_and_projectiles(int width, int height, float scale)
    {
        var own = new ObjectMotionSnapshot("own", 150, 0, 6, 90, RenderObjectType: SpaceObjectType.Missile,
            OwnProjectile: new(ProjectileKind.Torpedo, "player", "launcher", 150, 0, 150, 0, 600, 200, 0, 8207.3815, 6, "slow", 9, 200, 20));
        var foreign = new ObjectMotionSnapshot("foreign", -1800, 600, 6, 270, RenderObjectType: SpaceObjectType.Missile,
            ObservedProjectile: new(ProjectileKind.Torpedo, -1800, 600, -1, 0, 6, "slow", 9));
        var enemy = new ObjectMotionSnapshot("enemy", -600, -500, 1, 90, RenderObjectType: SpaceObjectType.NpcShip,
            RelationToPlayer: PlayerRelation.Enemy, HostileCombat: new("player", "engine", "weapon", 50, true, HostileCombatPhase.Pursuit,
                new(0, 100, 0, -600, -500, 1, 90, 0, 10)));
        var unrelated = new ObjectMotionSnapshot("station", 1e9, 1e9, 0, 0, RenderObjectType: SpaceObjectType.Station);
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(Scene(Player(), own, foreign, enemy, unrelated));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0, uiScale: scale);
        using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, width, height);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.F));
        screen.Render(canvas, width, height);
        var rect = screen.AvailableMapRect();
        Assert.True(rect.Width >= 100 && rect.Height >= 100, $"Battle viewport must remain useful: {rect}");
        AssertOutsidePanels(screen, rect, scale);
        foreach (var point in new[] { (0d, 0d), (-600d, -500d), (-1800d, 600d), (510d, 110d), (690d, 290d) })
        {
            float x = (float)(width / 2d + (point.Item1 - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit);
            float y = (float)(height / 2d + (point.Item2 - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit);
            Assert.True(rect.Contains(x, y), $"{point} must fit free map viewport {rect}; actual{x},{y}");
        }
        Assert.True(screen.CameraPixelsPerWorldUnit > .001); // unrelated station and unbounded foreign ray are excluded
        screen.OnDeactivated();
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public async Task Battle_fit_reframes_actual_engine_projectiles_away_from_panels(float scale)
    {
        await using var fixture = new PointCombatSessionFixture();
        fixture.Engine.ReceiveCommand(PointCombatSessionFixture.Launch());
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(fixture.Engine.CaptureSnapshot());
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0, uiScale: scale);
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.F)); screen.Render(canvas, 1920, 1080);
        var rect = screen.AvailableMapRect();
        Assert.True(rect.Width >= 100 && rect.Height >= 100);
        AssertOutsidePanels(screen, rect, scale);
        var own = Assert.Single(screen.PointProjectileVisuals.Values, v => v.DetonationCenter is not null);
        var center = own.DetonationCenter!.Value; float radius = (float)own.PhysicalRadiusPx!.Value;
        Assert.True(rect.Contains(center.X - radius, center.Y - radius));
        Assert.True(rect.Contains(center.X + radius, center.Y + radius));
        screen.OnDeactivated();
    }

    [Fact]
    public void Bare_F_and_Ctrl_F_have_distinct_actual_input_edges()
    {
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(Scene(Player()));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, 1920, 1080);
        var tracker = new KeyboardEdgeTracker(); var down = new HashSet<Key> { Key.F };
        var pressed = new Key[32]; var released = new Key[32];
        var (count, _) = tracker.PollBoth(down.Contains, pressed, released);
        Assert.Equal(1, count); Assert.Equal(Key.F, pressed[0]);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(pressed[0]));
        down.Clear(); var (_, up) = tracker.PollBoth(down.Contains, pressed, released);
        for (int i = 0; i < up; i++) screen.OnKeyUp(released[i]);
        down.UnionWith([Key.ControlLeft, Key.F]);
        (count, _) = tracker.PollBoth(down.Contains, pressed, released);
        Assert.Equal(new[] { Key.ControlLeft, Key.F }, pressed[..count]);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(pressed[0]));
        Assert.Equal(ScreenEvent.OpenFinance, screen.OnKeyDown(pressed[1]));
        tracker.ResyncToCurrentState(down.Contains);
        (count, up) = tracker.PollBoth(down.Contains, pressed, released); Assert.Equal(0, count); Assert.Equal(0, up);
        screen.OnDeactivated();
    }
}
