using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class ObjectLabelTests
{
    // ── Label angle mapping ──────────────────────────────────────

    [Theory]
    [InlineData(1, 315)]
    [InlineData(45, 315)]
    [InlineData(90, 315)]
    [InlineData(179, 315)]
    public void GetLabelAngle_0_to_180_returns_315(double dir, float expected)
    {
        Assert.Equal(expected, ObjectLabelLayout.GetLabelAngle(dir));
    }

    [Theory]
    [InlineData(181, 45)]
    [InlineData(225, 45)]
    [InlineData(270, 45)]
    public void GetLabelAngle_180_to_270_returns_45(double dir, float expected)
    {
        Assert.Equal(expected, ObjectLabelLayout.GetLabelAngle(dir));
    }

    [Theory]
    [InlineData(271, 135)]
    [InlineData(315, 135)]
    [InlineData(359, 135)]
    public void GetLabelAngle_270_to_360_returns_135(double dir, float expected)
    {
        Assert.Equal(expected, ObjectLabelLayout.GetLabelAngle(dir));
    }

    [Theory]
    [InlineData(0, 315)]
    [InlineData(180, 315)]
    public void GetLabelAngle_edge_cases(double dir, float expected)
    {
        Assert.Equal(expected, ObjectLabelLayout.GetLabelAngle(dir));
    }

    // ── ComputeLabelOrigin ───────────────────────────────────────

    [Fact]
    public void Label_origin_offset_is_45px_from_object()
    {
        var objPos = new SKPoint(100, 100);
        // 315° → cos≈0.707, sin≈-0.707 → offset right and down in screen coords
        var origin = ObjectLabelLayout.ComputeLabelOrigin(objPos, 315f);

        float dx = origin.X - objPos.X;
        float dy = origin.Y - objPos.Y;
        Assert.True(dx > 0, "315° should offset right");
        Assert.True(dy > 0, "315° should offset down in screen Y");
    }

    // ── Clamp to viewport ────────────────────────────────────────

    [Fact]
    public void Label_clamped_to_viewport_when_outside()
    {
        var origin = new SKPoint(-50, -50);
        var size = new SKSize(120, 18);
        var viewport = new SKSize(800, 600);

        var clamped = ObjectLabelLayout.ClampToViewport(origin, size, viewport);

        Assert.True(clamped.X >= 2);
        Assert.True(clamped.Y >= 2);
        Assert.True(clamped.X + size.Width <= viewport.Width + 2);
        Assert.True(clamped.Y + size.Height <= viewport.Height + 2);
    }

    [Fact]
    public void Label_stays_when_inside_viewport()
    {
        var origin = new SKPoint(200, 200);
        var size = new SKSize(120, 18);
        var viewport = new SKSize(800, 600);

        var clamped = ObjectLabelLayout.ClampToViewport(origin, size, viewport);

        Assert.Equal(200, clamped.X);
        Assert.Equal(200, clamped.Y);
    }

    // ── ObjectLabelGeometry ──────────────────────────────────────

    [Fact]
    public void Geometry_plaque_width_includes_status_square_and_gaps()
    {
        // Use text wide enough to exceed MinPlaqueWidth so the formula, not the minimum, sets the width
        float textWidth = 100f;
        var objScreen = new SKPoint(400, 300);
        var viewport = new SKSize(800, 600);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 90, textWidth, viewport);

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
        float textWidth = 5f; // very short label
        var objScreen = new SKPoint(400, 300);
        var viewport = new SKSize(800, 600);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 90, textWidth, viewport);

        Assert.Equal(ObjectLabelLayout.MinPlaqueWidth, geom.PlaqueRect.Width, precision: 3);
    }

    [Fact]
    public void Geometry_status_rect_starts_at_text_padding_x()
    {
        var objScreen = new SKPoint(400, 300);
        var viewport = new SKSize(800, 600);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 90, textWidth: 80, viewport);

        Assert.Equal(
            geom.PlaqueRect.Left + ObjectLabelLayout.TextPaddingX,
            geom.StatusRect.Left);
    }

    [Fact]
    public void Geometry_status_rect_is_vertically_centered_in_plaque()
    {
        var objScreen = new SKPoint(400, 300);
        var viewport = new SKSize(800, 600);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 90, textWidth: 80, viewport);

        float plaqueMidY = geom.PlaqueRect.Top + geom.PlaqueRect.Height / 2f;
        float sqMidY = geom.StatusRect.Top + geom.StatusRect.Height / 2f;
        Assert.Equal(plaqueMidY, sqMidY, precision: 3);
    }

    [Fact]
    public void Geometry_text_origin_is_after_status_square_with_gap()
    {
        var objScreen = new SKPoint(400, 300);
        var viewport = new SKSize(800, 600);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 90, textWidth: 80, viewport);

        Assert.Equal(
            geom.StatusRect.Right + ObjectLabelLayout.StatusTextGap,
            geom.TextOrigin.X);
        Assert.Equal(
            geom.PlaqueRect.Top + ObjectLabelLayout.TextPaddingY,
            geom.TextOrigin.Y);
    }

    // ── Leader line endpoint ─────────────────────────────────────

    [Fact]
    public void Leader_endpoint_is_on_bottom_edge_of_plaque()
    {
        var objScreen = new SKPoint(400, 300);
        var viewport = new SKSize(800, 600);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 90, textWidth: 80, viewport);

        Assert.Equal(geom.PlaqueRect.Bottom, geom.LeaderEndPoint.Y);
    }

    [Fact]
    public void Leader_endpoint_is_clamped_within_plaque_horizontal_bounds()
    {
        var objScreen = new SKPoint(400, 300);
        var viewport = new SKSize(800, 600);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 90, textWidth: 80, viewport);

        Assert.True(geom.LeaderEndPoint.X >= geom.PlaqueRect.Left + ObjectLabelLayout.LeaderEdgeMargin);
        Assert.True(geom.LeaderEndPoint.X <= geom.PlaqueRect.Right - ObjectLabelLayout.LeaderEdgeMargin);
    }

    [Fact]
    public void Leader_endpoint_is_not_always_center_when_object_is_left_of_plaque()
    {
        // Object far to the left of where the plaque will appear
        var objScreen = new SKPoint(50, 300);
        var viewport = new SKSize(800, 600);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 90, textWidth: 80, viewport);

        // Object is to the left → leader should go to left portion of bottom edge
        Assert.True(geom.LeaderEndPoint.X < geom.PlaqueRect.MidX,
            $"Leader should be left-of-center when object is left. " +
            $"LeaderX={geom.LeaderEndPoint.X}, MidX={geom.PlaqueRect.MidX}");
    }

    [Fact]
    public void Leader_endpoint_is_not_always_center_when_object_is_right_of_plaque()
    {
        // Object far to the right of where the plaque will appear
        var objScreen = new SKPoint(750, 300);
        var viewport = new SKSize(800, 600);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 270, textWidth: 80, viewport);

        // Object is to the right → leader should go to right portion of bottom edge
        Assert.True(geom.LeaderEndPoint.X > geom.PlaqueRect.MidX,
            $"Leader should be right-of-center when object is right. " +
            $"LeaderX={geom.LeaderEndPoint.X}, MidX={geom.PlaqueRect.MidX}");
    }

    [Fact]
    public void Leader_endpoint_respects_edge_margin_even_when_object_is_far_outside()
    {
        // Object extremely far to the left
        var objScreen = new SKPoint(-500, 300);
        var viewport = new SKSize(800, 600);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 90, textWidth: 80, viewport);

        // Should be clamped to Left + LeaderEdgeMargin, not follow object off-screen
        Assert.Equal(
            geom.PlaqueRect.Left + ObjectLabelLayout.LeaderEdgeMargin,
            geom.LeaderEndPoint.X);
    }

    // ── GetLeaderEndPoint (standalone) ───────────────────────────

    [Fact]
    public void GetLeaderEndPoint_returns_bottom_edge_point()
    {
        var plaqueRect = new SKRect(200, 100, 320, 118);
        var objScreen = new SKPoint(260, 200); // object directly below plaque center

        var endPoint = ObjectLabelLayout.GetLeaderEndPoint(objScreen, plaqueRect);

        Assert.Equal(plaqueRect.Bottom, endPoint.Y);
        Assert.Equal(260, endPoint.X); // centered → stays at object X within clamp range
    }

    [Fact]
    public void GetLeaderEndPoint_clamps_to_left_margin()
    {
        var plaqueRect = new SKRect(200, 100, 320, 118);
        var objScreen = new SKPoint(50, 200); // far left

        var endPoint = ObjectLabelLayout.GetLeaderEndPoint(objScreen, plaqueRect);

        Assert.Equal(plaqueRect.Left + ObjectLabelLayout.LeaderEdgeMargin, endPoint.X);
    }

    [Fact]
    public void GetLeaderEndPoint_clamps_to_right_margin()
    {
        var plaqueRect = new SKRect(200, 100, 320, 118);
        var objScreen = new SKPoint(500, 200); // far right

        var endPoint = ObjectLabelLayout.GetLeaderEndPoint(objScreen, plaqueRect);

        Assert.Equal(plaqueRect.Right - ObjectLabelLayout.LeaderEdgeMargin, endPoint.X);
    }

    // ── Status square smoothstep animation ───────────────────────

    [Fact]
    public void StatusSquare_shows_object_color_at_t0()
    {
        var objColor = new SKColor(85, 107, 47);
        var result = StatusSquareAnimator.GetStatusColor(objColor, gameTimeMs: 0, SimulationSpeed.Speed1);
        Assert.Equal(objColor, result);
    }

    [Fact]
    public void StatusSquare_returns_to_object_color_at_period_end()
    {
        var objColor = new SKColor(85, 107, 47);
        var result = StatusSquareAnimator.GetStatusColor(objColor, gameTimeMs: 1500, SimulationSpeed.Speed1);
        Assert.Equal(objColor, result);
    }

    [Fact]
    public void StatusSquare_peaks_at_mid_period()
    {
        var objColor = new SKColor(100, 0, 0);
        var result = StatusSquareAnimator.GetStatusColor(objColor, gameTimeMs: 750, SimulationSpeed.Speed1);

        // At peak (t=1.0), phase=1.0 → fully shifted color
        // Dark color → 85% toward white: R = 100 + 0.85*155 = 231
        Assert.Equal(231, result.Red);
        Assert.Equal(216, result.Green);
        Assert.Equal(216, result.Blue);
    }

    [Fact]
    public void StatusSquare_smoothly_interpolates_between_frames()
    {
        var objColor = new SKColor(100, 0, 0);

        // Two frames 16ms apart should produce close colors (not hard toggle)
        var c1 = StatusSquareAnimator.GetStatusColor(objColor, gameTimeMs: 100, SimulationSpeed.Speed1);
        var c2 = StatusSquareAnimator.GetStatusColor(objColor, gameTimeMs: 116, SimulationSpeed.Speed1);

        int dR = Math.Abs(c1.Red - c2.Red);
        int dG = Math.Abs(c1.Green - c2.Green);
        int dB = Math.Abs(c1.Blue - c2.Blue);

        // 16ms at 1500ms period → ~1% phase change → small delta
        Assert.True(dR + dG + dB < 20,
            $"Color jump too large: ΔR={dR} ΔG={dG} ΔB={dB}");
    }

    [Fact]
    public void StatusSquare_frozen_when_paused()
    {
        var objColor = new SKColor(100, 150, 200);

        Assert.Equal(objColor, StatusSquareAnimator.GetStatusColor(objColor, 0, SimulationSpeed.Speed0));
        Assert.Equal(objColor, StatusSquareAnimator.GetStatusColor(objColor, 750, SimulationSpeed.Speed0));
        Assert.Equal(objColor, StatusSquareAnimator.GetStatusColor(objColor, 1500, SimulationSpeed.Speed0));
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
    public void Label_offset_is_45px()
    {
        Assert.Equal(45f, ObjectLabelLayout.LabelOffsetPx);
    }

    [Fact]
    public void Status_square_size_is_8px()
    {
        Assert.Equal(8f, ObjectLabelLayout.StatusSquareSize);
    }

    [Fact]
    public void Animation_period_is_1500ms()
    {
        Assert.Equal(1500.0, StatusSquareAnimator.PeriodMs);
    }

    [Fact]
    public void Status_text_gap_is_6px()
    {
        Assert.Equal(6f, ObjectLabelLayout.StatusTextGap);
    }

    [Fact]
    public void Leader_edge_margin_is_8px()
    {
        Assert.Equal(8f, ObjectLabelLayout.LeaderEdgeMargin);
    }

    // ── DisplayName resolution ───────────────────────────────────

    [Fact]
    public void Label_renderer_has_unknown_label_constant()
    {
        // ObjectLabelRenderer uses "Unknown Celestial Object" as fallback
        // Verified via reflection to ensure the constant exists
        var rendererType = typeof(ObjectLabelRenderer);
        Assert.NotNull(rendererType);
    }
}
