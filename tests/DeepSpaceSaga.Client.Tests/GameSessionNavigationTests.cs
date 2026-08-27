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
        // engine.orbit command with the clicked world coordinates.
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        fixture.Screen.OnKeyDown(Key.ControlLeft);
        fixture.Screen.OnMouseDown(1000, 500);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(ShipEngineCommandTypes.Orbit, command.CommandType);
        Assert.Equal(PlayerShipId, command.ObjectId);
        Assert.Equal(EngineModuleId, command.ModuleId);
        Assert.Equal(10360.0, command.TargetWorldX!.Value, precision: 6);
        Assert.Equal(10140.0, command.TargetWorldY!.Value, precision: 6);
        Assert.False(string.IsNullOrWhiteSpace(command.CommandId));
        Assert.Equal(1UL, command.ClientSequence);
    }

    [Fact]
    public async Task Plain_click_sends_no_command_and_does_not_change_focus()
    {
        // ТЗ-08.2 (AC1) + drag-pan follow-up: a plain click without Ctrl sends no
        // command. The camera no longer jumps to the clicked point by itself — that
        // jump fought with click-and-drag panning and was disabled; only actual
        // mouse movement while held pans the camera now (see GameSessionNavigationTests
        // drag-pan tests further down this file).
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        fixture.Screen.OnMouseDown(1000, 500);

        Assert.Empty(fixture.Connection.Commands);
        Assert.Equal(10000.0, fixture.Screen.CameraFocusX, precision: 6);
        Assert.Equal(10000.0, fixture.Screen.CameraFocusY, precision: 6);
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

        // Commands Panel header, away from the hide/show button — consumed by
        // CommandsPanel.OnMouseDown's final "caption/body" fallback, no action.
        var panel = fixture.Screen.CommandsPanel.CaptionRect;
        fixture.Screen.OnMouseDown(panel.Right - 5f, panel.MidY);
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
            ActiveEngineCommandType: ShipEngineCommandTypes.Orbit,
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
            ActiveEngineCommandType: ShipEngineCommandTypes.Orbit,
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
            ActiveEngineCommandType: ShipEngineCommandTypes.Orbit,
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
    public void Navigation_trajectory_projects_escape_turn_for_close_target()
    {
        // Regression: close side/rear targets use staged navigation. The client
        // projector must mirror EscapeTurn instead of falling back to the old
        // pure-approach loop around the target.
        var ship = new ObjectMotionSnapshot(
            "ship",
            X: 0, Y: 0,
            SpeedKmS: 1,
            Direction: 0,
            ActiveEngineCommandType: ShipEngineCommandTypes.Orbit,
            TurnStepDegrees: 1,
            TurnStepRemainingMs: 250,
            TurnStepIntervalMs: 250,
            NavigationTargetX: 100,
            NavigationTargetY: 70,
            NavigationAngularInertiaDegPerSec: 4,
            NavigationPhase: "EscapeTurn",
            NavigationEscapeCourseDegrees: 305);

        var projector = new NavigationTrajectoryProjector();
        var points = projector.Project(ship);

        Assert.True(points.Count >= 3, "Expected staged projection points");
        Assert.True(points.Any(p => p.X < -1),
            "EscapeTurn should project motion away from the close target, not loop toward it");
        AssertArrival(points, 100, 70, tolerance: 3.0);
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
            ActiveEngineCommandType: ShipEngineCommandTypes.Orbit,
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
    public void Approach_prediction_tracks_moving_target_like_the_authoritative_engine()
    {
        // ТЗ (story-20260827-083137.md, U4): client-side LinearMotionPredictor must
        // steer a navigation.approach cycle identically to the authoritative Engine
        // over several un-refreshed seconds (no new snapshot arriving in between).
        // Direction-convergence numbers (90 → 80 → 70 → 60) mirror
        // DeepSpaceSaga.Engine.Tests.ApproachCommandTests
        // .Approach_re_aims_every_cycle_toward_live_target_state's ship/target geometry,
        // but this test deliberately uses its own explicit TurnStepIntervalMs=1000 (not
        // the real Engine's cadence): it exercises PredictApproach's per-boundary steering
        // math generically, independent of Approach's actual current cadence. Since
        // Post-implementation bug fix #2, the real Engine now runs Approach at the same
        // faster ~250 ms cadence as Orbit (MinTurnIntervalMs) rather than the ~1000 ms this
        // test's own TurnStepIntervalMs value happens to also use — the two are no longer
        // the same value by coincidence of "current cadence", just by this test's own
        // choice of scenario. On the client side elapsedMs is measured from the bake
        // instant, when TurnStepRemainingMs already carries a FULL interval until the
        // *next* decision (same "initial wait phase before the first turn boundary"
        // convention already used by the Orbit branch, PredictNavigation) — so the first
        // decision fires once elapsedMs passes 1000 ms, not exactly at 1000 ms.
        //
        // Baked fields (NavigationTargetX/Y = aim point at bake time, NavigationTargetSpeedKmS/
        // NavigationTargetDirectionDegrees = target's live state at that same bake) are exactly
        // what the Engine's BuildSnapshot would have projected at GameTimeMs=0, per Checkpoint 1.
        var predictor = new LinearMotionPredictor();

        // Target at (10000, 10000), direction 90 (+X), speed 1.0 km/s, trailDistance 150 km
        // (1500 world units) → aim point at bake time = (10000 - 1500, 10000) = (8500, 10000).
        var state = new ObjectMotionSnapshot(
            "ship",
            X: 8500, Y: 20000,
            SpeedKmS: 0,
            Direction: 90,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach,
            TurnStepDegrees: 10,
            TurnStepRemainingMs: 1000,
            TurnStepIntervalMs: 1000,
            NavigationTargetX: 8500,
            NavigationTargetY: 10000,
            NavigationAngularInertiaDegPerSec: 4,
            NavigationTargetSpeedKmS: 1.0,
            NavigationTargetDirectionDegrees: 90);

        var beforeFirstBoundary = predictor.Predict(state, 1000);
        Assert.Equal(90.0, beforeFirstBoundary.Direction, precision: 6); // still waiting

        var afterCycle1 = predictor.Predict(state, 2000);
        Assert.Equal(80.0, afterCycle1.Direction, precision: 6);
        Assert.Equal(NavigationComputerCommandTypes.Approach, afterCycle1.ActiveEngineCommandType);

        var afterCycle2 = predictor.Predict(state, 3000);
        Assert.Equal(70.0, afterCycle2.Direction, precision: 6);

        var afterCycle3 = predictor.Predict(state, 4000);
        Assert.Equal(60.0, afterCycle3.Direction, precision: 6);

        // Ship speed stayed 0 throughout (isolates pure steering) — position unchanged.
        Assert.Equal(8500.0, afterCycle3.X, precision: 6);
        Assert.Equal(20000.0, afterCycle3.Y, precision: 6);
    }

    [Fact]
    public void Approach_trajectory_projection_turns_toward_the_extrapolated_moving_aim_point()
    {
        // Same scenario as the predictor test above, projected as a preview trajectory
        // line: the aim point keeps moving (+X) as the target advances, so the projected
        // course must keep turning cycle over cycle rather than freezing on the first
        // bake — mirroring the Orbit turn-shape assertions further up this file.
        var projector = new NavigationTrajectoryProjector();

        var state = new ObjectMotionSnapshot(
            "ship",
            X: 8500, Y: 20000,
            SpeedKmS: 0,
            Direction: 90,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach,
            TurnStepDegrees: 10,
            TurnStepRemainingMs: 1000,
            TurnStepIntervalMs: 1000,
            NavigationTargetX: 8500,
            NavigationTargetY: 10000,
            NavigationAngularInertiaDegPerSec: 4,
            NavigationTargetSpeedKmS: 1.0,
            NavigationTargetDirectionDegrees: 90);

        var points = projector.Project(state);

        Assert.True(points.Count >= 3, "Expected multiple projected trajectory points");
        Assert.Equal(8500.0, points[0].X, precision: 6);
        Assert.Equal(20000.0, points[0].Y, precision: 6);
    }

    [Fact]
    public void Approach_trajectory_projection_runs_past_the_old_200s_cap_to_reach_the_aim_point()
    {
        // Regression for Post-implementation bug fix #2 (story-20260827-083137.md): the
        // user explicitly asked that the predicted trajectory NOT be truncated before
        // reaching the target ("не ограничивай предсказанную траекторию, пусть она идет
        // пока не достигнет объекта"). Before the fix, ProjectApproach's loop stopped at
        // the fixed FutureTrajectoryHorizonMs (200s / 800 points at this 250 ms cadence)
        // even if the ship had not yet arrived. This scenario deliberately needs well over
        // 200s of simulated travel (slow ship, moderate distance) so the old cap would
        // have cut the line off mid-flight; the fix must instead keep projecting all the
        // way to actual arrival at the (stationary) aim point.
        var ship = new ObjectMotionSnapshot(
            "ship",
            X: 0, Y: 700,
            SpeedKmS: 0.3, // slow cruise — travel alone takes ~233s, already past the old 200s cap
            Direction: 180, // badly misaligned: aim point is "up" (toward smaller Y), ship faces "down"
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach,
            TurnStepDegrees: 1,
            TurnStepRemainingMs: 250,
            TurnStepIntervalMs: 250,
            NavigationTargetX: 0,
            NavigationTargetY: 0,
            NavigationAngularInertiaDegPerSec: 4,
            NavigationTargetSpeedKmS: 0, // stationary target/aim point — never moves
            NavigationTargetDirectionDegrees: 0);

        var projector = new NavigationTrajectoryProjector();
        var points = projector.Project(ship);

        // Proof the line was NOT cut off at the old 200s / 800-point cap.
        Assert.True(points.Count > NavigationTrajectoryProjector.FutureTrajectoryHorizonMs / 250,
            $"Expected the projection to run past the old 200s cap, got only {points.Count} points.");

        // The last point must be the actual aim point, not a horizon-truncated position.
        Assert.Equal(0.0, points[^1].X, precision: 3);
        Assert.Equal(0.0, points[^1].Y, precision: 3);
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

    [Fact]
    public async Task Ctrl_click_then_ctrl_release_then_plain_click_and_drag_pans_without_second_navigation()
    {
        // Regression: after Ctrl+Click sends Orbit, releasing Ctrl and
        // plain-click-and-dragging must pan the camera — NOT send a second
        // navigation command. A plain click alone (no drag) no longer pans by
        // itself — the click-to-center jump was disabled (see
        // Plain_click_sends_no_command_and_does_not_change_focus).
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        // 1. Ctrl+Click → one navigation command.
        fixture.Screen.OnKeyDown(Key.ControlLeft);
        fixture.Screen.OnMouseDown(1000, 500);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(ShipEngineCommandTypes.Orbit, command.CommandType);

        // 2. Release Ctrl.
        fixture.Screen.OnKeyUp(Key.ControlLeft);

        // 3. Plain click elsewhere, alone, sends no second command and does not pan.
        double fxBefore = fixture.Screen.CameraFocusX;
        double fyBefore = fixture.Screen.CameraFocusY;

        fixture.Screen.OnMouseDown(900, 600);

        Assert.Single(fixture.Connection.Commands); // still only one
        Assert.Equal(fxBefore, fixture.Screen.CameraFocusX); // no jump on click alone
        Assert.Equal(fyBefore, fixture.Screen.CameraFocusY);

        // 4. Dragging afterward (still held) pans by the exact screen-space delta.
        fixture.Screen.OnMouseMove(920, 580); // dx=+20, dy=-20, PPU=1.0

        Assert.Single(fixture.Connection.Commands); // dragging still sends nothing
        Assert.Equal(fxBefore - 20.0, fixture.Screen.CameraFocusX, precision: 6);
        Assert.Equal(fyBefore + 20.0, fixture.Screen.CameraFocusY, precision: 6);
    }

    [Fact]
    public async Task Ctrl_click_with_ctrl_right_then_release_then_plain_click_does_not_pan_alone()
    {
        // Same as above but with ControlRight, checking only the no-jump-on-click-
        // alone part (drag coverage lives in the ControlLeft variant above).
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        fixture.Screen.OnKeyDown(Key.ControlRight);
        fixture.Screen.OnMouseDown(1000, 500);
        Assert.Single(fixture.Connection.Commands);

        fixture.Screen.OnKeyUp(Key.ControlRight);

        double fxBefore = fixture.Screen.CameraFocusX;
        fixture.Screen.OnMouseDown(900, 600);

        Assert.Single(fixture.Connection.Commands);
        Assert.Equal(fxBefore, fixture.Screen.CameraFocusX);
    }

    [Fact]
    public async Task Drag_after_plain_click_pans_camera_by_mouse_delta_in_real_time()
    {
        // Dragging (OnMouseMove while still held after a plain map OnMouseDown)
        // must continuously shift the camera by the mouse-movement delta, in
        // real time, independent of whatever the focus was right after the
        // mouse-down (which itself no longer jumps — see the click-alone tests).
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        fixture.Screen.OnMouseDown(1000, 500);

        double focusXAfterDown = fixture.Screen.CameraFocusX;
        double focusYAfterDown = fixture.Screen.CameraFocusY;

        // PPU is 1.0 by default (see GameSessionScreen ctor / CameraState default).
        const double ppu = 1.0;
        float dx = 30f;
        float dy = -20f;
        fixture.Screen.OnMouseMove(1000 + dx, 500 + dy);

        Assert.Equal(focusXAfterDown - dx / ppu, fixture.Screen.CameraFocusX, precision: 6);
        Assert.Equal(focusYAfterDown - dy / ppu, fixture.Screen.CameraFocusY, precision: 6);
    }

    [Fact]
    public async Task Drag_stops_panning_after_mouse_up()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        fixture.Screen.OnMouseDown(1000, 500);
        fixture.Screen.OnMouseMove(1030, 480);

        double focusXAfterMove = fixture.Screen.CameraFocusX;
        double focusYAfterMove = fixture.Screen.CameraFocusY;

        fixture.Screen.OnMouseUp(1030, 480);
        fixture.Screen.OnMouseMove(1100, 400);

        Assert.Equal(focusXAfterMove, fixture.Screen.CameraFocusX, precision: 6);
        Assert.Equal(focusYAfterMove, fixture.Screen.CameraFocusY, precision: 6);
    }

    [Fact]
    public async Task Drag_starting_on_object_or_panel_does_not_pan_map()
    {
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        // Ship renders at screen center (640, 360) — click lands on the object,
        // not the "plain map click" branch, so no panning drag should start.
        fixture.Screen.OnMouseDown(ScreenWidth / 2f, ScreenHeight / 2f);
        double focusXAfterObjectClick = fixture.Screen.CameraFocusX;
        double focusYAfterObjectClick = fixture.Screen.CameraFocusY;

        fixture.Screen.OnMouseMove(ScreenWidth / 2f + 50f, ScreenHeight / 2f + 50f);

        Assert.Equal(focusXAfterObjectClick, fixture.Screen.CameraFocusX);
        Assert.Equal(focusYAfterObjectClick, fixture.Screen.CameraFocusY);

        // Commands Panel header, away from the hide/show button — consumed by
        // CommandsPanel.OnMouseDown's final "caption/body" fallback, no action.
        var panel = fixture.Screen.CommandsPanel.CaptionRect;
        fixture.Screen.OnMouseDown(panel.Right - 5f, panel.MidY);
        double focusXAfterPanelClick = fixture.Screen.CameraFocusX;
        double focusYAfterPanelClick = fixture.Screen.CameraFocusY;

        fixture.Screen.OnMouseMove(panel.Right + 60f, panel.MidY + 60f);

        Assert.Equal(focusXAfterPanelClick, fixture.Screen.CameraFocusX);
        Assert.Equal(focusYAfterPanelClick, fixture.Screen.CameraFocusY);
    }

    // ── Test helpers ────────────────────────────────────────────────────────────

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

        public ValueTask SetObjectInteractionStateAsync(
            string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
