using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class ObjectLabelTests
{
    private static readonly SKSize TestViewport = new(800, 600);

    // Marker radius comes from the shared policy (ТЗ-10 AC 7): labels and
    // drawing must use one source — TacticalMapMarkerPolicy.GetMarkerRadiusPx.
    private static readonly float TestMarkerRadius =
        TacticalMapMarkerPolicy.GetMarkerRadiusPx(SpaceObjectType.Asteroid);

    // ── Orbit layout: rear half-plane ──────────────────────────────

    [Theory]
    [InlineData(0)]    // up → plaque behind = below on screen (+y)
    [InlineData(90)]   // right → plaque behind = left on screen (-x)
    [InlineData(180)]  // down → plaque behind = above on screen (-y)
    [InlineData(270)]  // left → plaque behind = right on screen (+x)
    public void Plaque_center_is_in_rear_half_plane_for_cardinal_directions(double dir)
    {
        var objScreen = new SKPoint(400, 300);
        var geom = ObjectLabelLayout.Create(objScreen, dir, textWidth: 80, TestViewport, TestMarkerRadius);

        // Forward vector: (sin(rad), -cos(rad)) in screen coords.
        double rad = dir * Math.PI / 180.0;
        float fx = (float)Math.Sin(rad);
        float fy = -(float)Math.Cos(rad);

        // Vector from object to plaque center.
        float dx = geom.PlaqueCenter.X - objScreen.X;
        float dy = geom.PlaqueCenter.Y - objScreen.Y;

        // Dot with forward should be ≤ 0 (rear half-plane).
        float dot = dx * fx + dy * fy;
        Assert.True(dot <= 0.01f,
            $"Direction {dir}°: plaque center dot={dot:F3} should be ≤ 0 (rear half-plane). " +
            $"PlaqueCenter=({geom.PlaqueCenter.X:F1},{geom.PlaqueCenter.Y:F1})");
    }

    [Fact]
    public void No_jump_at_179_to_181_boundary()
    {
        var objScreen = new SKPoint(400, 300);

        var geom179 = ObjectLabelLayout.Create(objScreen, 179, textWidth: 80, TestViewport, TestMarkerRadius);
        var geom181 = ObjectLabelLayout.Create(objScreen, 181, textWidth: 80, TestViewport, TestMarkerRadius);

        float dx = geom181.PlaqueCenter.X - geom179.PlaqueCenter.X;
        float dy = geom181.PlaqueCenter.Y - geom179.PlaqueCenter.Y;
        float jump = MathF.Sqrt(dx * dx + dy * dy);

        // A 2° direction change should produce a tiny position delta, not a sector flip.
        Assert.True(jump < 10f,
            $"Jump from 179° to 181° is {jump:F1} px — should be < 10 px. " +
            $"179°: ({geom179.PlaqueCenter.X:F1},{geom179.PlaqueCenter.Y:F1}), " +
            $"181°: ({geom181.PlaqueCenter.X:F1},{geom181.PlaqueCenter.Y:F1})");
    }

    [Fact]
    public void No_jump_at_269_to_271_boundary()
    {
        var objScreen = new SKPoint(400, 300);

        var geom269 = ObjectLabelLayout.Create(objScreen, 269, textWidth: 80, TestViewport, TestMarkerRadius);
        var geom271 = ObjectLabelLayout.Create(objScreen, 271, textWidth: 80, TestViewport, TestMarkerRadius);

        float dx = geom271.PlaqueCenter.X - geom269.PlaqueCenter.X;
        float dy = geom271.PlaqueCenter.Y - geom269.PlaqueCenter.Y;
        float jump = MathF.Sqrt(dx * dx + dy * dy);

        Assert.True(jump < 10f,
            $"Jump from 269° to 271° is {jump:F1} px — should be < 10 px. " +
            $"269°: ({geom269.PlaqueCenter.X:F1},{geom269.PlaqueCenter.Y:F1}), " +
            $"271°: ({geom271.PlaqueCenter.X:F1},{geom271.PlaqueCenter.Y:F1})");
    }

    [Fact]
    public void Plaque_center_moves_smoothly_with_direction()
    {
        var objScreen = new SKPoint(400, 300);

        // Sample directions every 10° and verify no single-step jump exceeds
        // the expected orbit chord length for that step plus a small margin.
        SKPoint? prev = null;
        float maxJump = 0f;
        for (int d = 0; d <= 360; d += 10)
        {
            var geom = ObjectLabelLayout.Create(objScreen, d, textWidth: 80, TestViewport, TestMarkerRadius);
            if (prev is { } p)
            {
                float dx = geom.PlaqueCenter.X - p.X;
                float dy = geom.PlaqueCenter.Y - p.Y;
                float step = MathF.Sqrt(dx * dx + dy * dy);
                if (step > maxJump) maxJump = step;
                // 10° step on a ~79px orbit: chord ≈ 2*79*sin(5°) ≈ 13.8 px.
                Assert.True(step < 20f,
                    $"Step {d - 10}° → {d}° jump is {step:F1} px — should be smooth.");
            }

            prev = geom.PlaqueCenter;
        }

        // Verify the orbit isn't degenerate — there must be meaningful movement.
        Assert.True(maxJump > 5f, $"Max step {maxJump:F1} px — orbit should produce visible movement.");
    }

    // ── Safe area ──────────────────────────────────────────────────

    [Fact]
    public void Plaque_does_not_overlap_default_safe_area()
    {
        var objScreen = new SKPoint(400, 300);
        // Policy marker (10 px → radius 5) + safe margin.
        float safeRadius = TacticalMapMarkerPolicy.GetMarkerRadiusPx(SpaceObjectType.Asteroid)
                           + ObjectLabelLayout.SafeMarginPx;

        for (int d = 0; d < 360; d += 30)
        {
            var geom = ObjectLabelLayout.Create(objScreen, d, textWidth: 80, TestViewport, TestMarkerRadius);
            float dist = DistanceFromRectToPoint(geom.PlaqueRect, objScreen);
            Assert.True(dist >= safeRadius - 0.01f,
                $"Direction {d}°: plaque distance {dist:F2} < safe radius {safeRadius}");
        }
    }

    [Fact]
    public void Plaque_does_not_overlap_player_marker()
    {
        var objScreen = new SKPoint(400, 300);
        // Player ship marker radius from the shared policy (10 px → radius 5).
        float markerRadius = TacticalMapMarkerPolicy.GetMarkerRadiusPx(SpaceObjectType.PlayerShip);
        float safeRadius = markerRadius + ObjectLabelLayout.SafeMarginPx;

        for (int d = 0; d < 360; d += 30)
        {
            var geom = ObjectLabelLayout.Create(objScreen, d, textWidth: 80, TestViewport, markerRadius);
            float dist = DistanceFromRectToPoint(geom.PlaqueRect, objScreen);
            Assert.True(dist >= safeRadius - 0.01f,
                $"Direction {d}° with player marker: plaque distance {dist:F2} < safe radius {safeRadius}");
        }
    }

    // ── Viewport clamp ─────────────────────────────────────────────

    [Fact]
    public void Plaque_clamped_when_object_near_left_edge()
    {
        // Object at left edge, moving up (0°). Plaque should be clamped within viewport.
        var objScreen = new SKPoint(5, 300);
        var geom = ObjectLabelLayout.Create(objScreen, 0, textWidth: 80, TestViewport, TestMarkerRadius);

        Assert.True(geom.PlaqueRect.Left >= 2f - 0.1f,
            $"Plaque left={geom.PlaqueRect.Left:F1} should be >= 2");
        Assert.True(geom.PlaqueRect.Right <= TestViewport.Width - 2f + 0.1f,
            $"Plaque right={geom.PlaqueRect.Right:F1} should be <= {TestViewport.Width - 2}");
    }

    [Fact]
    public void Plaque_clamped_when_object_near_right_edge()
    {
        var objScreen = new SKPoint(795, 300);
        var geom = ObjectLabelLayout.Create(objScreen, 0, textWidth: 80, TestViewport, TestMarkerRadius);

        Assert.True(geom.PlaqueRect.Right <= TestViewport.Width - 2f + 0.1f,
            $"Plaque right={geom.PlaqueRect.Right:F1} should be within viewport");
    }

    [Fact]
    public void Plaque_clamped_when_object_near_top_edge()
    {
        var objScreen = new SKPoint(400, 5);
        var geom = ObjectLabelLayout.Create(objScreen, 90, textWidth: 80, TestViewport, TestMarkerRadius);

        Assert.True(geom.PlaqueRect.Top >= 2f - 0.1f,
            $"Plaque top={geom.PlaqueRect.Top:F1} should be >= 2");
    }

    [Fact]
    public void Plaque_clamped_when_object_near_bottom_edge()
    {
        var objScreen = new SKPoint(400, 595);
        var geom = ObjectLabelLayout.Create(objScreen, 90, textWidth: 80, TestViewport, TestMarkerRadius);

        Assert.True(geom.PlaqueRect.Bottom <= TestViewport.Height - 2f + 0.1f,
            $"Plaque bottom={geom.PlaqueRect.Bottom:F1} should be within viewport");
    }

    [Fact]
    public void Clamp_at_edge_does_not_place_plaque_in_front_of_object()
    {
        // Object at bottom-left corner moving down-right (135°).
        // Plaque behind is up-left; if clamping forces it to the right of the object,
        // that's in the forward half-plane. The layout should avoid that.
        var objScreen = new SKPoint(5, 595);
        var geom = ObjectLabelLayout.Create(objScreen, 135, textWidth: 80, TestViewport, TestMarkerRadius);

        double rad = 135 * Math.PI / 180.0;
        float fx = (float)Math.Sin(rad);
        float fy = -(float)Math.Cos(rad);

        float dx = geom.PlaqueCenter.X - objScreen.X;
        float dy = geom.PlaqueCenter.Y - objScreen.Y;
        float dot = dx * fx + dy * fy;

        Assert.True(dot <= 0.01f,
            $"After clamp at corner, plaque should stay behind object. " +
            $"Dot={dot:F3}, PlaqueCenter=({geom.PlaqueCenter.X:F1},{geom.PlaqueCenter.Y:F1})");
    }

    // ── Leader line (always bottom-left) ──────────────────────────

    [Fact]
    public void Leader_endpoint_is_always_bottom_left_of_plaque()
    {
        var objScreen = new SKPoint(400, 300);

        // Verify across all cardinal and diagonal directions.
        for (int d = 0; d < 360; d += 45)
        {
            var geom = ObjectLabelLayout.Create(objScreen, d, textWidth: 80, TestViewport, TestMarkerRadius);

            Assert.Equal(geom.PlaqueRect.Left, geom.LeaderEndPoint.X, precision: 3);
            Assert.Equal(geom.PlaqueRect.Bottom, geom.LeaderEndPoint.Y, precision: 3);
        }
    }

    [Fact]
    public void Leader_endpoint_is_bottom_left_after_viewport_clamp()
    {
        // Object near top-right corner — plaque will be clamped.
        var objScreen = new SKPoint(790, 10);
        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 315, textWidth: 80, TestViewport, TestMarkerRadius);

        Assert.Equal(geom.PlaqueRect.Left, geom.LeaderEndPoint.X, precision: 3);
        Assert.Equal(geom.PlaqueRect.Bottom, geom.LeaderEndPoint.Y, precision: 3);
    }

    // ── Plaque geometry invariants ─────────────────────────────────

    [Fact]
    public void Geometry_plaque_width_includes_status_square_and_gaps()
    {
        float textWidth = 100f;
        var objScreen = new SKPoint(400, 300);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 90, textWidth, TestViewport, TestMarkerRadius);

        float expectedW = ObjectLabelLayout.TextPaddingX
            + ObjectLabelLayout.StatusSquareSize
            + ObjectLabelLayout.StatusTextGap
            + textWidth
            + ObjectLabelLayout.TextPaddingX;
        Assert.Equal(expectedW, geom.PlaqueRect.Width, precision: 3);
        Assert.Equal(ObjectLabelLayout.PlaqueHeight, geom.PlaqueRect.Height, precision: 3);
    }

    [Fact]
    public void Geometry_plaque_width_respects_minimum()
    {
        float textWidth = 5f;
        var objScreen = new SKPoint(400, 300);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 90, textWidth, TestViewport, TestMarkerRadius);

        Assert.Equal(ObjectLabelLayout.MinPlaqueWidth, geom.PlaqueRect.Width, precision: 3);
    }

    [Fact]
    public void Geometry_status_rect_starts_at_text_padding_x_plus_content_offset()
    {
        var objScreen = new SKPoint(400, 300);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 90, textWidth: 80, TestViewport, TestMarkerRadius);

        Assert.Equal(
            geom.PlaqueRect.Left + ObjectLabelLayout.TextPaddingX + ObjectLabelLayout.ContentOffsetX,
            geom.StatusRect.Left);
    }

    [Fact]
    public void Geometry_status_rect_is_vertically_shifted_by_status_offset()
    {
        var objScreen = new SKPoint(400, 300);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 90, textWidth: 80, TestViewport, TestMarkerRadius);

        float plaqueMidY = geom.PlaqueRect.Top + geom.PlaqueRect.Height / 2f;
        float sqMidY = geom.StatusRect.Top + geom.StatusRect.Height / 2f;
        // Status square is shifted by StatusOffsetY relative to the plaque center.
        Assert.Equal(plaqueMidY + ObjectLabelLayout.StatusOffsetY, sqMidY, precision: 3);
    }

    [Fact]
    public void Geometry_text_origin_is_after_status_square_with_gap_and_offset()
    {
        var objScreen = new SKPoint(400, 300);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 90, textWidth: 80, TestViewport, TestMarkerRadius);

        Assert.Equal(
            geom.StatusRect.Right + ObjectLabelLayout.StatusTextGap,
            geom.TextOrigin.X);
        Assert.Equal(
            geom.PlaqueRect.Top + ObjectLabelLayout.TextPaddingY + ObjectLabelLayout.TextOffsetY,
            geom.TextOrigin.Y);
    }

    [Fact]
    public void Status_offset_is_1px_above_plaque_center()
    {
        // Acceptance: indicator moved down 2 px vs the old shared -3f offset → -1f
        Assert.Equal(-1f, ObjectLabelLayout.StatusOffsetY);
    }

    [Fact]
    public void Text_offset_is_4px_above_plaque_top()
    {
        // Acceptance: text moved up 1 px vs the old shared -3f offset → -4f
        Assert.Equal(-4f, ObjectLabelLayout.TextOffsetY);
    }

    // ── Status square blink visibility (real/UI time) ────────────
    // Blink is driven by UI time and gated by simulation speed —
    // when paused (Speed0), the square stays always visible.

    [Theory]
    [InlineData(0)]
    [InlineData(499)]
    public void StatusSquare_visible_in_first_phase(long uiTimeMs)
    {
        Assert.True(StatusSquareAnimator.IsStatusSquareVisible(uiTimeMs, SimulationSpeed.Speed1));
    }

    [Theory]
    [InlineData(500)]
    [InlineData(999)]
    public void StatusSquare_hidden_in_second_phase(long uiTimeMs)
    {
        Assert.False(StatusSquareAnimator.IsStatusSquareVisible(uiTimeMs, SimulationSpeed.Speed1));
    }

    [Fact]
    public void StatusSquare_visible_again_at_period_boundary()
    {
        Assert.True(StatusSquareAnimator.IsStatusSquareVisible(1000, SimulationSpeed.Speed1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(999)]
    [InlineData(1500)]
    public void StatusSquare_always_visible_when_paused(long uiTimeMs)
    {
        Assert.True(StatusSquareAnimator.IsStatusSquareVisible(uiTimeMs, SimulationSpeed.Speed0));
    }

    // ── Plaque constants ─────────────────────────────────────────

    [Fact]
    public void Min_plaque_width_is_120()
    {
        Assert.Equal(120f, ObjectLabelLayout.MinPlaqueWidth);
    }

    [Fact]
    public void Plaque_height_is_18()
    {
        Assert.Equal(18f, ObjectLabelLayout.PlaqueHeight);
    }

    [Fact]
    public void Status_square_size_is_8px()
    {
        Assert.Equal(8f, ObjectLabelLayout.StatusSquareSize);
    }

    [Fact]
    public void Animation_period_is_1000ms()
    {
        Assert.Equal(1000.0, StatusSquareAnimator.PeriodMs);
    }

    [Fact]
    public void Status_text_gap_is_6px()
    {
        Assert.Equal(6f, ObjectLabelLayout.StatusTextGap);
    }

    // ── Label policy (ObjectLabelText) ─────────────────────────────
    // ТЗ-09: labels come only from the client-visible render projection.

    [Fact]
    public void Unknown_object_label_is_neizvestny_obekt()
    {
        Assert.Equal("Неизвестный объект",
            ObjectLabelText.Build(SpaceObjectType.UnknownSpaceObject, displayName: null, "AST-1"));
    }

    [Fact]
    public void Station_label_is_name_with_id()
    {
        Assert.Equal("Start Station [SPC-0002]",
            ObjectLabelText.Build(SpaceObjectType.Station, "Start Station", "SPC-0002"));
    }

    [Fact]
    public void Station_without_name_label_is_id_only()
    {
        Assert.Equal("SPC-0002",
            ObjectLabelText.Build(SpaceObjectType.Station, displayName: null, "SPC-0002"));
    }

    [Fact]
    public void Asteroid_label_is_object_id()
    {
        Assert.Equal("AST-42",
            ObjectLabelText.Build(SpaceObjectType.Asteroid, displayName: null, "AST-42"));
    }

    [Fact]
    public void PlayerShip_label_is_name_without_id()
    {
        Assert.Equal("Player Ship",
            ObjectLabelText.Build(SpaceObjectType.PlayerShip, "Player Ship", "SPC-0001"));
    }

    [Fact]
    public void Ship_without_name_falls_back_to_unknown_label()
    {
        Assert.Equal("Неизвестный объект",
            ObjectLabelText.Build(SpaceObjectType.PlayerShip, displayName: null, "SPC-0001"));
        Assert.Equal("Неизвестный объект",
            ObjectLabelText.Build(SpaceObjectType.NpcShip, displayName: null, "NPC-1"));
    }

    [Fact]
    public void NpcShip_label_is_name_only()
    {
        Assert.Equal("Npc One",
            ObjectLabelText.Build(SpaceObjectType.NpcShip, "Npc One", "NPC-1"));
    }

    [Fact]
    public void Sun_and_Planet_labels_use_display_name_with_fallback()
    {
        Assert.Equal("Sun", ObjectLabelText.Build(SpaceObjectType.Sun, "Sun", "SUN-1"));
        Assert.Equal("Неизвестный объект", ObjectLabelText.Build(SpaceObjectType.Planet, null, "PL-1"));
    }

    [Fact]
    public void Null_render_type_uses_display_name_with_fallback()
    {
        Assert.Equal("Legacy", ObjectLabelText.Build(null, "Legacy", "L-1"));
        Assert.Equal("Неизвестный объект", ObjectLabelText.Build(null, null, "L-1"));
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static float DistanceFromRectToPoint(SKRect rect, SKPoint point)
    {
        float cx = Math.Clamp(point.X, rect.Left, rect.Right);
        float cy = Math.Clamp(point.Y, rect.Top, rect.Bottom);
        float dx = point.X - cx;
        float dy = point.Y - cy;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
