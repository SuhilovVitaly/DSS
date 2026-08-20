using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// ТЗ: ActiveObject and SelectedObject — 30 px hit-testing, nearest/tie-break selection,
/// ActiveObjectId recompute triggers, left/right click rules (incl. Ctrl+Click priority),
/// object-lifecycle cleanup, non-duplicate Engine forwarding, and info-panel wiring.
/// </summary>
public class GameSessionObjectInteractionTests
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const string PlayerShipId = "SPC-0001";
    private const string EngineModuleId = "MOD-PLAYER-ENGINE-01";

    // No object in these fixtures uses this id, so PlayerShipObjectId never matches —
    // UpdateCameraFocusFromPlayer finds nothing to attach to and the camera stays fixed
    // exactly at its construction default (10000,10000), PPU 1.0. That makes screen
    // position predictable: world (10000,10000) renders at the 1280x720 viewport center
    // (640,360), and world (10000+dx, 10000+dy) renders at screen (640+dx, 360+dy).
    private const string NoPlayerShip = "NONE";

    private static ObjectMotionSnapshot ObjAt(string id, double worldY, double worldX = 10000)
        => new(id, X: worldX, Y: worldY, SpeedKmS: 0, Direction: 0);

    // ── Hit-testing radius (AC: 29.99 hits, 30 hits, 30.01 misses) ──────────────

    [Fact]
    public async Task Cursor_just_inside_30px_activates_the_object()
    {
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        Render(fixture.Screen);

        fixture.Screen.OnMouseMove(640, 360 + 29.99f);

        Assert.Equal("OBJ-1", fixture.Screen.ActiveObjectId);
    }

    [Fact]
    public async Task Cursor_exactly_30px_activates_the_object()
    {
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        Render(fixture.Screen);

        fixture.Screen.OnMouseMove(640, 360 + 30f);

        Assert.Equal("OBJ-1", fixture.Screen.ActiveObjectId);
    }

    [Fact]
    public async Task Cursor_just_outside_30px_leaves_ActiveObjectId_null()
    {
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        Render(fixture.Screen);

        fixture.Screen.OnMouseMove(640, 360 + 30.01f);

        Assert.Null(fixture.Screen.ActiveObjectId);
    }

    // ── Nearest object + deterministic tie-break ────────────────────────────────

    [Fact]
    public async Task Nearest_object_wins_regardless_of_declaration_order()
    {
        await using var fixture = CreateFixture([
            ObjAt("OBJ-FAR", 10000 + 20),
            ObjAt("OBJ-NEAR", 10000 + 5)]);
        Render(fixture.Screen);

        fixture.Screen.OnMouseMove(640, 360);

        Assert.Equal("OBJ-NEAR", fixture.Screen.ActiveObjectId);
    }

    [Fact]
    public async Task Tie_break_picks_lexicographically_smaller_object_id()
    {
        // Both exactly 10 px from the cursor — a genuine tie in squared distance.
        await using var fixture = CreateFixture([
            ObjAt("OBJ-B", 10000, worldX: 10010),
            ObjAt("OBJ-A", 10000, worldX: 9990)]);
        Render(fixture.Screen);

        fixture.Screen.OnMouseMove(640, 360);

        Assert.Equal("OBJ-A", fixture.Screen.ActiveObjectId);
    }

    // ── ActiveObjectId recompute triggers ───────────────────────────────────────

    [Fact]
    public async Task Active_transitions_directly_between_objects_and_back_to_null()
    {
        await using var fixture = CreateFixture([
            ObjAt("OBJ-A", 10000, worldX: 9950),  // screen (590, 360)
            ObjAt("OBJ-B", 10000, worldX: 10050)]); // screen (690, 360)
        Render(fixture.Screen);

        Assert.Null(fixture.Screen.ActiveObjectId);

        fixture.Screen.OnMouseMove(590, 360);
        Assert.Equal("OBJ-A", fixture.Screen.ActiveObjectId);

        fixture.Screen.OnMouseMove(690, 360);
        Assert.Equal("OBJ-B", fixture.Screen.ActiveObjectId);

        fixture.Screen.OnMouseMove(0, 0);
        Assert.Null(fixture.Screen.ActiveObjectId);
    }

    [Fact]
    public void Moving_object_under_stationary_cursor_clears_ActiveObjectId()
    {
        // Speed0 (used by the other fixtures) freezes visuals at a paused anchor by
        // design (Modal Pause Rule) — motion needs a running simulation. Uses a fake,
        // fully controllable clock (both SnapshotBuffer's and GameSessionScreen's
        // timestampProvider seams) so client-side motion prediction advances by an
        // exact, deterministic amount instead of racing real wall-clock time.
        var clock = new FakeClock();
        var buffer = new SnapshotBuffer(clock.Now);
        var moving = new ObjectMotionSnapshot("OBJ-1", X: 10000, Y: 10000, SpeedKmS: 50, Direction: 90);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(moving),
            PlayerShipObjectId: NoPlayerShip));

        var screen = new GameSessionScreen(
            buffer, new LinearMotionPredictor(), handle: null, timestampProvider: clock.Now);

        Render(screen);
        screen.OnMouseMove(640, 360);
        Assert.Equal("OBJ-1", screen.ActiveObjectId);

        // 50 km/s × 10 units/s per km/s × 0.2 s = 100 world units of drift.
        clock.AdvanceMs(200);
        Render(screen);

        Assert.Null(screen.ActiveObjectId);
    }

    [Fact]
    public async Task Camera_drag_pan_recomputes_ActiveObjectId()
    {
        // Click alone no longer jumps the camera (disabled — it fought with
        // click-and-drag panning), so triggering a pan now requires a drag
        // (OnMouseDown + OnMouseMove), not OnMouseDown alone.
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]); // at camera focus, screen center
        Render(fixture.Screen);
        fixture.Screen.OnMouseMove(640, 360);
        Assert.Equal("OBJ-1", fixture.Screen.ActiveObjectId);

        // Drag the camera far away — the object's screen position moves well
        // outside the cursor's 30 px hit radius even though the object itself
        // never moved in world space.
        fixture.Screen.OnMouseDown(100, 100);
        fixture.Screen.OnMouseMove(400, 400);
        Render(fixture.Screen);

        Assert.Null(fixture.Screen.ActiveObjectId);
    }

    [Fact]
    public async Task Zoom_change_recomputes_ActiveObjectId_without_a_new_mouse_move()
    {
        // 20 px away at PPU 1.0 (inside the 30 px radius). Zooming IN grows screen
        // distance (distance = world distance × PPU); the wheel handler caps PPU at
        // 2.0, so distance saturates at 20×2.0 = 40 px — outside the radius.
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000 + 20)]);
        Render(fixture.Screen);
        fixture.Screen.OnMouseMove(640, 360);
        Assert.Equal("OBJ-1", fixture.Screen.ActiveObjectId);

        for (int i = 0; i < 5; i++)
            fixture.Screen.OnMouseWheel(640, 360, 1f); // zoom in, saturates at PPU 2.0
        Render(fixture.Screen);

        Assert.Null(fixture.Screen.ActiveObjectId);
    }

    [Fact]
    public async Task Deactivating_the_screen_clears_ActiveObjectId()
    {
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        Render(fixture.Screen);
        fixture.Screen.OnMouseMove(640, 360);
        Assert.Equal("OBJ-1", fixture.Screen.ActiveObjectId);

        fixture.Screen.OnDeactivated();

        Assert.Null(fixture.Screen.ActiveObjectId);
    }

    // ── Hit-testing respects the scale visibility filter ────────────────────────

    [Fact]
    public async Task Object_hidden_by_scale_filter_cannot_become_ActiveObjectId()
    {
        var asteroid = new ObjectMotionSnapshot(
            "AST-1", X: 10000, Y: 10000, SpeedKmS: 0, Direction: 0,
            ObjectType: SpaceObjectType.Asteroid, RenderObjectType: SpaceObjectType.Asteroid);

        await using var fixture = CreateFixture([asteroid]);
        Render(fixture.Screen);
        fixture.Screen.OnMouseMove(640, 360);
        Assert.Equal("AST-1", fixture.Screen.ActiveObjectId);

        // Zoom out below the full-visibility threshold — Asteroid hides from _renderStates.
        for (int i = 0; i < 6; i++)
            fixture.Screen.OnMouseWheel(640, 360, -1f);
        Render(fixture.Screen);

        Assert.Null(fixture.Screen.ActiveObjectId);
    }

    // ── Left click: selection ────────────────────────────────────────────────────

    [Fact]
    public async Task Left_click_on_object_selects_it_without_moving_camera_or_navigating()
    {
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        Render(fixture.Screen);
        double fx = fixture.Screen.CameraFocusX;
        double fy = fixture.Screen.CameraFocusY;

        var result = fixture.Screen.OnMouseDown(640, 360);

        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);
        Assert.Equal(ScreenEvent.None, result);
        Assert.Equal(fx, fixture.Screen.CameraFocusX);
        Assert.Equal(fy, fixture.Screen.CameraFocusY);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Left_click_on_empty_map_does_not_change_SelectedObjectId()
    {
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        Render(fixture.Screen);
        fixture.Screen.OnMouseDown(640, 360);
        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);

        fixture.Screen.OnMouseDown(100, 100); // far from OBJ-1 — empty map, pans camera

        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);
    }

    [Fact]
    public async Task Ctrl_left_click_on_object_selects_instead_of_navigating()
    {
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        Render(fixture.Screen);
        fixture.Screen.OnKeyDown(Key.ControlLeft);

        fixture.Screen.OnMouseDown(640, 360);

        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Ctrl_left_click_on_empty_map_still_sends_navigation_command()
    {
        // Selection priority applies only within the 30 px object radius — free map
        // area keeps the existing Ctrl+Click navigation behavior (regression).
        await using var fixture = CreateFixtureWithPlayerShip();
        Render(fixture.Screen);
        fixture.Screen.OnKeyDown(Key.ControlLeft);

        fixture.Screen.OnMouseDown(1000, 500); // far from the ship at screen center

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(ShipEngineCommandTypes.Orbit, command.CommandType);
        Assert.Null(fixture.Screen.SelectedObjectId);
    }

    // ── Camera follow-on-select (ТЗ: camera focus follows the selected object) ─────

    [Fact]
    public void Selecting_a_non_player_object_makes_camera_follow_it_continuously()
    {
        // Uses a fake, fully controllable clock (same pattern as
        // Moving_object_under_stationary_cursor_clears_ActiveObjectId) and a running
        // simulation (Speed1) so a position change between snapshots isn't held by the
        // paused-visual-anchor freeze, and advances real wall-clock time past the 0.3 s
        // visual-reconciliation smoothing window so the new position is fully applied.
        var clock = new FakeClock();
        var ship = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: 0, Direction: 0);
        var buffer = new SnapshotBuffer(clock.Now);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship, ObjAt("OBJ-1", 10000 + 50)), // screen (640,410)
            PlayerShipObjectId: PlayerShipId));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), handle: null, timestampProvider: clock.Now);

        Render(screen);
        screen.OnMouseDown(640, 410);

        Assert.Equal("OBJ-1", screen.SelectedObjectId);
        Assert.False(screen.IsFocusAttachedToPlayer);
        Assert.Equal("OBJ-1", screen.CameraFollowObjectId);

        clock.AdvanceMs(400);
        Render(screen);
        Assert.Equal(10000, screen.CameraFocusX);
        Assert.Equal(10050, screen.CameraFocusY);

        // OBJ-1 moves to a new position in a fresh snapshot — proves per-frame
        // following, not a one-time jump to where it was at selection time.
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 2,
            GameTimeMs: 1000,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship, ObjAt("OBJ-1", 10000 + 200, worldX: 10000 + 100)),
            PlayerShipObjectId: PlayerShipId));
        clock.AdvanceMs(400);
        Render(screen); // the frame that observes the new snapshot creates a fresh
                         // reconciliation correction (eases in over subsequent frames)
        clock.AdvanceMs(400); // past the 0.3 s reconciliation smoothing window
        Render(screen);

        Assert.Equal(10100, screen.CameraFocusX);
        Assert.Equal(10200, screen.CameraFocusY);
    }

    [Fact]
    public async Task Selecting_the_player_ship_after_following_another_object_reattaches_player_follow()
    {
        await using var fixture = CreateFixtureWithPlayerShipAndObjects([ObjAt("OBJ-1", 10000 + 50)]); // screen (640,410)
        Render(fixture.Screen);

        fixture.Screen.OnMouseDown(640, 410); // select+follow OBJ-1
        Assert.Equal("OBJ-1", fixture.Screen.CameraFollowObjectId);
        Assert.False(fixture.Screen.IsFocusAttachedToPlayer);

        fixture.Screen.OnMouseDown(640, 360); // select the player ship (camera hasn't moved yet)

        Assert.Equal(PlayerShipId, fixture.Screen.SelectedObjectId);
        Assert.True(fixture.Screen.IsFocusAttachedToPlayer);
        Assert.Null(fixture.Screen.CameraFollowObjectId);
    }

    [Fact]
    public async Task CtrlC_from_a_free_map_point_reattaches_player_follow()
    {
        // Pre-existing behavior — must still hold after generalizing camera-follow.
        await using var fixture = CreateFixtureWithPlayerShip();
        Render(fixture.Screen);

        // Off the (now always-visible, 4-group) Commands Panel footprint — a real
        // free map point, not swallowed as a UI-panel click.
        fixture.Screen.OnMouseDown(1000, 500);
        fixture.Screen.OnMouseMove(1200, 700); // drag -> detaches from player
        Assert.False(fixture.Screen.IsFocusAttachedToPlayer);

        fixture.Screen.OnKeyDown(Key.ControlLeft);
        fixture.Screen.OnKeyDown(Key.C);

        Assert.True(fixture.Screen.IsFocusAttachedToPlayer);
        Assert.Null(fixture.Screen.CameraFollowObjectId);
    }

    [Fact]
    public void CtrlC_while_following_a_non_player_object_detaches_and_freezes_at_its_position()
    {
        var clock = new FakeClock();
        var ship = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: 0, Direction: 0);
        var buffer = new SnapshotBuffer(clock.Now);
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship, ObjAt("OBJ-1", 10000 + 50)), // screen (640,410)
            PlayerShipObjectId: PlayerShipId));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), handle: null, timestampProvider: clock.Now);

        Render(screen);
        screen.OnMouseDown(640, 410); // select+follow OBJ-1
        clock.AdvanceMs(400);
        Render(screen);
        Assert.Equal(10000, screen.CameraFocusX);
        Assert.Equal(10050, screen.CameraFocusY);

        screen.OnKeyDown(Key.ControlLeft);
        screen.OnKeyDown(Key.C);

        Assert.Null(screen.CameraFollowObjectId);
        Assert.False(screen.IsFocusAttachedToPlayer);
        // Frozen exactly where OBJ-1 was — not jumped to the player ship at (10000,10000).
        Assert.Equal(10000, screen.CameraFocusX);
        Assert.Equal(10050, screen.CameraFocusY);

        // OBJ-1 keeps moving in a fresh snapshot — camera must NOT follow it anymore.
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 2,
            GameTimeMs: 1000,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: ImmutableArray.Create(ship, ObjAt("OBJ-1", 10000 + 500, worldX: 10000 + 500)),
            PlayerShipObjectId: PlayerShipId));
        clock.AdvanceMs(400);
        Render(screen);

        Assert.Equal(10000, screen.CameraFocusX);
        Assert.Equal(10050, screen.CameraFocusY);
    }

    [Fact]
    public async Task Starting_a_manual_drag_while_following_a_non_player_object_detaches_follow()
    {
        await using var fixture = CreateFixtureWithPlayerShipAndObjects([ObjAt("OBJ-1", 10000 + 50)]); // screen (640,410)
        Render(fixture.Screen);

        fixture.Screen.OnMouseDown(640, 410); // select+follow OBJ-1
        Render(fixture.Screen);
        Assert.Equal("OBJ-1", fixture.Screen.CameraFollowObjectId);
        double focusXBeforeDrag = fixture.Screen.CameraFocusX;
        double focusYBeforeDrag = fixture.Screen.CameraFocusY;

        // Plain map click far from any object (ship and OBJ-1 both render near screen
        // center now that OBJ-1 is followed) and off the (now always-visible, 4-group)
        // Commands Panel footprint starts a drag-pan.
        fixture.Screen.OnMouseDown(1000, 500);
        Assert.Null(fixture.Screen.CameraFollowObjectId);

        fixture.Screen.OnMouseMove(1050, 540); // drag delta (50, 40)

        double ppu = fixture.Screen.CameraPixelsPerWorldUnit;
        double expectedX = focusXBeforeDrag - 50 / ppu;
        double expectedY = focusYBeforeDrag - 40 / ppu;
        Assert.Equal(expectedX, fixture.Screen.CameraFocusX);
        Assert.Equal(expectedY, fixture.Screen.CameraFocusY);

        Render(fixture.Screen); // confirm it does NOT snap back to following OBJ-1
        Assert.Equal(expectedX, fixture.Screen.CameraFocusX);
        Assert.Equal(expectedY, fixture.Screen.CameraFocusY);
    }

    // ── Right click: clear selection ─────────────────────────────────────────────

    [Fact]
    public async Task Right_click_on_object_clears_selection_but_not_active()
    {
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        Render(fixture.Screen);
        fixture.Screen.OnMouseMove(640, 360);
        fixture.Screen.OnMouseDown(640, 360);
        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);
        Assert.Equal("OBJ-1", fixture.Screen.ActiveObjectId);

        var result = fixture.Screen.OnMouseDown(640, 360, MouseButton.Right);

        Assert.Null(fixture.Screen.SelectedObjectId);
        Assert.Equal("OBJ-1", fixture.Screen.ActiveObjectId);
        Assert.Equal(ScreenEvent.None, result);
        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Right_click_on_empty_map_clears_selection_without_moving_camera()
    {
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        Render(fixture.Screen);
        fixture.Screen.OnMouseDown(640, 360);
        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);

        double fx = fixture.Screen.CameraFocusX;
        double fy = fixture.Screen.CameraFocusY;
        // Off the (now always-visible, 4-group) Commands Panel footprint — a real
        // free map point, not swallowed as a UI-panel click.
        fixture.Screen.OnMouseDown(1000, 500, MouseButton.Right);

        Assert.Null(fixture.Screen.SelectedObjectId);
        Assert.Equal(fx, fixture.Screen.CameraFocusX);
        Assert.Equal(fy, fixture.Screen.CameraFocusY);
    }

    [Fact]
    public async Task Right_click_on_ui_panel_does_not_reset_selection()
    {
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        Render(fixture.Screen);
        fixture.Screen.OnMouseDown(640, 360);
        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);

        // A point on the Commands Panel header away from the hide/show button —
        // consumed by CommandsPanel.OnMouseDown's final "caption/body" fallback
        // with no side effect (ТЗ §54: right-click on a UI panel must not clear
        // the selection).
        var panel = fixture.Screen.CommandsPanel.CaptionRect;
        var result = fixture.Screen.OnMouseDown(panel.Right - 5f, panel.MidY, MouseButton.Right);

        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);
        Assert.Equal(ScreenEvent.None, result);
    }

    // ── Object lifecycle ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Object_removed_from_the_snapshot_clears_both_ids()
    {
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        Render(fixture.Screen);
        fixture.Screen.OnMouseMove(640, 360);
        fixture.Screen.OnMouseDown(640, 360);
        Assert.Equal("OBJ-1", fixture.Screen.ActiveObjectId);
        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);

        fixture.Handle.Buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 2,
            GameTimeMs: 1000,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray<ObjectMotionSnapshot>.Empty,
            PlayerShipObjectId: NoPlayerShip));
        Render(fixture.Screen);

        Assert.Null(fixture.Screen.ActiveObjectId);
        Assert.Null(fixture.Screen.SelectedObjectId);
    }

    [Fact]
    public async Task Scale_filter_hides_active_but_selected_survives_while_object_still_exists()
    {
        var asteroid = new ObjectMotionSnapshot(
            "AST-1", X: 10000, Y: 10000, SpeedKmS: 0, Direction: 0,
            ObjectType: SpaceObjectType.Asteroid, RenderObjectType: SpaceObjectType.Asteroid);

        await using var fixture = CreateFixture([asteroid]);
        Render(fixture.Screen);
        fixture.Screen.OnMouseMove(640, 360);
        fixture.Screen.OnMouseDown(640, 360);
        Assert.Equal("AST-1", fixture.Screen.SelectedObjectId);

        for (int i = 0; i < 6; i++)
            fixture.Screen.OnMouseWheel(640, 360, -1f);
        Render(fixture.Screen);

        Assert.Null(fixture.Screen.ActiveObjectId);
        Assert.Equal("AST-1", fixture.Screen.SelectedObjectId); // still exists in the world
    }

    // ── Non-duplicate Engine forwarding ──────────────────────────────────────────

    [Fact]
    public async Task Unchanged_state_does_not_send_duplicate_updates_to_the_engine()
    {
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        Render(fixture.Screen);

        fixture.Screen.OnMouseMove(640, 360);       // Active becomes OBJ-1 — one queued update
        fixture.Screen.OnMouseMove(641, 360);        // still within radius, same nearest object
        fixture.Screen.OnMouseMove(639, 360);        // ditto — no new update either time

        var calls = await WaitForCallsAsync(fixture.Connection, minCount: 1);

        var call = Assert.Single(calls);
        Assert.Equal("OBJ-1", call.ActiveObjectId);
        Assert.Null(call.SelectedObjectId);
    }

    [Fact]
    public async Task Full_state_reaches_the_connection_when_it_actually_changes()
    {
        // The spec explicitly allows coalescing not-yet-sent updates down to the latest
        // full state — so only the FINAL pair is guaranteed to arrive, not every
        // intermediate transition. Assert on the end state, not the call count.
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        Render(fixture.Screen);

        fixture.Screen.OnMouseMove(640, 360);   // Active -> OBJ-1
        fixture.Screen.OnMouseDown(640, 360);   // Selected -> OBJ-1

        var calls = await WaitForCallsAsync(fixture.Connection, minCount: 1);

        Assert.NotEmpty(calls);
        Assert.Equal(("OBJ-1", "OBJ-1"), calls[^1]);
    }

    // ── Info panel wiring ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Info_panel_lines_show_placeholder_then_real_ids()
    {
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        Render(fixture.Screen);

        var before = fixture.Screen.BuildPanelLines(null);
        Assert.Contains(("Active Id", "—"), before);
        Assert.Contains(("Selected Id", "—"), before);

        fixture.Screen.OnMouseMove(640, 360);
        fixture.Screen.OnMouseDown(640, 360);

        var after = fixture.Screen.BuildPanelLines(null);
        Assert.Contains(("Active Id", "OBJ-1"), after);
        Assert.Contains(("Selected Id", "OBJ-1"), after);
    }

    [Fact]
    public async Task Closed_info_panel_does_not_disable_hit_testing_or_engine_sync()
    {
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        // Taller viewport than the file default: with all 4 Commands Panel groups
        // open (fixed PanelBodyHeight per group) the panel's body now extends to
        // ~840px from the top, which would otherwise overlap the bottom-anchored
        // info panel's close button on a 720px-tall canvas.
        Render(fixture.Screen, height: 1000);

        fixture.Screen.OnMouseDown(fixture.Screen.LastCloseRect.MidX, fixture.Screen.LastCloseRect.MidY);
        Assert.False(fixture.Screen.IsPanelVisible);

        // Screen center for the 1280×1000 viewport used by this test (not the file's
        // default 1280×720), where OBJ-1 renders.
        fixture.Screen.OnMouseMove(640, 500);
        Assert.Equal("OBJ-1", fixture.Screen.ActiveObjectId);

        fixture.Screen.OnMouseDown(640, 500);
        Assert.Equal("OBJ-1", fixture.Screen.SelectedObjectId);

        var calls = await WaitForCallsAsync(fixture.Connection, minCount: 1);
        Assert.NotEmpty(calls);
    }

    // ── Review fixes: stale/phantom cursor position, transport retry ───────────────

    [Fact]
    public void No_mouse_move_yet_does_not_phantom_activate_an_object_near_the_origin()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(ObjAt("OBJ-1", 9640, worldX: 9360)), // screen (0,0)
            PlayerShipObjectId: NoPlayerShip));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());

        Render(screen); // no OnMouseMove has ever been called

        Assert.Null(screen.ActiveObjectId);
    }

    [Fact]
    public async Task Cursor_position_outside_viewport_bounds_is_treated_as_no_cursor()
    {
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]);
        Render(fixture.Screen);

        fixture.Screen.OnMouseMove(-10, 360); // outside [0, viewportW]

        Assert.Null(fixture.Screen.ActiveObjectId);
    }

    [Fact]
    public async Task Reactivation_does_not_reuse_stale_mouse_position_until_a_fresh_move()
    {
        // Regression: SkiaWindow/Silk.NET have no mouse-leave signal, and OnActivated
        // previously left the pre-modal cursor position in place — reopening after a
        // modal (GameMenu) closed could reactivate an object under a position the
        // mouse hasn't actually revisited yet.
        await using var fixture = CreateFixture([ObjAt("OBJ-1", 10000)]); // at screen center
        Render(fixture.Screen);
        fixture.Screen.OnMouseMove(640, 360);
        Assert.Equal("OBJ-1", fixture.Screen.ActiveObjectId);

        fixture.Screen.OnDeactivated();
        fixture.Screen.OnActivated();
        Render(fixture.Screen); // no OnMouseMove between reactivation and this render

        Assert.Null(fixture.Screen.ActiveObjectId);
    }

    [Fact]
    public async Task Transport_failure_is_retried_and_the_state_eventually_reaches_the_connection()
    {
        // Regression: a failed background send must not permanently strand the engine
        // on stale ActiveObjectId/SelectedObjectId when the client's local state never
        // changes again afterwards — GameSessionHandle must retry, not log-and-drop.
        var connection = new RecordingConnection();
        connection.FailNextInteractionStateCalls(2);
        await using var handle = new GameSessionHandle(connection);

        handle.UpdateObjectInteractionState("OBJ-1", "OBJ-1");

        var calls = await WaitForCallsAsync(connection, minCount: 1, timeout: TimeSpan.FromSeconds(5));

        var call = Assert.Single(calls);
        Assert.Equal("OBJ-1", call.ActiveObjectId);
        Assert.Equal("OBJ-1", call.SelectedObjectId);
    }

    // ── Test helpers ─────────────────────────────────────────────────────────────

    private static TestFixture CreateFixture(IEnumerable<ObjectMotionSnapshot> objects)
    {
        var connection = new RecordingConnection();
        var handle = new GameSessionHandle(connection);
        handle.Buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: objects.ToImmutableArray(),
            PlayerShipObjectId: NoPlayerShip));

        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        return new TestFixture(connection, handle, screen);
    }

    /// <summary>Ship at (10000,10000) — for tests that need a real navigable player ship.</summary>
    private static TestFixture CreateFixtureWithPlayerShip()
    {
        var connection = new RecordingConnection();
        var handle = new GameSessionHandle(connection);
        var ship = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: 1.0, Direction: 0);
        handle.Buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(ship),
            PlayerShipObjectId: PlayerShipId));

        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        return new TestFixture(connection, handle, screen);
    }

    /// <summary>Ship at (10000,10000) plus extra objects — for camera-follow-on-select tests
    /// that need both a real player ship AND at least one other selectable object.</summary>
    private static TestFixture CreateFixtureWithPlayerShipAndObjects(IEnumerable<ObjectMotionSnapshot> extraObjects)
    {
        var connection = new RecordingConnection();
        var handle = new GameSessionHandle(connection);
        var ship = new ObjectMotionSnapshot(PlayerShipId, 10000, 10000, SpeedKmS: 0, Direction: 0);
        var objects = new List<ObjectMotionSnapshot> { ship };
        objects.AddRange(extraObjects);
        handle.Buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: objects.ToImmutableArray(),
            PlayerShipObjectId: PlayerShipId));

        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        return new TestFixture(connection, handle, screen);
    }

    private static void Render(GameSessionScreen screen, int height = ScreenHeight)
    {
        using var bitmap = new SKBitmap(ScreenWidth, height);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, height);
    }

    private static async Task<List<(string? ActiveObjectId, string? SelectedObjectId)>> WaitForCallsAsync(
        RecordingConnection connection, int minCount, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(2));
        while (DateTime.UtcNow < deadline && connection.GetInteractionStateCallsSnapshot().Count < minCount)
            await Task.Delay(15);

        // Give any (incorrect) extra in-flight sends a chance to land before asserting count.
        await Task.Delay(100);
        return connection.GetInteractionStateCallsSnapshot();
    }

    /// <summary>Deterministic Stopwatch-tick clock for tests that need controlled elapsed time.</summary>
    private sealed class FakeClock
    {
        private long _ticks = Stopwatch.GetTimestamp();

        public long Now() => _ticks;

        public void AdvanceMs(double milliseconds) =>
            _ticks += (long)(milliseconds / 1000.0 * Stopwatch.Frequency);
    }

    private sealed record TestFixture(
        RecordingConnection Connection,
        GameSessionHandle Handle,
        GameSessionScreen Screen) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Handle.DisposeAsync();
    }

    private sealed class RecordingConnection : IGameSessionConnection
    {
        private readonly object _gate = new();
        private readonly List<(string? ActiveObjectId, string? SelectedObjectId)> _interactionStateCalls = [];
        private int _interactionStateFailuresRemaining;

        public List<PlayerCommand> Commands { get; } = [];

        public List<(string? ActiveObjectId, string? SelectedObjectId)> GetInteractionStateCallsSnapshot()
        {
            lock (_gate)
            {
                return [.. _interactionStateCalls];
            }
        }

        /// <summary>The next <paramref name="count"/> calls to SetObjectInteractionStateAsync throw instead of recording.</summary>
        public void FailNextInteractionStateCalls(int count)
        {
            lock (_gate)
            {
                _interactionStateFailuresRemaining = count;
            }
        }

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
            lock (_gate)
            {
                if (_interactionStateFailuresRemaining > 0)
                {
                    _interactionStateFailuresRemaining--;
                    throw new InvalidOperationException("Injected transport failure for testing.");
                }

                _interactionStateCalls.Add((activeObjectId, selectedObjectId));
            }

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
