using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Ctrl+Click navigation tests (ТЗ-08): click semantics (AC1/AC2), focus behavior,
/// object/panel hit-testing, NavigationTrajectoryProjector shape (AC4) and the
/// GetNavigationTrajectory test seam.
/// </summary>
public class GameSessionNavigationTests
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const string PlayerShipId = "SPC-0001";
    private const string EngineModuleId = "MOD-PLAYER-ENGINE-01";

    // Ship at (10000, 10000), camera focus (10000, 10000), PPU 1.0:
    // ScreenToWorld(1000, 500) = (10000 + (1000-640)/1, 10000 + (500-360)/1) = (10360, 10140).
    // (1000, 500) is outside the Commands Panel (8,8)-(368,248) — a free map area.

    [Fact]
    public async Task Ctrl_click_on_free_area_sends_exactly_one_navigate_command_with_world_coordinates()
    {
        // ТЗ-08.1 (AC2): Ctrl+Click over free map area sends exactly one
        // engine.navigate-to-point command with the clicked world coordinates.
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        fixture.Screen.OnKeyDown(Key.ControlLeft);
        fixture.Screen.OnMouseDown(1000, 500);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(ShipEngineCommandTypes.NavigateToPoint, command.CommandType);
        Assert.Equal(PlayerShipId, command.ObjectId);
        Assert.Equal(EngineModuleId, command.ModuleId);
        Assert.Equal(10360.0, command.TargetWorldX!.Value, precision: 6);
        Assert.Equal(10140.0, command.TargetWorldY!.Value, precision: 6);
        Assert.False(string.IsNullOrWhiteSpace(command.CommandId));
        Assert.Equal(1UL, command.ClientSequence);
    }

    [Fact]
    public async Task Plain_click_sends_no_command_and_changes_focus()
    {
        // ТЗ-08.2 (AC1): plain click without Ctrl keeps the old behavior — camera pans,
        // no command is sent.
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        fixture.Screen.OnMouseDown(1000, 500);

        Assert.Empty(fixture.Connection.Commands);
        Assert.Equal(10360.0, fixture.Screen.CameraFocusX, precision: 6);
        Assert.Equal(10140.0, fixture.Screen.CameraFocusY, precision: 6);
    }

    [Fact]
    public async Task Ctrl_click_does_not_change_camera_focus()
    {
        // ТЗ-08.3: Ctrl+Click never pans the camera — focus stays untouched.
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        double fxBefore = fixture.Screen.CameraFocusX;
        double fyBefore = fixture.Screen.CameraFocusY;

        fixture.Screen.OnKeyDown(Key.ControlRight);
        fixture.Screen.OnMouseDown(1000, 500);

        Assert.Equal(fxBefore, fixture.Screen.CameraFocusX);
        Assert.Equal(fyBefore, fixture.Screen.CameraFocusY);
        Assert.Single(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Ctrl_click_on_object_or_panel_sends_no_command()
    {
        // ТЗ-08.4: Ctrl+Click over an object (hit-test with +4 px slack) or over a
        // panel (consumed by the panel hit-tests before the map branch) sends nothing.
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        fixture.Screen.OnKeyDown(Key.ControlLeft);

        // Ship renders at screen center (640, 360) — the click lands on the object.
        fixture.Screen.OnMouseDown(ScreenWidth / 2f, ScreenHeight / 2f);
        Assert.Empty(fixture.Connection.Commands);

        // Command panel padding (outside any button) consumes the click.
        var panel = fixture.Screen.LastCommandPanelRect;
        fixture.Screen.OnMouseDown(panel.Left + 2f, panel.MidY);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Navigation_trajectory_projection_shows_wait_turn_then_straight_to_target()
    {
        // ТЗ-08.5 (AC4): NavigationTrajectoryProjector produces the approved shape —
        // straight (wait while r < R), stepwise turn once r ≥ R, straight through the
        // target — and stops at IsArrived instead of running the full horizon.
        // Deterministic geometries (arrival circles cannot be jumped over):
        //   (A) wait case: ship (0,0) at 0.7 km/s → R = v/ω ≈ 100.3; target (0,-100)
        //       exactly on course and r = 100 < R → straight flight, arrival when the
        //       step grid lands inside ArrivalEpsilon (phase 100 mod 1.75 = 0.25).
        //   (B) turn case: ship (0,0) at 1 km/s → R ≈ 143.24; target (3,-150): r ≈ 150
        //       ≥ R, course delta ≈ 1.15° → one stepwise turn to course 1°, then a
        //       straight approach with miss distance r·sin(delta) ≈ 0.39 < ArrivalEpsilon.
        var projector = new NavigationTrajectoryProjector();

        var waitShip = new ObjectMotionSnapshot(
            "ship",
            X: 0, Y: 0,
            SpeedKmS: 0.7,
            Direction: 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.NavigateToPoint,
            TurnStepDegrees: 1,
            TurnStepRemainingMs: 250,
            TurnStepIntervalMs: 250,
            NavigationTargetX: 0,
            NavigationTargetY: -100,
            NavigationAngularInertiaDegPerSec: 4);

        var waitPoints = projector.Project(waitShip);

        Assert.True(waitPoints.Count >= 3, "Expected a straight wait segment before arrival");
        Assert.Equal(0.0, waitPoints[0].X, precision: 6);
        Assert.Equal(0.0, waitPoints[0].Y, precision: 6);
        for (int i = 1; i < waitPoints.Count; i++)
        {
            Assert.Equal(0.0, waitPoints[i].X, precision: 6); // straight up, no turn while r < R
            Assert.True(waitPoints[i].Y < waitPoints[i - 1].Y, "Wait segment must fly straight up");
        }

        AssertArrival(waitPoints, 0, -100);

        var turnShip = new ObjectMotionSnapshot(
            "ship",
            X: 0, Y: 0,
            SpeedKmS: 1,
            Direction: 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.NavigateToPoint,
            TurnStepDegrees: 1,
            TurnStepRemainingMs: 250,
            TurnStepIntervalMs: 250,
            NavigationTargetX: 3,
            NavigationTargetY: -150,
            NavigationAngularInertiaDegPerSec: 4);

        var turnPoints = projector.Project(turnShip);

        Assert.True(turnPoints.Count >= 3, "Expected a straight segment, a turn and an approach");
        Assert.Equal(0.0, turnPoints[0].X, precision: 6);
        Assert.Equal(0.0, turnPoints[0].Y, precision: 6);

        // Initial straight segment: before the first cycle step the ship flies with
        // the CURRENT course (0° = up), so x stays near zero.
        Assert.True(Math.Abs(turnPoints[1].X) <= 0.1, "Initial segment must fly straight up");

        // Turn segment: a later point has x > 0 (course rotated toward the target,
        // which sits to the right and below the initial course).
        Assert.True(turnPoints.Any(p => p.X > 0), "Expected the trajectory to turn toward the target");

        // The path is not a single straight line: at least one segment changes
        // direction (0° → 1°), then the approach continues straight through the target.
        int directionChanges = 0;
        for (int i = 2; i < turnPoints.Count; i++)
        {
            var a = turnPoints[i - 2];
            var b = turnPoints[i - 1];
            var c = turnPoints[i];
            double cross = (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);
            if (Math.Abs(cross) > 1e-9)
                directionChanges++;
        }

        Assert.True(directionChanges >= 1, "Expected the course to change at least once (turn)");

        // Final approach: the last few points are collinear (straight through the
        // target once the course matches the bearing).
        var lastDir = UnitDirection(turnPoints[^2], turnPoints[^1]);
        bool finalSegmentStraight = true;
        for (int i = turnPoints.Count - 3; i >= turnPoints.Count - 6 && i >= 0; i--)
        {
            var dir = UnitDirection(turnPoints[i], turnPoints[i + 1]);
            double dot = dir.X * lastDir.X + dir.Y * lastDir.Y;
            if (dot < 0.999)
            {
                finalSegmentStraight = false;
                break;
            }
        }

        Assert.True(finalSegmentStraight, "Expected a straight final approach through the target");
        AssertArrival(turnPoints, 3, -150);
    }

    [Fact]
    public void Navigation_trajectory_arrives_for_lateral_miss_target()
    {
        // Regression: lateral miss (1000,-3000) at 4 km/s with 1°/250ms turn.
        // Before the segment-arrival fix this would circle forever.
        // R = 4000 / (4π/180) / 100 ≈ 573 wu; r = sqrt(1000²+3000²) ≈ 3162 wu ≥ R → turns possible.
        var ship = new ObjectMotionSnapshot(
            "ship",
            X: 0, Y: 0,
            SpeedKmS: 4,
            Direction: 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.NavigateToPoint,
            TurnStepDegrees: 1,
            TurnStepRemainingMs: 250,
            TurnStepIntervalMs: 250,
            NavigationTargetX: 1000,
            NavigationTargetY: -3000,
            NavigationAngularInertiaDegPerSec: 4);

        var projector = new NavigationTrajectoryProjector();
        var points = projector.Project(ship);

        Assert.True(points.Count >= 2, "Expected at least a start point and one step");
        // Discrete steps at 4 km/s × 250ms = 10 wu/step; arrival tolerance must allow
        // up to one step distance plus the proximity epsilon.
        AssertArrival(points, 1000, -3000, tolerance: 11.0);
    }

    [Fact]
    public void Navigation_trajectory_completes_in_under_horizon()
    {
        // The projection must stop at IsArrived, not run the full 3000 ms.
        var ship = new ObjectMotionSnapshot(
            "ship",
            X: 0, Y: 0,
            SpeedKmS: 4,
            Direction: 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.NavigateToPoint,
            TurnStepDegrees: 1,
            TurnStepRemainingMs: 250,
            TurnStepIntervalMs: 250,
            NavigationTargetX: 0,
            NavigationTargetY: -100,
            NavigationAngularInertiaDegPerSec: 4);

        var projector = new NavigationTrajectoryProjector();
        var points = projector.Project(ship);

        Assert.True(points.Count < NavigationTrajectoryProjector.FutureTrajectoryHorizonMs / 250,
            $"Projection must stop at IsArrived, not run full horizon. Got {points.Count} points.");
        AssertArrival(points, 0, -100);
    }

    private static void AssertArrival(IReadOnlyList<FutureTrajectoryPoint> points, double targetX, double targetY, double tolerance = 1.0)
    {
        double dx = points[^1].X - targetX;
        double dy = points[^1].Y - targetY;
        double distanceToTarget = Math.Sqrt(dx * dx + dy * dy);
        Assert.True(distanceToTarget <= tolerance + 1e-6,
            $"Projection must stop at arrival, last point {points[^1].X:F3},{points[^1].Y:F3} " +
            $"is {distanceToTarget:F3} from the target (tolerance {tolerance:F1})");
        Assert.True(points.Count < NavigationTrajectoryProjector.FutureTrajectoryHorizonMs / 250,
            "Projection must stop at IsArrived, not run the full horizon");
    }

    private static (double X, double Y) UnitDirection(FutureTrajectoryPoint from, FutureTrajectoryPoint to)
    {
        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        double len = Math.Sqrt(dx * dx + dy * dy);
        return len > 0 ? (dx / len, dy / len) : (0, 0);
    }

    [Fact]
    public async Task Navigation_trajectory_test_seam_is_empty_without_authoritative_target()
    {
        // ТЗ-08.6: GetNavigationTrajectory returns empty until the snapshot carries an
        // authoritative NavigationTarget (no active navigate cycle).
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        var trajectory = fixture.Screen.GetNavigationTrajectory(PlayerShipId);
        Assert.Empty(trajectory);
    }

    // ── Test helpers (same fixture pattern as GameSessionEngineCommandPanelTests) ──

    private static TestFixture CreateFixture(double speedKmS = 1.0)
    {
        var connection = new RecordingConnection();
        var handle = new GameSessionHandle(connection);
        var ship = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: speedKmS, Direction: 0);
        handle.Buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(ship),
            PlayerShipObjectId: PlayerShipId));

        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        return new TestFixture(connection, handle, screen);
    }

    private static void Render(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    private sealed record TestFixture(
        RecordingConnection Connection,
        GameSessionHandle Handle,
        GameSessionScreen Screen) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            return Handle.DisposeAsync();
        }
    }

    private sealed class RecordingConnection : IGameSessionConnection
    {
        public List<PlayerCommand> Commands { get; } = [];

        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return ValueTask.CompletedTask;
        }

        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask SaveAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
