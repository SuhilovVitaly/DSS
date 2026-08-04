using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

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
}
