using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
using Xunit;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// UI scale (100% / 120% / 150%) must only resize the GameSession overlay panels —
/// never the tactical map, camera, or map hit-testing. See CLAUDE.md and the "UI
/// scale only in the main game session window" requirements.
/// </summary>
[Collection("InterfaceLog")]
public class GameSessionUiScaleTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1480;

    private static (SnapshotBuffer, GameSessionScreen) CreateScreen(float uiScale = 1.0f)
    {
        var buffer = new SnapshotBuffer();
        var predictor = new LinearMotionPredictor();
        var screen = new GameSessionScreen(buffer, predictor, uiScale: uiScale);
        return (buffer, screen);
    }

    private static void Render(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    // ── Validation / fallback ────────────────────────────────────

    [Fact]
    public void Default_uiScale_is_100_percent()
    {
        var (_, screen) = CreateScreen();
        Assert.Equal(1.0f, screen.UiScale);
    }

    [Theory]
    [InlineData(1.0f)]
    [InlineData(1.2f)]
    [InlineData(1.5f)]
    public void Allowed_uiScale_values_are_accepted(float scale)
    {
        var (_, screen) = CreateScreen(scale);
        Assert.Equal(scale, screen.UiScale);
    }

    [Theory]
    [InlineData(0.8f)]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(2.0f)]
    public void Invalid_uiScale_falls_back_to_100_percent(float invalidScale)
    {
        var (_, screen) = CreateScreen(invalidScale);
        Assert.Equal(1.0f, screen.UiScale);
    }

    [Fact]
    public void SetUiScale_applies_immediately_and_rejects_invalid_values()
    {
        var (_, screen) = CreateScreen();

        screen.SetUiScale(1.5f);
        Assert.Equal(1.5f, screen.UiScale);

        screen.SetUiScale(0.8f); // not in the allowed set
        Assert.Equal(1.0f, screen.UiScale);
    }

    // ── Map invariants ───────────────────────────────────────────

    [Fact]
    public void Camera_zoom_is_identical_regardless_of_uiScale()
    {
        var (_, screen100) = CreateScreen(1.0f);
        var (_, screen150) = CreateScreen(1.5f);
        Render(screen100);
        Render(screen150);

        screen100.OnMouseWheel(ScreenWidth / 2f, ScreenHeight / 2f, 1.0f);
        screen150.OnMouseWheel(ScreenWidth / 2f, ScreenHeight / 2f, 1.0f);

        Assert.Equal(screen100.CameraPixelsPerWorldUnit, screen150.CameraPixelsPerWorldUnit);
    }

    [Fact]
    public void Map_click_pans_camera_to_the_same_world_point_regardless_of_uiScale()
    {
        var (_, screen100) = CreateScreen(1.0f);
        var (_, screen150) = CreateScreen(1.5f);
        Render(screen100);
        Render(screen150);

        // A raw click well clear of every corner/edge UI panel, on free map area.
        float x = ScreenWidth * 0.4f;
        float y = ScreenHeight * 0.6f;

        screen100.OnMouseDown(x, y);
        screen150.OnMouseDown(x, y);

        Assert.Equal(screen100.CameraFocusX, screen150.CameraFocusX, precision: 6);
        Assert.Equal(screen100.CameraFocusY, screen150.CameraFocusY, precision: 6);
    }

    // ── UI panel layout scales with uiScale ──────────────────────

    [Fact]
    public void Speed_panel_logical_rect_shrinks_viewport_as_uiScale_grows()
    {
        // Panels are laid out in a logical viewport (raw / uiScale). At 150%, the
        // logical viewport is narrower, so the speed panel's logical right edge
        // must sit closer to the origin than at 100% — the canvas transform (not a
        // layout change) is what makes it visually bigger on screen.
        var (_, screen100) = CreateScreen(1.0f);
        var (_, screen150) = CreateScreen(1.5f);
        Render(screen100);
        Render(screen150);

        Assert.True(screen150.LastSpeedPanelRect.Right < screen100.LastSpeedPanelRect.Right);
    }

    // ── Hit-testing follows the visual (scaled) position ─────────

    [Fact]
    public void Speed_button_hit_test_uses_scaled_raw_coordinates()
    {
        var (buffer, screen) = CreateScreen(1.5f);
        Render(screen);

        // Logical (unscaled) button center — the button actually renders at
        // logicalCenter * uiScale in raw window pixels.
        var logicalRect = screen.SpeedButtonRects[2]; // "5x"
        float rawX = logicalRect.MidX * 1.5f;
        float rawY = logicalRect.MidY * 1.5f;

        screen.OnMouseDown(rawX, rawY);

        Assert.Equal(SimulationSpeed.Speed2, buffer.CurrentSpeed);
    }

    [Fact]
    public void Speed_button_click_at_unscaled_raw_coordinates_misses_at_150_percent()
    {
        var (buffer, screen) = CreateScreen(1.5f);
        Render(screen);

        var logicalRect = screen.SpeedButtonRects[2]; // "5x"

        // Clicking the logical coordinates directly (as if uiScale were 1.0) must
        // NOT hit the button once the panel is actually rendered at 150%.
        screen.OnMouseDown(logicalRect.MidX, logicalRect.MidY);

        Assert.NotEqual(SimulationSpeed.Speed2, buffer.CurrentSpeed);
    }
}
