using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class PointCombatUS0020TK0003Tests
{
    private static ObjectMotionSnapshot Foreign(double x = 100) => new("foreign", x, 0, 6, 90,
        RenderObjectType: SpaceObjectType.Missile, ObservedProjectile: new(ProjectileKind.Torpedo, x, 0, 1, 0, 6, "slow", 9));
    private static AuthoritativeSnapshot Scene(ulong sequence, params ObjectMotionSnapshot[] missiles) => new(sequence, (long)sequence * 100,
        SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip), .. missiles], "player");

    [Fact]
    public void Foreign_torpedo_has_no_forward_geometry()
    {
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(Scene(1, Foreign()));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        var geometry = screen.CombatTrajectories["foreign"];
        Assert.Empty(geometry.Prediction); Assert.Empty(geometry.Target); Assert.Null(geometry.Detonation);
        Assert.Empty(screen.PointProjectileVisuals["foreign"].Label);
        screen.OnDeactivated();
    }

    [Fact]
    public void Foreign_tail_scales_with_speed()
    {
        Assert.Equal(0, GameSessionScreen.ObservedTailPixels(0, 1));
        Assert.True(GameSessionScreen.ObservedTailPixels(2, .1) < GameSessionScreen.ObservedTailPixels(10, .1));
        Assert.Equal(18, GameSessionScreen.ObservedTailPixels(10000, 100));
    }

    [Fact]
    public void Foreign_trace_uses_presented_history()
    {
        var trails = new ObservedProjectileTrail(); var session = Guid.NewGuid();
        trails.Update(session, Scene(1, Foreign(10)));
        trails.Update(session, Scene(1, Foreign(10)));
        Assert.Single(trails.Get("foreign"));
        trails.Update(session, Scene(2, Foreign(20)));
        Assert.Equal(new[] { new FutureTrajectoryPoint(10, 0), new(20, 0) }, trails.Get("foreign"));
        trails.Update(session, Scene(3));
        trails.Update(session, Scene(4, Foreign(100)));
        Assert.Single(trails.Get("foreign"));
        trails.Update(Guid.NewGuid(), Scene(1, Foreign(200)));
        Assert.Equal(new FutureTrajectoryPoint(200, 0), Assert.Single(trails.Get("foreign")));
    }

    [Fact]
    public void Own_torpedo_visuals_remain()
    {
        var own = new ObjectMotionSnapshot("own", 0, 0, 6, 90, RenderObjectType: SpaceObjectType.Missile,
            OwnProjectile: new(ProjectileKind.Torpedo, "player", "tube", 0, 0, 0, 0, 300, 0, 0, 5000, 6, "slow", 9, 200, 20));
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(Scene(1, own));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        Assert.NotEmpty(screen.CombatTrajectories["own"].Prediction);
        Assert.NotNull(screen.PointProjectileVisuals["own"].DetonationCenter);
        screen.OnDeactivated();
    }
}
