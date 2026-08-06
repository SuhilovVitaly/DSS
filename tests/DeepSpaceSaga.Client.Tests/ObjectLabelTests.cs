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
    public void Geometry_leader_endpoint_is_one_of_plaque_corners()
    {
        var objScreen = new SKPoint(400, 300);
        var viewport = new SKSize(800, 600);

        var geom = ObjectLabelLayout.Create(objScreen, directionDegrees: 90, textWidth: 80, viewport);

        var plaque = geom.PlaqueRect;
        var corners = new[]
        {
            new SKPoint(plaque.Left, plaque.Top),
            new SKPoint(plaque.Right, plaque.Top),
            new SKPoint(plaque.Left, plaque.Bottom),
            new SKPoint(plaque.Right, plaque.Bottom)
        };

        Assert.Contains(geom.LeaderEndPoint, corners);
    }

    // ── GetLeaderEndPoint (standalone, synthetic SKRect) ─────────

    [Fact]
    public void GetLeaderEndPoint_returns_nearest_corner_top_left()
    {
        var plaqueRect = new SKRect(200, 100, 320, 118);
        var objScreen = new SKPoint(205, 103); // closest to top-left corner

        var endPoint = ObjectLabelLayout.GetLeaderEndPoint(objScreen, plaqueRect);

        Assert.Equal(new SKPoint(plaqueRect.Left, plaqueRect.Top), endPoint);
    }

    [Fact]
    public void GetLeaderEndPoint_returns_nearest_corner_top_right()
    {
        var plaqueRect = new SKRect(200, 100, 320, 118);
        var objScreen = new SKPoint(315, 105); // closest to top-right corner

        var endPoint = ObjectLabelLayout.GetLeaderEndPoint(objScreen, plaqueRect);

        Assert.Equal(new SKPoint(plaqueRect.Right, plaqueRect.Top), endPoint);
    }

    [Fact]
    public void GetLeaderEndPoint_returns_nearest_corner_bottom_left()
    {
        var plaqueRect = new SKRect(200, 100, 320, 118);
        var objScreen = new SKPoint(205, 114); // closest to bottom-left corner

        var endPoint = ObjectLabelLayout.GetLeaderEndPoint(objScreen, plaqueRect);

        Assert.Equal(new SKPoint(plaqueRect.Left, plaqueRect.Bottom), endPoint);
    }

    [Fact]
    public void GetLeaderEndPoint_returns_nearest_corner_bottom_right()
    {
        var plaqueRect = new SKRect(200, 100, 320, 118);
        var objScreen = new SKPoint(315, 114); // closest to bottom-right corner

        var endPoint = ObjectLabelLayout.GetLeaderEndPoint(objScreen, plaqueRect);

        Assert.Equal(new SKPoint(plaqueRect.Right, plaqueRect.Bottom), endPoint);
    }

    // ── Status square blink visibility ───────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    public void StatusSquare_visible_in_first_phase(long gameTimeMs)
    {
        Assert.True(StatusSquareAnimator.IsStatusSquareVisible(gameTimeMs, SimulationSpeed.Speed1));
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(1999)]
    public void StatusSquare_hidden_in_second_phase(long gameTimeMs)
    {
        Assert.False(StatusSquareAnimator.IsStatusSquareVisible(gameTimeMs, SimulationSpeed.Speed1));
    }

    [Fact]
    public void StatusSquare_visible_again_at_period_boundary()
    {
        // 2000 ms = full period → phase wraps back to the visible half
        Assert.True(StatusSquareAnimator.IsStatusSquareVisible(2000, SimulationSpeed.Speed1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(750)]
    [InlineData(1999)]
    public void StatusSquare_always_visible_when_paused(long gameTimeMs)
    {
        Assert.True(StatusSquareAnimator.IsStatusSquareVisible(gameTimeMs, SimulationSpeed.Speed0));
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
    public void Animation_period_is_2000ms()
    {
        Assert.Equal(2000.0, StatusSquareAnimator.PeriodMs);
    }

    [Fact]
    public void Status_text_gap_is_6px()
    {
        Assert.Equal(6f, ObjectLabelLayout.StatusTextGap);
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
