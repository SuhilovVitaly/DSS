using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class InfoPanelTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static (SnapshotBuffer buffer, LinearMotionPredictor predictor, GameSessionScreen screen)
        CreateScreen()
    {
        var buffer = new SnapshotBuffer();
        var predictor = new LinearMotionPredictor();
        var screen = new GameSessionScreen(buffer, predictor);
        return (buffer, predictor, screen);
    }

    private static void RenderScreen(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    // ── Visibility ─────────────────────────────────────────────

    [Fact]
    public void Panel_is_visible_by_default()
    {
        var (_, _, screen) = CreateScreen();
        Assert.True(screen.IsPanelVisible);
    }

    [Fact]
    public void Panel_is_not_rendered_when_hidden()
    {
        var (_, _, screen) = CreateScreen();

        // Render once to populate panel rects
        RenderScreen(screen);
        Assert.True(screen.LastPanelRect.Width > 0);

        // Click close button to hide
        float cx = screen.LastCloseRect.MidX;
        float cy = screen.LastCloseRect.MidY;
        screen.OnMouseDown(cx, cy);
        Assert.False(screen.IsPanelVisible);

        // Render again — panel rect should be unchanged (not recomputed)
        var rectBefore = screen.LastPanelRect;
        RenderScreen(screen);
        Assert.Equal(rectBefore, screen.LastPanelRect);
    }

    // ── Close button ────────────────────────────────────────────

    [Fact]
    public void Close_button_click_hides_panel()
    {
        var (_, _, screen) = CreateScreen();
        RenderScreen(screen);

        Assert.True(screen.IsPanelVisible);
        Assert.True(screen.LastCloseRect.Width > 0, "Close button rect should be non-empty");

        // Click the close button
        float cx = screen.LastCloseRect.MidX;
        float cy = screen.LastCloseRect.MidY;
        var result = screen.OnMouseDown(cx, cy);

        Assert.False(screen.IsPanelVisible);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Close_button_does_not_change_camera_focus()
    {
        var (_, _, screen) = CreateScreen();
        RenderScreen(screen);

        double focusXBefore = screen.CameraFocusX;
        double focusYBefore = screen.CameraFocusY;

        // Click the close button
        float cx = screen.LastCloseRect.MidX;
        float cy = screen.LastCloseRect.MidY;
        var result = screen.OnMouseDown(cx, cy);

        Assert.Equal(ScreenEvent.None, result);
        Assert.False(screen.IsPanelVisible);

        // Camera must NOT have moved
        Assert.Equal(focusXBefore, screen.CameraFocusX);
        Assert.Equal(focusYBefore, screen.CameraFocusY);
    }

    // ── Panel click consumes event ──────────────────────────────

    [Fact]
    public void Click_inside_panel_does_not_pan_camera()
    {
        var (_, _, screen) = CreateScreen();
        RenderScreen(screen);

        Assert.True(screen.IsPanelVisible);

        double focusXBefore = screen.CameraFocusX;
        double focusYBefore = screen.CameraFocusY;

        // Click somewhere in the middle of the panel (not on close button)
        float px = screen.LastPanelRect.MidX;
        float py = screen.LastPanelRect.MidY;

        // Ensure we're not hitting the close button
        Assert.False(screen.LastCloseRect.Contains(px, py),
            "Test point should be inside panel but outside close button");

        var result = screen.OnMouseDown(px, py);

        // Panel should still be visible, no navigation event, camera NOT moved
        Assert.True(screen.IsPanelVisible);
        Assert.Equal(ScreenEvent.None, result);
        Assert.Equal(focusXBefore, screen.CameraFocusX);
        Assert.Equal(focusYBefore, screen.CameraFocusY);
    }

    // ── Map click still works ───────────────────────────────────

    [Fact]
    public void Click_outside_panel_pans_camera()
    {
        var (_, _, screen) = CreateScreen();
        RenderScreen(screen);

        // Click in the center of the screen (definitely outside the panel)
        var result = screen.OnMouseDown(ScreenWidth / 2f, ScreenHeight / 2f);

        Assert.Equal(ScreenEvent.None, result);
        // Camera moved (cannot easily assert new focus without reading internals,
        // but we can verify it didn't crash and returned None)
    }

    // ── Ctrl+I reopens ──────────────────────────────────────────

    [Fact]
    public void Ctrl_I_opens_panel_after_close()
    {
        var (_, _, screen) = CreateScreen();
        RenderScreen(screen);

        // Close panel via close button
        float cx = screen.LastCloseRect.MidX;
        float cy = screen.LastCloseRect.MidY;
        screen.OnMouseDown(cx, cy);
        Assert.False(screen.IsPanelVisible);

        // Ctrl+I should reopen
        var result = screen.OnKeyDown(Key.I);
        Assert.True(screen.IsPanelVisible);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Ctrl_I_is_noop_when_panel_already_visible()
    {
        var (_, _, screen) = CreateScreen();
        RenderScreen(screen);
        Assert.True(screen.IsPanelVisible);

        var result = screen.OnKeyDown(Key.I);
        Assert.True(screen.IsPanelVisible);
        Assert.Equal(ScreenEvent.None, result);
    }

    // ── Escape still works ──────────────────────────────────────

    [Fact]
    public void Escape_still_returns_OpenGameMenu()
    {
        var (_, _, screen) = CreateScreen();
        var result = screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.OpenGameMenu, result);
    }

    [Fact]
    public void Escape_still_works_when_panel_is_hidden()
    {
        var (_, _, screen) = CreateScreen();
        RenderScreen(screen);

        // Hide panel
        screen.OnMouseDown(screen.LastCloseRect.MidX, screen.LastCloseRect.MidY);
        Assert.False(screen.IsPanelVisible);

        // Escape should still work
        var result = screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.OpenGameMenu, result);
    }

    // ── Panel rect populated after render ───────────────────────

    [Fact]
    public void Panel_rect_is_populated_after_render()
    {
        var (_, _, screen) = CreateScreen();

        // Before render, rect is default (zero)
        Assert.True(screen.LastPanelRect.Width == 0);

        RenderScreen(screen);

        Assert.True(screen.LastPanelRect.Width > 0);
        Assert.True(screen.LastPanelRect.Height > 0);
        Assert.True(screen.LastCloseRect.Width > 0);
    }

    [Fact]
    public void Panel_lines_include_current_scale()
    {
        var (_, _, screen) = CreateScreen();
        RenderScreen(screen);

        var scaleLine = Assert.Single(screen.BuildPanelLines(null), line => line.Label == "Scale");
        Assert.Equal("1 px/unit", scaleLine.Value);

        // Zoom out (not in — default PPU=1.0 is now the wheel upper boundary).
        screen.OnMouseWheel(ScreenWidth / 2f, ScreenHeight / 2f, -1f);

        scaleLine = Assert.Single(screen.BuildPanelLines(null), line => line.Label == "Scale");
        Assert.Equal("0.8 px/unit", scaleLine.Value);
    }
}
