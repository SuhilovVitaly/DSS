using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Motion;
using SkiaSharp;
using Xunit;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public class GameSessionZoomTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static (SnapshotBuffer, GameSessionScreen) CreateScreen()
    {
        var buffer = new SnapshotBuffer();
        var predictor = new LinearMotionPredictor();
        var screen = new GameSessionScreen(buffer, predictor);
        return (buffer, screen);
    }

    private static void Render(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    [Fact]
    public void Mouse_wheel_returns_None()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        var result = screen.OnMouseWheel(960, 540, 1.0f);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Mouse_wheel_up_zooms_in()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        // Zoom out first so PPU is not at the upper boundary (M1 = 1.0).
        screen.OnMouseWheel(960, 540, -1.0f);
        double ppuBefore = screen.CameraPixelsPerWorldUnit;
        screen.OnMouseWheel(960, 540, 1.0f);
        double ppuAfter = screen.CameraPixelsPerWorldUnit;

        Assert.True(ppuAfter > ppuBefore, $"Zoom in: {ppuAfter} > {ppuBefore}");
    }

    [Fact]
    public void Mouse_wheel_down_zooms_out()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        double ppuBefore = screen.CameraPixelsPerWorldUnit;
        screen.OnMouseWheel(960, 540, -1.0f);
        double ppuAfter = screen.CameraPixelsPerWorldUnit;

        Assert.True(ppuAfter < ppuBefore, $"Zoom out: {ppuAfter} < {ppuBefore}");
    }

    [Fact]
    public void Mouse_wheel_zero_delta_does_nothing()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        double ppuBefore = screen.CameraPixelsPerWorldUnit;
        var result = screen.OnMouseWheel(960, 540, 0f);
        double ppuAfter = screen.CameraPixelsPerWorldUnit;

        Assert.Equal(ScreenEvent.None, result);
        Assert.Equal(ppuBefore, ppuAfter);
    }

    [Fact]
    public void Zoom_does_not_change_panel_visibility()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        Assert.True(screen.IsPanelVisible);

        screen.OnMouseWheel(960, 540, 1.0f);

        Assert.True(screen.IsPanelVisible);
    }

    [Fact]
    public void Zoom_does_not_change_camera_focus_when_at_center()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        double fxBefore = screen.CameraFocusX;
        double fyBefore = screen.CameraFocusY;

        // Zoom at center — focus should stay (since anchor = center)
        screen.OnMouseWheel(960, 540, 1.0f);

        Assert.Equal(fxBefore, screen.CameraFocusX);
        Assert.Equal(fyBefore, screen.CameraFocusY);
    }

    // ── Wheel zoom boundary tests (M0.5=2.0 max, M1000=0.001 min) ──

    [Fact]
    public void Wheel_zoom_in_at_M0_5_boundary_does_not_exceed_max()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        // Click M0.5 to set PPU to the upper boundary.
        screen.OnMouseDown(screen.ScaleButtonRects[0].MidX, screen.ScaleButtonRects[0].MidY);
        Assert.Equal(2.0, screen.CameraPixelsPerWorldUnit);

        screen.OnMouseWheel(960, 540, 1.0f);

        Assert.Equal(2.0, screen.CameraPixelsPerWorldUnit);
    }

    [Fact]
    public void Wheel_zoom_out_at_M1000_boundary_does_not_go_below_min()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        // Click M1000 to set PPU to the lower boundary.
        screen.OnMouseDown(screen.ScaleButtonRects[4].MidX, screen.ScaleButtonRects[4].MidY);
        Assert.Equal(0.001, screen.CameraPixelsPerWorldUnit);

        screen.OnMouseWheel(960, 540, -1.0f);

        Assert.Equal(0.001, screen.CameraPixelsPerWorldUnit);
    }

    [Fact]
    public void Wheel_zoom_at_boundary_does_not_log()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        // Click M0.5 — PPU is now at the upper boundary (2.0).
        screen.OnMouseDown(screen.ScaleButtonRects[0].MidX, screen.ScaleButtonRects[0].MidY);
        Assert.Equal(2.0, screen.CameraPixelsPerWorldUnit);

        // Read only the tail appended after the action to isolate from parallel test writers.
        string logPath = Path.Combine(Environment.CurrentDirectory, InterfaceLog.FileName);
        long lengthBefore = File.Exists(logPath) ? new FileInfo(logPath).Length : 0;

        screen.OnMouseWheel(960, 540, 1.0f); // zoom-in at boundary → no change

        string tail = LogTailReader.ReadTail(lengthBefore);

        bool scaleLineFound = tail.Contains("Scale → PPU=", StringComparison.Ordinal);
        Assert.False(scaleLineFound, "Wheel zoom at boundary should NOT log 'Scale → PPU='");
    }

    [Fact]
    public void Multi_step_wheel_zoom_in_at_M0_5_never_exceeds_max()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        // Click M0.5 — PPU is at the upper boundary (2.0).
        screen.OnMouseDown(screen.ScaleButtonRects[0].MidX, screen.ScaleButtonRects[0].MidY);
        Assert.Equal(2.0, screen.CameraPixelsPerWorldUnit);

        for (int i = 0; i < 10; i++)
            screen.OnMouseWheel(960, 540, 1.0f);

        Assert.Equal(2.0, screen.CameraPixelsPerWorldUnit);
    }

    [Fact]
    public void Wheel_zoom_in_from_M1_reaches_half_step()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        // Default PPU is 1.0 (M1) — one wheel step in reaches 1.25,
        // proving the wheel allows zooming up to M0.5 (2.0).
        Assert.Equal(1.0, screen.CameraPixelsPerWorldUnit);

        screen.OnMouseWheel(960, 540, 1.0f);
        Assert.Equal(1.25, screen.CameraPixelsPerWorldUnit, precision: 12);

        // And it never exceeds the new upper boundary.
        for (int i = 0; i < 10; i++)
            screen.OnMouseWheel(960, 540, 1.0f);

        Assert.True(screen.CameraPixelsPerWorldUnit <= 2.0,
            $"PPU={screen.CameraPixelsPerWorldUnit} must not exceed 2.0");
    }
}
